namespace Satori.AppServices.ViewModels.DailyStandUps;

public class DaySummary : ISummary
{
    public DateOnly Date { get; internal init; }

    public required PeriodSummary ParentPeriod { get; init; }
    public required ProjectSummary[] Projects { get; set; }
    public IEnumerable<TimeEntry> TimeEntries => Projects.SelectMany(p => p.TimeEntries);

    public TimeSpan TotalTime { get; set; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }
    public bool IsRunning { get; internal set; }

    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; }

    public override string ToString() => $"Day {Date:yyyy-MM-dd}";
}