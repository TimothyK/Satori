namespace Satori.AppServices.ViewModels.DailyStandUps;

public class PeriodSummary : ISummary
{
    public static PeriodSummary CreateEmpty() => new();

    public List<DaySummary> Days { get; } = [];
    public IEnumerable<TimeEntry> TimeEntries => Days.SelectMany(d => d.TimeEntries);
    public TimeSpan TotalTime { get; set; }
    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }
    public bool IsRunning { get; internal set; }
}