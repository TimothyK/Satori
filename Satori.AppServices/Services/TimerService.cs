using Satori.AppServices.Services.Abstractions;
using Satori.Kimai;
using Satori.Kimai.Models;

namespace Satori.AppServices.Services;

/// <summary>
/// Service to create a new Kimai time entry, typically with no end time (i.e. a running timer)
/// </summary>
public class TimerService(
    IKimaiServer kimai
    , UserService userService
    , IAlertService alertService
)
{
    /// <summary>
    /// Restarts and existing timer
    /// </summary>
    /// <param name="timeEntryIds">Id of the Kimai Time Entry</param>
    /// <returns></returns>
    public async Task RestartTimerAsync(params int[] timeEntryIds)
    {
        List<TimeEntryCollapsed> entries = [];
        foreach (var id in timeEntryIds)
        {
            entries.Add(await kimai.GetTimeEntryAsync(id));
        }

        var entry = new TimeEntryForCreate
        {
            User = entries.Select(t => t.User).Distinct().Single(),
            Activity = entries.Select(t => t.Activity).Distinct().Single(),
            Project = entries.Select(t => t.Project).Distinct().Single(),
            Begin = DateTimeOffset.Now,
        };
        await kimai.CreateTimeEntryAsync(entry);
    }
}