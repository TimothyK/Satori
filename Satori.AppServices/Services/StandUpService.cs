﻿using CodeMonkeyProjectiles.Linq;
using Flurl;
using Microsoft.Extensions.Logging;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.ExportPayloads;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.Kimai;
using Satori.Kimai.Models;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;
using UriFormatException = System.UriFormatException;

namespace Satori.AppServices.Services;

public partial class StandUpService(
    IKimaiServer kimai
    , IAzureDevOpsServer azureDevOps
    , UserService userService
    , IDailyActivityExporter dailyActivityExporter
    , ITaskAdjustmentExporter taskAdjustmentExporter
    , ILoggerFactory loggerFactory
    )
{
    #region GetStandUpDaysAsync

    public async Task<PeriodSummary> GetStandUpPeriodAsync(DateOnly begin, DateOnly end)
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

        var getUserTask = userService.GetCurrentUserAsync();
        var getTimeSheetTask = GetTimeSheetAsync(begin, end);

        await Task.WhenAll(getUserTask, getTimeSheetTask);

        var language = getUserTask.Result.Language.Replace("-", "_");
        var url = kimai.BaseUrl.AppendPathSegments(language, "timesheet");

        var timeSheet = getTimeSheetTask.Result;
        var period = new PeriodSummary()
        {
            TotalTime = GetDuration(timeSheet),
            AllExported = GetAllExported(timeSheet),
            CanExport = GetCanExport(timeSheet),
            IsRunning = GetIsRunning(timeSheet),
        };
        foreach (var entries in timeSheet.GroupBy(GetDateOnly))
        {
            AddDayViewModel(period, entries, url);
        }
        var allDays = Enumerable.Range(0, daysInRange).Select(begin.AddDays);
        AddMissingDays(period, allDays, url);

        return period;
    }

    private async Task<List<KimaiTimeEntry>> GetTimeSheetAsync(DateOnly begin, DateOnly end)
    {
        var filter = new TimeSheetFilter()
        {
            Begin = begin.ToDateTime(TimeOnly.MinValue),
            End = end.ToDateTime(TimeOnly.MaxValue),
            IsRunning = null,
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

    private void AddMissingDays(PeriodSummary period, IEnumerable<DateOnly> allDays, Url url)
    {
        var missingDays = allDays
            .Where(d => d.IsNotIn(period.Days.Select(x => x.Date)));

        foreach (var day in missingDays)
        {
            var timeEntryGroup = new NullGroup<DateOnly, KimaiTimeEntry>(day);
            AddDayViewModel(period, timeEntryGroup, url);
        }
    }

    private void AddDayViewModel(PeriodSummary period, IGrouping<DateOnly, KimaiTimeEntry> entries, Url url)
    {
        var uri = url.ToUri()
            // ReSharper disable once StringLiteralTypo
            .AppendQueryParam("daterange", $"{entries.Key:O} - {entries.Key:O}")
            .AppendQueryParam("state", 1)  // stopped & running
            .AppendQueryParam("billable", 0)
            .AppendQueryParam("exported", 1)
            .AppendQueryParam("orderBy", "begin")
            .AppendQueryParam("order", "DESC")
            .AppendQueryParam("searchTerm", string.Empty)
            .AppendQueryParam("performSearch", "performSearch")
            .ToUri();

        var day = new DaySummary()
        {
            Date = entries.Key,
            ParentPeriod = period,
            TotalTime = GetDuration(entries),
            AllExported = GetAllExported(entries),
            CanExport = GetCanExport(entries),
            IsRunning = GetIsRunning(entries),
            Url = uri,
            Projects = [],
        }.With(day => day.Projects = ToProjectsViewModel(entries, uri, day));

        var days = period.Days.ToList();
        days.Add(day);
        days = days.OrderByDescending(d => d.Date).ToList();
        var index = days.IndexOf(day);
        period.Days.Insert(index, day);
    }

    private ProjectSummary[] ToProjectsViewModel(IEnumerable<KimaiTimeEntry> entries, Url url, DaySummary day)
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
                    IsRunning = GetIsRunning(g),
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

        var activitySummaries = groups.Select(g =>
            {
                var uri = url.ToUri().AppendQueryParam("activities[]", g.Key.Id).ToUri();
                return new ActivitySummary()
                {
                    ActivityId = g.Key.Id,
                    ActivityName = g.Key.Name,
                    ParentProjectSummary = project,
                    ActivityDescription = g.Key.Comment,
                    TotalTime = GetDuration(g),
                    AllExported = GetAllExported(g),
                    CanExport = GetCanExport(g),
                    IsRunning = GetIsRunning(g),
                    Url = uri,
                    TimeEntries = [],
                    TaskSummaries = [],
                }.With(a => a.TimeEntries = g.Select(entry => ToViewModel(a, entry)).ToArray());
            })
            .OrderByDescending(a => a.TotalTime).ThenBy(a => a.ActivityName)
            .ToArray();

        foreach(var summary in activitySummaries)
        {
            SummarizeTimeEntries(summary);
        }

        return activitySummaries;
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
            IsRunning = kimaiEntry.End == null,
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
            if (!azureDevOps.Enabled)
            {
                return null;
            }

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
    private static bool GetIsRunning(IEnumerable<KimaiTimeEntry> entries) => entries.Any(t => t.End == null);

    private static bool GetCanExport(KimaiTimeEntry entry)
    {
        if (entry.Exported) return false;
        if (entry.End == null) return false;
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

    public async Task GetWorkItemsAsync(PeriodSummary period)
    {
        var activitySummaries = period.Days
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
            var task = GetAllWorkItemIds(entry).Distinct()
                .Join(workItems, id => id, wi => wi.Id, (_, wi) => wi)
                .OrderByDescending(wi => wi.Type == WorkItemType.Task)
                .ThenBy(wi => wi.Id)
                .FirstOrDefault();

            if (task != null)
            {
                entry.Task = task;
            }
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

    public async Task<WorkItem?> GetWorkItemAsync(int workItemId)
    {
        var workItems = await GetWorkItemsAsync([workItemId]);
        var workItem = workItems.FirstOrDefault();
        if (workItem == null)
        {
            return null;
        }

        if (workItem.Type == WorkItemType.Task && workItem.Parent != null && workItem.Parent.Type == WorkItemType.Unknown)
        {
            var parent = await GetWorkItemAsync(workItem.Parent.Id);
            workItem.Parent = parent;
        }

        return workItem;
    }

    /// <summary>
    /// Loads the placeholder children work items
    /// </summary>
    /// <param name="workItem"></param>
    /// <returns></returns>
    public async Task GetChildWorkItemsAsync(WorkItem workItem)
    {
        var placeholderChildren = workItem.Children
            .Where(wi => wi.Type == WorkItemType.Unknown)
            .ToArray();
        if (placeholderChildren.None())
        {
            return;
        }

        var children = (await GetWorkItemsAsync(placeholderChildren.Select(wi => wi.Id))).ToArray();
        foreach (var placeholder in placeholderChildren)
        {
            workItem.Children.Remove(placeholder);
        }
        workItem.Children.AddRange(children);

        foreach (var child in children)
        {
            child.Parent = workItem;
        }
    }

    private async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(IEnumerable<int> workItemIds) =>
        await GetWorkItemsAsync(workItemIds.ToArray());

    private async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(int[] workItemIds)
    {
        try
        {
            return (await azureDevOps.GetWorkItemsAsync(workItemIds)).Select(wi => wi.ToViewModel());
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<StandUpService>();
            logger.LogError(ex, "Failed to load work items {WorkItemIds}", workItemIds);

            var badIds = workItemIds.Where(id => ex.Message.Contains($" {id} ")).ToList();
            if (badIds.Count > 0)
            {
                return await GetWorkItemsAsync(workItemIds.Except(badIds).ToArray());
            }

            return [];
        }
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

    public static void ResetTimeRemaining(TimeEntry[] timeEntries)
    {
        foreach (var entry in timeEntries.Where(x => x.Task?.State != ScrumState.Done))
        {
            var unexported = timeEntries
                .Where(x => x.Task?.Id == entry.Task?.Id)
                .Where(x => !x.Exported)
                .Select(x => x.TotalTime)
                .Sum();

            var estimate = entry.Task?.RemainingWork ?? entry.Task?.OriginalEstimate;
            entry.TimeRemaining = estimate - unexported;

            if (entry.ParentTaskSummary != null)
            {
                entry.ParentTaskSummary.TimeRemaining = entry.TimeRemaining;
            }
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

    private static string? GetDistinctComments(ActivitySummary activitySummary, Func<TimeEntry, string?> selector) =>
        GetDistinctComments(activitySummary.TimeEntries, selector);

    private static string? GetDistinctComments(IEnumerable<TimeEntry> entries, Func<TimeEntry, string?> selector)
    {
        var lines = entries
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

        var taskSummaries = taskGroups.Select(g => new TaskSummary()
        {
            Task = g.Key,
            ParentActivitySummary = g.Select(entry => entry.ParentActivitySummary).Distinct().Single(),
            TimeEntries = g.ToArray(),
            TotalTime = g.Select(x => x.TotalTime).Sum(),
            TimeRemaining = g.First().TimeRemaining,
            NeedsEstimate = g.First().NeedsEstimate,
        }).ToArray();

        foreach (var taskSummary in taskSummaries)
        {
            foreach (var entry in taskSummary.TimeEntries)
            {
                entry.ParentTaskSummary = taskSummary;
            }
            taskSummary.AllExported = taskSummary.TimeEntries.All(x => x.Exported);
            taskSummary.CanExport = taskSummary.TimeEntries.Any(x => x.CanExport);
            taskSummary.IsRunning = taskSummary.TimeEntries.Any(x => x.IsRunning);
            taskSummary.Accomplishments = GetDistinctComments(taskSummary.TimeEntries, x => x.Accomplishments);
            taskSummary.Impediments = GetDistinctComments(taskSummary.TimeEntries, x => x.Impediments);
            taskSummary.Learnings = GetDistinctComments(taskSummary.TimeEntries, x => x.Learnings);
            taskSummary.OtherComments = GetDistinctComments(taskSummary.TimeEntries, x => x.OtherComments);
        }

        return taskSummaries;
    }

    #endregion GetWorkItemsAsync

    #region Export

    public async Task ExportAsync(params TimeEntry[] timeEntries)
    {
        var exportableEntries = timeEntries.Where(x => x.CanExport).ToArray();

        foreach (var activitySummary in exportableEntries.Select(entry => entry.ParentActivitySummary).Distinct())
        {
            var payload = ToPayload(activitySummary, exportableEntries);
            await dailyActivityExporter.SendAsync(payload);
        }

        foreach (var g in exportableEntries
                 .Where(x => x.Task != null)
                 .Where(x => x.Task!.AssignedTo == Person.Me)
                 .GroupBy(x => x.Task))
        {
            var adjustment = new TaskAdjustment(g.Key!.Id, g.Select(x => x.TotalTime).Sum());
            await taskAdjustmentExporter.SendAsync(adjustment);

            g.Key.RemainingWork -= adjustment.Adjustment;
            g.Key.CompletedWork = (g.Key.CompletedWork ?? TimeSpan.Zero) + adjustment.Adjustment;
        }
        
        foreach (var entry in exportableEntries)
        {
            await kimai.ExportTimeSheetAsync(entry.Id);

            entry.Exported = true;
            entry.CanExport = false;
        }

        foreach (var taskSummary in exportableEntries.Select(entry => entry.ParentTaskSummary).Distinct())
        {
            _ = taskSummary ?? throw new InvalidOperationException("TaskSummary is null");
            taskSummary.AllExported = taskSummary.TimeEntries.All(x => x.Exported);
            taskSummary.CanExport = taskSummary.TimeEntries.Any(x => x.CanExport);
            taskSummary.IsRunning = taskSummary.TimeEntries.Any(x => x.IsRunning);
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
        foreach (var period in exportableEntries.Select(entry => entry.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod).Distinct())
        {
            period.AllExported = period.Days.All(x => x.AllExported);
            period.CanExport = period.Days.Any(x => x.CanExport);
        }
    }

    private static DailyActivity ToPayload(ActivitySummary activitySummary, TimeEntry[] exportableEntries)
    {
        var previous = activitySummary.TimeEntries.Where(x => x.Exported).Select(x => x.TotalTime).Sum();
        var adjustment = exportableEntries.Where(x => x.ParentActivitySummary == activitySummary).Select(x => x.TotalTime).Sum();

        var payload = new DailyActivity()
        {
            Date = activitySummary.ParentProjectSummary.ParentDay.Date,
            ActivityId = activitySummary.ActivityId,
            ActivityName = activitySummary.ActivityName,
            ActivityDescription = activitySummary.ActivityDescription,
            ProjectId = activitySummary.ParentProjectSummary.ProjectId,
            ProjectName = activitySummary.ParentProjectSummary.ProjectName,
            CustomerId = activitySummary.ParentProjectSummary.CustomerId,
            CustomerName = activitySummary.ParentProjectSummary.CustomerName,
            TotalTime = previous + adjustment,
            Accomplishments = activitySummary.Accomplishments,
            Impediments = activitySummary.Impediments,
            Learnings = activitySummary.Learnings,
            OtherComments = activitySummary.OtherComments,
            Tasks = ToComment(activitySummary.TaskSummaries)
        };
        return payload;
    }

    private static string? ToComment(TaskSummary[] taskSummaries)
    {
        var lines = taskSummaries.Where(x => x.Task != null).Select(x => ToComment(x.Task!)).ToArray();
        return lines.None() ? null : string.Join(Environment.NewLine, lines);
    }

    private static string ToComment(WorkItem task)
    {
        var builder = new StringBuilder();

        if (task.Parent != null && task.Type == WorkItemType.Task)
        {
            builder.Append($"D#{task.Parent.Id} {task.Parent.Title}");
            builder.Append(" » ");
        }
        builder.Append($"D#{task.Id} {task.Title}");

        return builder.ToString();
    }

    #endregion Export
}