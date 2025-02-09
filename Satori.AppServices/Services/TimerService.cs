using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.CommentParsing;
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
        try
        {
            await RestartTimerUnsafeAsync(timeEntryIds);
        }
        catch (Exception ex)
        {
            alertService.BroadcastAlert(ex);
        }
    }

    private async Task RestartTimerUnsafeAsync(int[] timeEntryIds)
    {
        var startTime = await StopRunningTimeEntryAsync() ?? DateTimeOffset.Now;

        List<TimeEntryCollapsed> entries = [];
        foreach (var id in timeEntryIds)
        {
            entries.Add(await kimai.GetTimeEntryAsync(id));
        }

        var me = await userService.GetCurrentUserAsync();

        var descriptions = string.Join('\n', entries.Select(t => t.Description));
        var comments = CommentParser.Parse(descriptions);

        var entry = new TimeEntryForCreate
        {
            User = me.KimaiId ?? throw new InvalidOperationException("Kimai UserId is unknown"),
            Activity = entries.SelectDistinctSingle(t => t.Activity, "activities"),
            Project = entries.SelectDistinctSingle(t => t.Project, "projects"),
            Begin = startTime,
            Description = comments.Join(type => type.IsNotIn(CommentType.ScrumTypes)),
        };
        await kimai.CreateTimeEntryAsync(entry);
    }

    /// <summary>
    /// Stops the active running time entry for the current user
    /// </summary>
    /// <returns>
    /// The End time that Kimai assigns to the running time entry.
    /// Returns null if there wasn't an active time entry
    /// </returns>
    private async Task<DateTimeOffset?> StopRunningTimeEntryAsync()
    {
        var filter = new TimeSheetFilter
        {
            IsRunning = true
        };

        var timeSheet = await kimai.GetTimeSheetAsync(filter);
        if (timeSheet.None())
        {
            return null;  //No active time entry to stop
        }

        return await kimai.StopTimerAsync(timeSheet.Single().Id);
    }
}