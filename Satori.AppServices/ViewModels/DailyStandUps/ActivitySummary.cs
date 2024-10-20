namespace Satori.AppServices.ViewModels.DailyStandUps;

public class ActivitySummary
{
    public int ActivityId { get; init; }
    public required string ActivityName { get; init; }

    public required ProjectSummary ParentProjectSummary { get; init; }

    public string? ActivityDescription { get; init; }

    public required TimeEntry[] TimeEntries { get; set; }

    public TimeSpan TotalTime { get; internal init; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }

    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; } = true;

    public required TaskSummary[] TaskSummaries { get; set; }
    [Obsolete]
    public string? Accomplishments { get; set; }
    [Obsolete]
    public string? Impediments { get; set; }
    [Obsolete]
    public string? Learnings { get; set; }
    [Obsolete]
    public string? OtherComments { get; set; }

}