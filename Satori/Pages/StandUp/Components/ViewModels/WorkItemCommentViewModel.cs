using CodeMonkeyProjectiles.Linq;
using Microsoft.VisualStudio.Threading;
using Satori.AppServices.Extensions;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Pages.StandUp.Components.ViewModels.Models;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.Pages.StandUp.Components.ViewModels;

public class WorkItemCommentViewModel : CommentViewModel
{
    #region Constructors

    private WorkItemCommentViewModel(WorkItem? workItem, TimeEntry[] allTimeEntries, IEnumerable<TimeEntry> activeTimeEntries, PeriodSummary period)
        : base(
            CommentType.WorkItem
            , CommentType.WorkItem.GetComment(allTimeEntries.FirstOrDefault(entry => entry.Task == workItem))
            , allTimeEntries
            , activeTimeEntries)
    {
        WorkItem = workItem;
        State = workItem?.State ?? ScrumState.Unknown;

        UnexportedTime = workItem == null ? TimeSpan.Zero 
            : period.Days
                .Where(day => !day.AllExported)
                .SelectMany(day => day.TimeEntries)
                .Except(EntriesUnderEdit)
                .Where(entry => entry.Task?.Id == workItem.Id)
                .Where(entry => !entry.Exported)
                .Select(entry => entry.TotalTime)
                .Sum();
        SetTimeRemaining();
        TimeRemainingInput = TimeRemaining?.TotalHours.ToNearest(0.1) ?? 0.0;
    }

    public static WorkItemCommentViewModel FromNew(TimeEntry[] timeEntries, PeriodSummary period)
    {
        var vm = new WorkItemCommentViewModel(workItem: null, timeEntries, [], period);
        return vm;
    }
    public static WorkItemCommentViewModel FromExisting(WorkItem workItem, TimeEntry[] timeEntries, PeriodSummary period)
    {
        var vm = new WorkItemCommentViewModel(workItem, timeEntries, timeEntries.Where(entry => entry.Task == workItem), period);
        return vm;
    }

    #endregion

    public WorkItem? WorkItem { get; set; }

    #region State

    public ScrumState State { get; set; }

    public void SetState(ScrumState state)
    {
        State = state;
        SetTimeRemaining();
    }

    public double TimeRemainingInput { get; set; }

    #endregion State

    #region TimeRemaining

    private IEnumerable<TimeEntry> EntriesUnderEdit => IsActive.Keys;

    /// <summary>
    /// Time against <see cref="WorkItem"/> that is not exported yet and not under edit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a constant amount that should be removed from RemainingWork read from AzDO.
    /// </para>
    /// </remarks>
    private TimeSpan UnexportedTime { get; }

    public TimeSpan? TimeRemaining { get; set; }

    private void SetTimeRemaining()
    {
        if (State.IsNotIn(ScrumState.ToDo, ScrumState.InProgress))
        {
            TimeRemaining = null;
            return;
        }

        var selectedTime = EntriesUnderEdit
            .Where(entry => IsActive[entry])
            .Select(entry => entry.TotalTime)
            .Sum();

        var updateInput = Math.Abs((TimeRemaining?.TotalHours ?? 0.0) - TimeRemainingInput) < 0.1;
        TimeRemaining = WorkItem?.RemainingWork - UnexportedTime - selectedTime;
        if (updateInput)
        {
            TimeRemainingInput = TimeRemaining?.TotalHours.ToNearest(0.1) ?? 0.0;
        }
    }

    #endregion TimeRemaining

    #region Activation

    public event AsyncEventHandler<CancelEventArgs>? WorkItemActivatingAsync;
    public event AsyncEventHandler? WorkItemActivatedAsync;

    protected virtual async Task<CancelEventArgs> OnWorkItemActivatingAsync()
    {
        var e = new CancelEventArgs();
        if (WorkItemActivatingAsync != null)
        {
            await WorkItemActivatingAsync.InvokeAsync(this, e);
        }
        return e;
    }

    protected virtual async Task OnWorkItemActivatedAsync()
    {
        if (WorkItemActivatedAsync != null)
        {
            await WorkItemActivatedAsync.InvokeAsync(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// The ToggleActive button also serves as the Add button to add a new work item.
    /// The Add functionality is handled by <see cref="OnWorkItemActivatingAsync"/>.
    /// </summary>
    /// <param name="timeEntry"></param>
    public override async Task ToggleActiveAsync(TimeEntry timeEntry)
    {
        if (!IsActive[timeEntry])
        {
            var e = await OnWorkItemActivatingAsync();
            if (e.Cancel)
            {
                return;
            }
        }

        await base.ToggleActiveAsync(timeEntry);
        SetTimeRemaining();

        if (IsActive[timeEntry])
        {
            await OnWorkItemActivatedAsync();
        }
    }

    #endregion
}