using CodeMonkeyProjectiles.Linq;
using Flurl;
using Microsoft.Extensions.Logging;
using Satori.AppServices.Models;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.CommentParsing;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.ExportPayloads;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.Kimai;
using Satori.Kimai.Models;
using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Satori.Kimai.Utilities;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;
using UriFormatException = System.UriFormatException;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

public partial class StandUpService(
    IKimaiServer kimai
    , IAzureDevOpsServer azureDevOps
    , UserService userService
    , IDailyActivityExporter dailyActivityExporter
    , ITaskAdjustmentExporter taskAdjustmentExporter
    , ILoggerFactory loggerFactory
    , IAlertService alertService
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

        var timeSheet = getTimeSheetTask.Result;
        var period = new PeriodSummary()
        {
            TotalTime = GetDuration(timeSheet),
            AllExported = GetAllExported(timeSheet),
            CanExport = GetCanExport(timeSheet),
            IsRunning = GetIsRunning(timeSheet),
        };

        AddDaysViewModels(period, timeSheet);

        var allDays = Enumerable.Range(0, daysInRange).Select(begin.AddDays);
        AddMissingDays(period, allDays);

        return period;
    }

    private static void SetIsOverlapping(List<KimaiTimeEntry> timeSheet)
    {
        var entries = timeSheet.OrderBy(x => x.Begin).ToArray();
        for (var i = 0; i < entries.Length; i++)
        {
            var j = i + 1;
            while (j < entries.Length 
                   && entries[i].End != null && entries[j].End != null
                   && entries[j].Begin < entries[i].End)
            {
                entries[i].IsOverlapping = true;
                entries[j].IsOverlapping = true;
                j++;
            }
        }
    }

    private void AddDaysViewModels(PeriodSummary period, List<KimaiTimeEntry> timeSheet)
    {
        foreach (var entries in timeSheet.GroupBy(GetDateOnly))
        {
            AddDayViewModel(period, entries);
        }
    }

    private Url KimaiTimeSheetUrl
    {
        get
        {
            var language = Person.Me?.Language.Replace("-", "_") ?? "en";
            var url = kimai.BaseUrl.AppendPathSegments(language, "timesheet");
            return url;
        }
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
            catch (Exception ex)
            {
                alertService.BroadcastAlert(ex);
                done = true;
            }
        } while (!done);

        SetIsOverlapping(timeSheet);

        return timeSheet;
    }

    private void AddMissingDays(PeriodSummary period, IEnumerable<DateOnly> allDays)
    {
        var missingDays = allDays
            .Where(d => d.IsNotIn(period.Days.Select(x => x.Date)));

        foreach (var day in missingDays)
        {
            var timeEntryGroup = new NullGroup<DateOnly, KimaiTimeEntry>(day);
            AddDayViewModel(period, timeEntryGroup);
        }
    }

    private void AddDayViewModel(PeriodSummary period, IGrouping<DateOnly, KimaiTimeEntry> entries)
    {
        var uri = KimaiTimeSheetUrl.ToUri()
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
            ProjectVisible = entry.Project.Visible,
            CustomerID = entry.Project.Customer.Id,
            CustomerName = entry.Project.Customer.Name,
            CustomerVisible = entry.Project.Customer.Visible,
            CustomerComment = entry.Project.Customer.Comment,
        });

        return groups.Select(g =>
            {
                var uri = url.ToUri().AppendQueryParam("projects[]", g.Key.ProjectID).ToUri();
                return new ProjectSummary()
                {
                    ProjectId = g.Key.ProjectID,
                    ProjectName = g.Key.ProjectName,
                    IsActive = g.Key.ProjectVisible,
                    ParentDay = day,
                    CustomerId = g.Key.CustomerID,
                    CustomerName = g.Key.CustomerName,
                    CustomerIsActive = g.Key.CustomerVisible,
                    CustomerAcronym = GetCustomerAcronym(g.Key.CustomerName),
                    CustomerUrl = CustomerLogoParser.GetCustomerLogo(g.Key.CustomerComment),
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

    private ActivitySummary[] ToActivitiesViewModel(IEnumerable<KimaiTimeEntry> entries, Url url, ProjectSummary project)
    {
        var groups = entries.GroupBy(entry => new
        {
            entry.Activity.Id,
            entry.Activity.Name,
            entry.Activity.Comment,
            entry.Activity.Visible,
            ProjectId = entry.Project.Id,
        });

        var activitySummaries = groups.Select(g =>
            {
                var uri = url.ToUri().AppendQueryParam("activities[]", g.Key.Id).ToUri();
                return new ActivitySummary()
                {
                    ActivityId = g.Key.Id,
                    ActivityName = g.Key.Name,
                    IsActive = g.Key.Visible,
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

   private TimeEntry ToViewModel(ActivitySummary activitySummary, KimaiTimeEntry kimaiEntry)
   { 
        var comments = CommentParser.Parse(kimaiEntry.Description).ToList();

        WorkItem? workItem = null;
        var workItemComment = comments.OfType<WorkItemComment>().FirstOrDefault();
        if (workItemComment != null && azureDevOps.Enabled)
        {
            workItem = BuildWorkItem(workItemComment);
            comments.Remove(workItemComment);
        }

        return new TimeEntry()
        {
            Id = kimaiEntry.Id,
            ParentActivitySummary = activitySummary,
            Begin = kimaiEntry.Begin,
            End = kimaiEntry.End,
            TotalTime = GetDuration(kimaiEntry),
            IsRunning = kimaiEntry.End == null,
            IsOverlapping = kimaiEntry.IsOverlapping,
            Exported = kimaiEntry.Exported,
            CanExport = GetCanExport(kimaiEntry),
            Task = workItem,
            Accomplishments = comments.Join(type => type == CommentType.Accomplishment),
            Impediments = comments.Join(type => type == CommentType.Impediment),
            Learnings = comments.Join(type => type == CommentType.Learning),
            OtherComments = comments.Join(type => type.IsNotIn(CommentType.ScrumTypes)),
        };
    }

    private WorkItem BuildWorkItem(WorkItemComment comment)
    {
        var (id, title) = comment.WorkItems.Last();
        var task = CreateWorkItem(id, title);
        if (comment.WorkItems.Count == 1)
        {
            return task;
        }

        var (parentId, parentTitle) = comment.WorkItems.First();
        task.Parent = CreateWorkItem(parentId, parentTitle);
        return task;
    }

    private WorkItem CreateWorkItem(int id, string title)
    {
        return new WorkItem
        {
            Id = id,
            Title = title.Trim(),
            ProjectName = "TeamProject",
            Url = azureDevOps.ConnectionSettings.Url.AppendPathSegments("_workItems", "edit", id),
            ApiUrl = azureDevOps.ConnectionSettings.Url.AppendPathSegments("_apis/wit/workItems", id),
            AssignedTo = Person.Empty,
            CreatedBy = Person.Empty,
            Type = WorkItemType.Unknown,
            State = ScrumState.InProgress,
            Tags = [],
        };
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
        if (entry.IsOverlapping) return false;

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
                .ThenByDescending(wi => wi.Type.IsIn(WorkItemType.BoardTypes))
                .ThenByDescending(wi => wi.Type == WorkItemType.Feature)
                .ThenByDescending(wi => wi.Type == WorkItemType.Epic)
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

    private static void ResetTimeRemaining(TimeEntry[] timeEntries)
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

    private static string? RejoinLines(string[] lines)
    {
        return lines.None() ? null : string.Join(Environment.NewLine, lines);
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
        })
            .OrderByDescending(t => t.TotalTime)
            .ToArray();

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

        await ExportToMessageQueueAsync(exportableEntries);
        await ExportToAzureDevOpsAsync(exportableEntries);
        await ExportToKimaiAsync(exportableEntries);

        CascadeExportFlags(exportableEntries);
    }

    #region Export To Message Queue

    private async Task ExportToMessageQueueAsync(TimeEntry[] exportableEntries)
    {
        foreach (var activitySummary in exportableEntries.Select(entry => entry.ParentActivitySummary).Distinct())
        {
            var payload = await ToPayloadAsync(activitySummary, exportableEntries);
            await dailyActivityExporter.SendAsync(payload);
        }
    }

    private async Task<DailyActivity> ToPayloadAsync(ActivitySummary activitySummary, TimeEntry[] exportableEntries)
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
            Tasks = ToComment(activitySummary.TaskSummaries),
            WorkItems = await ToWorkItemsAsync(activitySummary.TaskSummaries),
        };
        return payload;
    }

    private async Task<IReadOnlyCollection<ViewModels.ExportPayloads.WorkItem>> ToWorkItemsAsync(TaskSummary[] taskSummaries)
    {
        var workItems = taskSummaries.SelectMany(timeEntry => GetWorkItemAncestors(timeEntry.Task))
            .Distinct()
            .ToList();

        var unknownWorkItems = workItems.Where(wi => wi.Type == WorkItemType.Unknown).ToArray();
        if (unknownWorkItems.Any())
        {
            var freshWorkItems = await GetWorkItemsAsync(unknownWorkItems.Select(wi => wi.Id));
            foreach (var invalidWorkItem in unknownWorkItems)
            {
                workItems.Remove(invalidWorkItem);
            }
            workItems.AddRange(freshWorkItems);
        }

        return workItems
            .Select(x => new ViewModels.ExportPayloads.WorkItem
            {
                Id = x.Id, 
                Title = x.Title ?? string.Empty, 
                Type = x.Type.ToApiValue(),
                ParentId = x.Parent?.Id,
            })
            .ToImmutableArray();
    }

    private static IEnumerable<WorkItem> GetWorkItemAncestors(WorkItem? workItem)
    {
        while (true)
        {
            if (workItem == null)
            {
                yield break;
            }

            yield return workItem;
            workItem = workItem.Parent;
        }
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

    #endregion

    private async Task ExportToAzureDevOpsAsync(TimeEntry[] exportableEntries)
    {
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
    }

    private async Task ExportToKimaiAsync(TimeEntry[] exportableEntries)
    {
        foreach (var entry in exportableEntries)
        {
            await kimai.ExportTimeSheetAsync(entry.Id);

            entry.Exported = true;
            entry.CanExport = false;
        }
    }

    private static void CascadeExportFlags(params TimeEntry[] exportableEntries)
    {
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
            activitySummary.IsRunning = activitySummary.TimeEntries.Any(x => x.IsRunning);
        }
        foreach (var projectSummary in exportableEntries.Select(entry => entry.ParentActivitySummary.ParentProjectSummary).Distinct())
        {
            projectSummary.AllExported = projectSummary.Activities.All(x => x.AllExported);
            projectSummary.CanExport = projectSummary.Activities.Any(x => x.CanExport);
            projectSummary.IsRunning = projectSummary.Activities.Any(x => x.IsRunning);
        }
        foreach (var day in exportableEntries.Select(entry => entry.ParentActivitySummary.ParentProjectSummary.ParentDay).Distinct())
        {
            day.AllExported = day.Projects.All(x => x.AllExported);
            day.CanExport = day.Projects.Any(x => x.CanExport);
            day.IsRunning = day.Projects.Any(x => x.IsRunning);
        }
        foreach (var period in exportableEntries.Select(entry => entry.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod).Distinct())
        {
            period.AllExported = period.Days.All(x => x.AllExported);
            period.CanExport = period.Days.Any(x => x.CanExport);
            period.IsRunning = period.Days.Any(x => x.IsRunning);
        }
    }

    #endregion Export

    #region Update Time Entry Description

    /// <summary>
    /// Updates the description on the time entries in Kimai and reloads the Day model
    /// </summary>
    /// <param name="day">View Model to update after Kimai is updated</param>
    /// <param name="newDescriptionMap">Dictionary map of Kimai Time Entry Ids and their new description values</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task UpdateTimeEntryDescriptionAsync(DaySummary day, Dictionary<int, string> newDescriptionMap)
    {
        await UpdateKimaiTimeEntryDescriptionAsync(newDescriptionMap);
        await UpdateViewModelAsync(day);
    }

    private async Task UpdateKimaiTimeEntryDescriptionAsync(Dictionary<int, string> newDescriptionMap)
    {
        foreach (var kvp in newDescriptionMap)
        {
            await kimai.UpdateTimeEntryDescriptionAsync(kvp.Key, kvp.Value);
        }
    }

    private async Task UpdateViewModelAsync(DaySummary day)
    {
        var timeSheet = await GetTimeSheetAsync(day.Date, day.Date);

        var period = day.ParentPeriod;
        period.Days.Remove(day);

        AddDaysViewModels(period, timeSheet);

        await GetWorkItemsAsync(period);
    }

    #endregion Update Time Entry Description

    #region StopTimer
    
    public async Task StopTimerAsync(TimeEntry timeEntry)
    {
        var end = await kimai.StopTimerAsync(timeEntry.Id);

        timeEntry.End = end;
        SetIsOverlapping(timeEntry);
        
        timeEntry.IsRunning = false;
        timeEntry.CanExport = GetCanExport(timeEntry);
        CascadeExportFlags(timeEntry);

        CascadeEndTimeChange(timeEntry, end);
    }

    private static void SetIsOverlapping(TimeEntry timeEntry)
    {
        var period = timeEntry.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod;
        var otherTimeEntries = period.TimeEntries.Except(timeEntry.Yield()).ToArray();
        var overlappingTimeEntries = otherTimeEntries
            .Where(t => t.IsOverlapping(timeEntry))
            .ToArray();
            
        timeEntry.IsOverlapping = overlappingTimeEntries.Any();
        foreach (var overlappingTimeEntry in overlappingTimeEntries)
        {
            overlappingTimeEntry.IsOverlapping = true;
        }
    }

    private static bool GetCanExport(TimeEntry timeEntry)
    {
        if (!timeEntry.ParentActivitySummary.IsActive) return false;
        if (!timeEntry.ParentActivitySummary.ParentProjectSummary.IsActive) return false;
        if (!timeEntry.ParentActivitySummary.ParentProjectSummary.CustomerIsActive) return false;
        if (timeEntry.ParentActivitySummary.ActivityName == "TBD") return false;
        if (timeEntry.ParentActivitySummary.ParentProjectSummary.ProjectName == "TBD") return false;
        if (timeEntry.IsOverlapping) return false;

        return true;
    }

    public static void CascadeEndTimeChange(TimeEntry timeEntry, DateTimeOffset end)
    {
        timeEntry.TotalTime = end - timeEntry.Begin;

        var task = timeEntry.Task;
        if (task?.RemainingWork != null && task.State != ScrumState.Done)
        {
            var period = timeEntry.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod;
            var timeEntries = period.TimeEntries
                .Where(t => t.Task == task)
                .ToArray();
            ResetTimeRemaining(timeEntries);
        }

        var taskSummary = timeEntry.ParentTaskSummary ?? throw new InvalidOperationException();
        taskSummary.TotalTime = taskSummary.TimeEntries.Select(e => e.TotalTime).Sum();

        var activitySummary = taskSummary.ParentActivitySummary;
        activitySummary.TotalTime = activitySummary.TimeEntries.Select(t => t.TotalTime).Sum();

        var projectSummary = activitySummary.ParentProjectSummary;
        projectSummary.TotalTime = projectSummary.Activities.Select(a => a.TotalTime).Sum();

        var daySummary = projectSummary.ParentDay;
        daySummary.TotalTime = daySummary.Projects.Select(p => p.TotalTime).Sum();

        var periodSummary = daySummary.ParentPeriod;
        periodSummary.TotalTime = periodSummary.Days.Select(d => d.TotalTime).Sum();
    }

    #endregion StopTimer
}