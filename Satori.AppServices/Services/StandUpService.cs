using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.TaskAdjustments;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.Kimai;
using Satori.Kimai.Models;
using System.Net;
using System.Text.RegularExpressions;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;
using UriFormatException = System.UriFormatException;

namespace Satori.AppServices.Services;

public partial class StandUpService(IKimaiServer kimai, IAzureDevOpsServer azureDevOps
    , ITaskAdjuster taskAdjuster
    )
{
    #region GetStandUpDaysAsync

    public async Task<StandUpDay[]> GetStandUpDaysAsync(DateOnly begin, DateOnly end)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (today < end)
        {
            end = today;
        }
        if (end < begin)
        {
            throw new ArgumentException($"Start date {begin:O} must be before or equal to end date {end:O}");
        }
        var daysInRange = (end.ToDateTime(TimeOnly.MinValue) - begin.ToDateTime(TimeOnly.MinValue)).Days + 1;
        if (daysInRange > 7)
        {
            throw new ArgumentException("There are too many days requested in this report.  Please use a smaller date range");
        }

        var getUserTask = kimai.GetMyUserAsync();
        var getTimeSheetTask = GetTimeSheetAsync(begin, end);

        await Task.WhenAll(getUserTask, getTimeSheetTask);

        var language = getUserTask.Result.Language;
        var url = kimai.BaseUrl.AppendPathSegments(language, "timesheet");

        var timeSheet = getTimeSheetTask.Result;
        var days = timeSheet.GroupBy(GetDateOnly);
        var standUpDays = days.Select(entries => ToDayViewModel(entries, url)).ToList();

        var allDays = Enumerable.Range(0, daysInRange).Select(begin.AddDays);
        AddMissingDays(standUpDays, allDays, url);

        return standUpDays.OrderByDescending(day => day.Date).ToArray();
    }

    private async Task<List<KimaiTimeEntry>> GetTimeSheetAsync(DateOnly begin, DateOnly end)
    {
        var filter = new TimeSheetFilter()
        {
            Begin = begin.ToDateTime(TimeOnly.MinValue),
            End = end.ToDateTime(TimeOnly.MaxValue),
            IsRunning = false,
            Page = 1,
            Size = 250,
        };

        var timeSheet = new List<KimaiTimeEntry>();
        bool done;
        do
        {
            try
            {
                var page = await kimai.GetTimeSheetAsync(filter);
                timeSheet.AddRange(page);
                done = page.Length < filter.Size;
                filter.Page++;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                done = true;
            }
        } while (!done);

        return timeSheet;
    }

    private void AddMissingDays(List<StandUpDay> standUpDays, IEnumerable<DateOnly> allDays, Url url)
    {
        standUpDays.AddRange( 
            allDays
                .Where(d => d.IsNotIn(standUpDays.Select(x => x.Date)))
                .Select(d => new NullGroup<DateOnly, KimaiTimeEntry>(d))
                .Select(g => ToDayViewModel(g, url))
        );
    }

    private StandUpDay ToDayViewModel(IGrouping<DateOnly, KimaiTimeEntry> entries, Url url)
    {
        var uri = url.ToUri()
            // ReSharper disable once StringLiteralTypo
            .AppendQueryParam("daterange", $"{entries.Key:O} - {entries.Key:O}")
            .AppendQueryParam("state", 3)  // stopped
            .AppendQueryParam("billable", 0)
            .AppendQueryParam("exported", 1)
            .AppendQueryParam("orderBy", "begin")
            .AppendQueryParam("order", "DESC")
            .AppendQueryParam("searchTerm", string.Empty)
            .AppendQueryParam("performSearch", "performSearch")
            .ToUri();

        return new StandUpDay()
        {
            Date = entries.Key,
            TotalTime = GetDuration(entries),
            AllExported = GetAllExported(entries),
            CanExport = GetCanExport(entries),
            Url = uri,
            Projects = [],
        }.With(day => day.Projects = ToProjectsViewModel(entries, uri, day));
    }

    private ProjectSummary[] ToProjectsViewModel(IEnumerable<KimaiTimeEntry> entries, Url url, StandUpDay day)
    {
        var groups = entries.GroupBy(entry => new
        {
            ProjectID = entry.Project.Id,
            ProjectName = entry.Project.Name,
            CustomerID = entry.Project.Customer.Id,
            CustomerName = entry.Project.Customer.Name,
            CustomerComment = entry.Project.Customer.Comment,
        });

        return groups.Select(g =>
            {
                var uri = url.ToUri().AppendQueryParam("projects[]", g.Key.ProjectID).ToUri();
                return new ProjectSummary()
                {
                    ProjectId = g.Key.ProjectID,
                    ProjectName = g.Key.ProjectName,
                    ParentDay = day,
                    CustomerId = g.Key.CustomerID,
                    CustomerName = g.Key.CustomerName,
                    CustomerAcronym = GetCustomerAcronym(g.Key.CustomerName),
                    CustomerUrl = GetCustomerLogo(g.Key.CustomerComment),
                    TotalTime = GetDuration(g),
                    AllExported = GetAllExported(g),
                    CanExport = GetCanExport(g),
                    Url = uri,
                    Activities = [],
                }.With(p => p.Activities = ToActivitiesViewModel(g, uri, p));
            })
            .OrderByDescending(p => p.TotalTime).ThenBy(p => p.ProjectName)
            .ToArray();
    }

    [GeneratedRegex(@"\((?'acronym'.*)\)", RegexOptions.IgnoreCase)]
    private static partial Regex CustomerAcronymRegex();

    private static string? GetCustomerAcronym(string customerName)
    {
        var match = CustomerAcronymRegex().Match(customerName);
        return match.Success ? match.Groups["acronym"].Value : null;
    }

    [GeneratedRegex(@"\[Logo\]\((?'url'.*)\)", RegexOptions.IgnoreCase)]
    private static partial Regex CustomerLogoRegex();

    private static Uri? GetCustomerLogo(string? comment)
    {
        if (comment == null)
        {
            return null;
        }
        
        var match = CustomerLogoRegex().Match(comment);
        if (!match.Success)
        {
            return null;
        }

        try
        {
            return new Uri(match.Groups["url"].Value);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private ActivitySummary[] ToActivitiesViewModel(IEnumerable<KimaiTimeEntry> entries, Url url, ProjectSummary project)
    {
        var groups = entries.GroupBy(entry => new
        {
            entry.Activity.Id,
            entry.Activity.Name,
            entry.Activity.Comment,
            ProjectId = entry.Project.Id,
        });

        return groups.Select(g =>
            {
                var uri = url.ToUri().AppendQueryParam("activities[]", g.Key.Id).ToUri();
                return new ActivitySummary()
                {
                    ActivityId = g.Key.Id,
                    ActivityName = g.Key.Name,
                    ParentProjectSummary = project,
                    Comment = g.Key.Comment,
                    TotalTime = GetDuration(g),
                    AllExported = GetAllExported(g),
                    CanExport = GetCanExport(g),
                    Url = uri,
                    TimeEntries = [],
                    TaskSummaries = [],
                }.With(a => a.TimeEntries = g.Select(entry => ToViewModel(a, entry)).ToArray());
            })
            .OrderByDescending(a => a.TotalTime).ThenBy(a => a.ActivityName)
            .ToArray();
    }

    [GeneratedRegex(@"^D#?(?'id'\d+)[\s-]*(?'title'.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex WorkItemCommentRegex();
    [GeneratedRegex(@"^D#?(?'parentId'\d+)[\s-]*(?'parentTitle'.*)\s+D#?(?'id'\d+)[\s-]*(?'title'.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ParentedWorkItemCommentRegex();

    private TimeEntry ToViewModel(ActivitySummary activitySummary, KimaiTimeEntry kimaiEntry)
    {
        var lines = kimaiEntry.Description?.Split('\n')
            .SelectWhereHasValue(x => string.IsNullOrWhiteSpace(x) ? null : x.Trim())
            .ToList() ?? [];

        return new TimeEntry()
        {
            Id = kimaiEntry.Id,
            ParentActivitySummary = activitySummary,
            Begin = kimaiEntry.Begin,
            End = kimaiEntry.End,
            TotalTime = GetDuration(kimaiEntry),
            Exported = kimaiEntry.Exported,
            CanExport = GetCanExport(kimaiEntry),
            Task = ExtractWorkItem(),
            Accomplishments = ExtractLinesWithPrefix("🏆"),
            Impediments = ExtractLinesWithPrefix("🧱"),
            Learnings = ExtractLinesWithPrefix("🧠"),
            OtherComments = RejoinLines(lines),
        };

        WorkItem? ExtractWorkItem()
        {
            var parentedWorkItemRegex = ParentedWorkItemCommentRegex();
            var match = lines.Select(x => parentedWorkItemRegex.Match(x)).FirstOrDefault(m => m.Success);
            WorkItem? parentWorkItem = null;
            if (match != null)
            {
                var parentId = int.Parse(match.Groups["parentId"].Value);
                var parentTitle = match.Groups["parentTitle"].Value;
                parentWorkItem = CreateWorkItem(parentId, parentTitle);
            }
            else
            {
                var workItemRegex = WorkItemCommentRegex();
                match = lines.Select(x => workItemRegex.Match(x)).FirstOrDefault(m => m.Success);
            }
            if (match == null)
            {
                return null;
            }

            lines.Remove(match.Value);
            
            var id = int.Parse(match.Groups["id"].Value);
            var title = match.Groups["title"].Value;
            var task = CreateWorkItem(id, title);
            
            task.Parent = parentWorkItem;

            return task;

        }

        string? ExtractLinesWithPrefix(string prefix)
        {
            var foundLines = lines.Where(x => x.StartsWith(prefix)).ToArray();
            lines.RemoveAll(x => x.IsIn(foundLines));

            return RejoinLines(foundLines.Select(x => x[prefix.Length..].Trim()));
        }
    }

    private WorkItem CreateWorkItem(int id, string title)
    {
        return new WorkItem
        {
            Id = id,
            Title = title.Trim(),
            Url = azureDevOps.ConnectionSettings.Url.AppendPathSegments("_workItems", "edit", id),
            AssignedTo = Person.Empty,
            CreatedBy = Person.Empty,
            Type = WorkItemType.Unknown,
            State = ScrumState.InProgress,
            Tags = [],
        };
    }

    private static string? RejoinLines(IEnumerable<string> lines) => RejoinLines(lines.ToArray());
    private static string? RejoinLines(string[] lines)
    {
        return lines.None() ? null : string.Join(Environment.NewLine, lines);
    }

    private static bool GetAllExported(IEnumerable<KimaiTimeEntry> entries) => entries.All(entry => entry.Exported);

    private static bool GetCanExport(IEnumerable<KimaiTimeEntry> entries) => entries.Any(GetCanExport);

    private static bool GetCanExport(KimaiTimeEntry entry)
    {
        if (entry.Exported) return false;
        if (!entry.Activity.Visible) return false;
        if (!entry.Project.Visible) return false;
        if (!entry.Project.Customer.Visible) return false;
        if (entry.Activity.Name == "TBD") return false;
        if (entry.Project.Name == "TBD") return false;

        return true;
    }

    private static TimeSpan GetDuration(IEnumerable<KimaiTimeEntry> entries) => entries.Select(GetDuration).Sum();

    private static TimeSpan GetDuration(KimaiTimeEntry entry) => entry.End != null ? entry.End.Value - entry.Begin : TimeSpan.Zero;

    private static DateOnly GetDateOnly(KimaiTimeEntry entry) => DateOnly.FromDateTime(entry.Begin.Date);

    #endregion GetStandUpDaysAsync

    #region GetWorkItemsAsync

    public async Task GetWorkItemsAsync(StandUpDay[] days)
    {
        var activitySummaries = days
            .SelectMany(day => day.Projects)
            .SelectMany(p => p.Activities)
            .ToArray();
        var timeEntries = activitySummaries
            .SelectMany(activitySummary => activitySummary.TimeEntries)
            .Where(entry => entry.Task != null)
            .ToArray();

        var workItems = await GetWorkItemsAsync(timeEntries);
        ResetWorkItems(timeEntries, workItems);

        ResetTimeRemaining(timeEntries);
        ResetNeedsEstimate(timeEntries);

        foreach(var summary in activitySummaries)
        {
            SummarizeTimeEntries(summary);
        }
    }

    /// <summary>
    /// Replace the Work Items referenced in the time entries with the new work items provided.
    /// </summary>
    /// <param name="timeEntries">Time Entries where the Task reference is just a placeholder <see cref="WorkItem"/> created merely from the Kimai time entry comment</param>
    /// <param name="workItems">Work Items that were loaded from Azure DevOps </param>
    private static void ResetWorkItems(TimeEntry[] timeEntries, List<WorkItem> workItems)
    {
        foreach (var task in workItems.Where(wi => wi.Type == WorkItemType.Task))
        {
            task.Parent = workItems.SingleOrDefault(wi => wi.Id == task.Parent?.Id);
        }

        foreach (var entry in timeEntries)
        {
            entry.Task = GetAllWorkItemIds(entry).Distinct()
                .Join(workItems, id => id, wi => wi.Id, (_, wi) => wi)
                .OrderByDescending(wi => wi.Type == WorkItemType.Task)
                .ThenBy(wi => wi.Id)
                .FirstOrDefault();
        }
    }

    private async Task<List<WorkItem>> GetWorkItemsAsync(TimeEntry[] timeEntries)
    {
        var workItemIds = timeEntries.SelectMany(GetAllWorkItemIds).Distinct();

        var workItems = (await GetWorkItemsAsync(workItemIds)).ToList();

        var parentIds = workItems
            .Where(wi => wi.Type == WorkItemType.Task)
            .SelectWhereHasValue(wi => wi.Parent?.Id)
            .Except(workItems.Select(wi => wi.Id));

        workItems.AddRange(await GetWorkItemsAsync(parentIds));
        return workItems;
    }

    private async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(IEnumerable<int> workItemIds)
    {
        return (await azureDevOps.GetWorkItemsAsync(workItemIds)).Select(wi => wi.ToViewModel());
    }

    private static IEnumerable<int> GetAllWorkItemIds(TimeEntry entry)
    {
        if (entry.Task == null)
        {
            yield break;
        }
        yield return entry.Task.Id;

        var workItem = entry.Task.Parent;
        while (workItem != null)
        {
            yield return workItem.Id;
            workItem = workItem.Parent;
        }
    }

    private static void ResetTimeRemaining(TimeEntry[] timeEntries)
    {
        foreach (var entry in timeEntries.Where(x => x.Task?.State != ScrumState.Done))
        {
            var unexported = timeEntries
                .Where(x => x.Task?.Id == entry.Task?.Id)
                .Where(x => !x.Exported)
                .SelectWhereHasValue(x => x.End - x.Begin)
                .Sum();

            var estimate = entry.Task?.RemainingWork ?? entry.Task?.OriginalEstimate;
            entry.TimeRemaining = estimate - unexported;
        }
    }
    
    private static void ResetNeedsEstimate(TimeEntry[] timeEntries)
    {
        foreach (var entry in timeEntries.Where(x => x.Task != null && x.Task.State != ScrumState.Done))
        {
            entry.NeedsEstimate = entry.Task!.State.IsIn(ScrumState.ToDo, ScrumState.InProgress)
                                  && entry.Task!.OriginalEstimate == null
                                  && entry.Task!.RemainingWork == null;
        }
    }

    private static void SummarizeTimeEntries(ActivitySummary activitySummary)
    {
        activitySummary.TaskSummaries = Summarize(activitySummary.TimeEntries);

        activitySummary.Accomplishments = GetDistinctComments(activitySummary, x => x.Accomplishments);
        activitySummary.Impediments = GetDistinctComments(activitySummary, x => x.Impediments);
        activitySummary.Learnings = GetDistinctComments(activitySummary, x => x.Learnings);
        activitySummary.OtherComments = GetDistinctComments(activitySummary, x => x.OtherComments);
    }

    private static string? GetDistinctComments(ActivitySummary activitySummary, Func<TimeEntry, string?> selector)
    {
        var lines = activitySummary.TimeEntries
            .OrderBy(entry => entry.Begin)
            .Select(selector)
            .SelectMany(comment => comment?.Split('\n') ?? [])
            .SelectWhereHasValue(x => string.IsNullOrWhiteSpace(x) ? null : x.Trim())
            .Distinct()
            .ToArray();

        return RejoinLines(lines);
    }

    private static TaskSummary[] Summarize(TimeEntry[] entries)
    {
        var taskGroups = entries.GroupBy(entry => entry.Task).ToArray();

        if (taskGroups is [{ Key: null }])
        {
            return [];
        }

        return taskGroups.Select(g => new TaskSummary()
        {
            Task = g.Key,
            TotalTime = g.Select(x => x.TotalTime).Sum(),
            TimeRemaining = g.First().TimeRemaining,
            NeedsEstimate = g.First().NeedsEstimate,
        }).ToArray();
    }

    #endregion GetWorkItemsAsync

    #region Export

    public async Task ExportAsync(params TimeEntry[] timeEntries)
    {
        var exportableEntries = timeEntries.Where(x => x.CanExport).ToArray();
        foreach (var entry in exportableEntries)
        {
            if (entry.Task != null)
            {
                await taskAdjuster.SendAsync(new TaskAdjustment(entry.Task.Id, entry.TotalTime));
                entry.Task.RemainingWork -= entry.TotalTime;
                entry.Task.CompletedWork = (entry.Task.CompletedWork ?? TimeSpan.Zero) + entry.TotalTime;
            }

            await kimai.ExportTimeSheetAsync(entry.Id);

            entry.Exported = true;
            entry.CanExport = false;
        }

        foreach (var activitySummary in exportableEntries.Select(entry => entry.ParentActivitySummary).Distinct())
        {
            activitySummary.AllExported = activitySummary.TimeEntries.All(x => x.Exported);
            activitySummary.CanExport = activitySummary.TimeEntries.Any(x => x.CanExport);
        }
        foreach (var projectSummary in exportableEntries.Select(entry => entry.ParentActivitySummary.ParentProjectSummary).Distinct())
        {
            projectSummary.AllExported = projectSummary.Activities.All(x => x.AllExported);
            projectSummary.CanExport = projectSummary.Activities.Any(x => x.CanExport);
        }
        foreach (var day in exportableEntries.Select(entry => entry.ParentActivitySummary.ParentProjectSummary.ParentDay).Distinct())
        {
            day.AllExported = day.Projects.All(x => x.AllExported);
            day.CanExport = day.Projects.Any(x => x.CanExport);
        }
    }

    #endregion Export
}