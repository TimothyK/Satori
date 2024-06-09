namespace Satori.AppServices.ViewModels.DailyStandUps;

public class ActivitySummary
{
    public int ActivityId { get; init; }
    public required string ActivityName { get; init; }

    public required ProjectSummary ParentProjectSummary { get; init; }

    public string? Comment { get; init; }

    public required TimeEntry[] TimeEntries { get; set; }

    public TimeSpan TotalTime { get; internal init; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }

    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; }

    public required TaskSummary[] TaskSummaries { get; set; }
    public string? Accomplishments { get; set; }
    public string? Impediments { get; set; }
    public string? Learnings { get; set; }
    public string? OtherComments { get; set; }

}