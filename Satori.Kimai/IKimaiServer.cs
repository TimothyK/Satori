using Satori.Kimai.Models;

namespace Satori.Kimai;

public interface IKimaiServer
{
    bool Enabled { get; }
    Uri BaseUrl { get; }

    Task<TimeEntry[]> GetTimeSheetAsync(TimeSheetFilter filter);

    Task<User> GetMyUserAsync();

    /// <summary>
    /// Mark a time entry as exported
    /// </summary>
    /// <param name="id">Kimai ID of the time entry record</param>
    /// <returns></returns>
    Task ExportTimeSheetAsync(int id);

    Task<DateTimeOffset> StopTimerAsync(int id);

    Task UpdateTimeEntryDescriptionAsync(int id, string description);

    Task<TimeEntry> CreateTimeEntryAsync(TimeEntryForCreate entry);
}