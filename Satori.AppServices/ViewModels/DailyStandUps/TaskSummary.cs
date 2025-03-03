using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.AppServices.ViewModels.DailyStandUps;

public class TaskSummary : ISummary
{
    public WorkItem? Task { get; init; }

    public required ActivitySummary ParentActivitySummary { get; init; }

    public required TimeEntry[] TimeEntries { get; set; }
    IEnumerable<TimeEntry> ISummary.TimeEntries => TimeEntries;

    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; } = true;

    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }
    public bool IsRunning { get; internal set; }


    public TimeSpan TotalTime { get; set; }

    public bool NeedsEstimate { get; init; }
    public TimeSpan? TimeRemaining { get; set; }

    public string? Accomplishments { get; set; }
    public string? Impediments { get; set; }
    public string? Learnings { get; set; }
    public string? OtherComments { get; set; }

    public override string ToString() =>
        $"{ParentActivitySummary.ParentProjectSummary.ProjectName} {ParentActivitySummary.ActivityName} {Task?.Title} for {ParentActivitySummary.ParentProjectSummary.ParentDay.Date:yyyy-MM-dd}";
}