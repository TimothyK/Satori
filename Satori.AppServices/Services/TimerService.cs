using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Models;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.CommentParsing;
using Satori.Kimai;
using Satori.Kimai.Models;
using Satori.TimeServices;

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
    #region RestartTimerAsync

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
        var startTime = await StopRunningTimeEntryAsync() ?? DateTimeOffset.Now.TruncateSeconds();

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

    #endregion RestartTimerAsync

    #region GetActivelyTimedWorkItemIdsAsync

    private static readonly Cache<IReadOnlyCollection<int>> ActivelyTimedWorkItemIdsCache = new(new TimeServer());

    /// <summary>
    /// Returns the Work Item IDs of the tasks that are actively being timed in Kimai.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This will return all active work item IDs for all users in Kimai.
    /// The current user must have permissions to view other user's time sheets in Kimai.
    /// If not, only the current user's active time entries will be returned.
    /// </para>
    /// <para>
    /// Only the WorkItemIDs are returned.
    /// This doesn't return which user is actively working which work item.
    /// However, it can be assumed that the user assigned to the Azure DevOps Task is the person working on it.
    /// </para>
    /// </remarks>
    /// <returns>Azure DevOps Work Item IDs</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<IReadOnlyCollection<int>> GetActivelyTimedWorkItemIdsAsync(CachingAlgorithm cachingAlgorithm = CachingAlgorithm.UseCache)
    {
        var cache = ActivelyTimedWorkItemIdsCache;

        await cache.Semaphore.WaitAsync();
        try
        {
            if (!cache.IsExpired && cachingAlgorithm == CachingAlgorithm.UseCache)
            {
                return cache.Value ?? throw new InvalidOperationException("Cache value should not be null if cache is not expired");
            }

            return cache.Value = await GetActivelyTimedWorkItemIdsFromKimaiAsync();
        }
        finally
        {
            cache.Semaphore.Release();
        }
    }

    private async Task<IReadOnlyCollection<int>> GetActivelyTimedWorkItemIdsFromKimaiAsync()
    {
        var filter = new TimeSheetFilter()
        {
            IsRunning = true,
            AllUsers = true,
        };

        var timeSheets = await kimai.GetTimeSheetAsync(filter);
        return timeSheets
            .SelectMany(timeEntry => CommentParser.Parse(timeEntry.Description))
            .OfType<WorkItemComment>()
            .SelectMany(comment => comment.WorkItems.Select(workItem => workItem.Id))
            .Distinct()
            .ToList().AsReadOnly();
    }

    #endregion GetActivelyTimedWorkItemIdsAsync

}