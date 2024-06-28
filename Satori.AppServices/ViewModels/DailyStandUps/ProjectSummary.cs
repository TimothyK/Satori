namespace Satori.AppServices.ViewModels.DailyStandUps;

public class ProjectSummary
{
    public int ProjectId { get; init; }
    public required string ProjectName { get; init; }

    public required StandUpDay ParentDay { get; init; }

    public int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerAcronym { get; init; }
    public Uri? CustomerUrl { get; init; }

    public required ActivitySummary[] Activities { get; set; }
    
    public TimeSpan TotalTime { get; internal init; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }

    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; }
}