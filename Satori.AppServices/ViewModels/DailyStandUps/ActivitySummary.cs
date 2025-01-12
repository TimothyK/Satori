
namespace Satori.AppServices.ViewModels.DailyStandUps;

public class ActivitySummary : ISummary
{
    public int ActivityId { get; init; }
    public required string ActivityName { get; init; }

    public required ProjectSummary ParentProjectSummary { get; init; }

    public string? ActivityDescription { get; init; }
    public bool IsActive { get; init; }

    public required TimeEntry[] TimeEntries { get; set; }
    IEnumerable<TimeEntry> ISummary.TimeEntries => TimeEntries;

    public TimeSpan TotalTime { get; set; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }
    public bool IsRunning { get; internal set; }


    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; } = false;

    public required TaskSummary[] TaskSummaries { get; set; }
    public string? Accomplishments { get; set; }
    public string? Impediments { get; set; }
    public string? Learnings { get; set; }
    public string? OtherComments { get; set; }
}