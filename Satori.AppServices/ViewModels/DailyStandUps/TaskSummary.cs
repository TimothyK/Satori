using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.AppServices.ViewModels.DailyStandUps;

public class TaskSummary
{
    public WorkItem? Task { get; init; }

    public required ActivitySummary ParentActivitySummary { get; init; }

    public required TimeEntry[] TimeEntries { get; set; }

    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; } = true;

    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }
    public bool IsRunning { get; internal set; }


    public TimeSpan TotalTime { get; internal init; }

    public bool NeedsEstimate { get; init; }
    public TimeSpan? TimeRemaining { get; init; }

    public string? Accomplishments { get; set; }
    public string? Impediments { get; set; }
    public string? Learnings { get; set; }
    public string? OtherComments { get; set; }
}