namespace Satori.AppServices.ViewModels.DailyStandUps;

public interface ISummary
{
    IEnumerable<TimeEntry> TimeEntries { get; }
    TimeSpan TotalTime { get; set; }
    bool AllExported { get; }
    bool CanExport { get; }
    bool IsRunning { get; }
}