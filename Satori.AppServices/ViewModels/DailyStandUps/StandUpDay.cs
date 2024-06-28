namespace Satori.AppServices.ViewModels.DailyStandUps;

public class StandUpDay
{
    public DateOnly Date { get; internal init; }

    public required ProjectSummary[] Projects { get; set; }

    public TimeSpan TotalTime { get; internal init; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }

    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; }

}