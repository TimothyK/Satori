using CodeMonkeyProjectiles.Linq;
using Microsoft.VisualStudio.Threading;
using Satori.AppServices.Extensions;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Pages.StandUp.Components.ViewModels.Models;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.Pages.StandUp.Components.ViewModels;

public class WorkItemCommentViewModel : CommentViewModel
{
    private readonly PeriodSummary _period;

    #region Constructors

    private WorkItemCommentViewModel(WorkItem? workItem, TimeEntry[] allTimeEntries, IEnumerable<TimeEntry> activeTimeEntries, PeriodSummary period)
        : base(
            CommentType.WorkItem
            , CommentType.WorkItem.GetComment(allTimeEntries.FirstOrDefault(entry => entry.Task == workItem))
            , allTimeEntries
            , activeTimeEntries)
    {
        _period = period;


        if (workItem != null)
        {
            SetWorkItem(workItem);
        }
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

    #region WorkItem

    public WorkItem? WorkItem { get; private set; }

    public void SetWorkItem(WorkItem workItem)
    {
        WorkItem = workItem;
        Text = KimaiDescription;

        State = workItem.State;

        UnexportedTime =  
            _period.Days
                .Where(day => !day.AllExported)
                .SelectMany(day => day.TimeEntries)
                .Except(EntriesUnderEdit)
                .Where(entry => entry.Task?.Id == workItem.Id)
                .Where(entry => !entry.Exported)
                .Select(entry => entry.TotalTime)
                .Sum();

        SetTimeRemaining();
        TimeRemainingInput = TimeRemaining?.TotalHours.ToNearest(0.1) ?? 0.0;
        OnHasChanged();
    }

    public override string? KimaiDescription => 
        WorkItem == null ? null
        : WorkItem.Parent == null ? $"D#{WorkItem.Id} {WorkItem.Title}"
        : $"D#{WorkItem.Parent.Id} {WorkItem.Parent.Title} » D#{WorkItem.Id} {WorkItem.Title}";

    public IEnumerable<WorkItem> Children
    {
        get
        {
            if (WorkItem == null || WorkItem.Type == WorkItemType.Task)
            {
                return [];
            }

            return WorkItem.Children
                .Where(t => t.Type != WorkItemType.Task || (t.AssignedTo == Person.Me && t.IterationPath == WorkItem.IterationPath))
                .Where(t => t.Type == WorkItemType.Task || t.State < ScrumState.Done);
        }
    }

    public ExpandableCssClass IsAddChildExpanded { get; set; } = ExpandableCssClass.Collapsed;

    #endregion WorkItem

    #region State

    public ScrumState State { get; set; } = ScrumState.Unknown;

    public void SetState(ScrumState state)
    {
        State = state;
        SetTimeRemaining();
        OnHasChanged();
    }

    #endregion State

    #region TimeRemaining

    private double _timeRemainingInput;

    public double TimeRemainingInput
    {
        get => _timeRemainingInput;
        set
        {
            _timeRemainingInput = value;
            OnHasChanged();
        }
    }

    private IEnumerable<TimeEntry> EntriesUnderEdit => IsActive.Keys;

    /// <summary>
    /// Time against <see cref="WorkItem"/> that is not exported yet and not under edit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a constant amount that should be removed from RemainingWork read from AzDO.
    /// </para>
    /// </remarks>
    public TimeSpan UnexportedTime { get; private set; } = TimeSpan.Zero;

    public TimeSpan? TimeRemaining { get; set; }

    private void SetTimeRemaining()
    {
        if (State.IsNotIn(ScrumState.ToDo, ScrumState.InProgress))
        {
            TimeRemaining = null;
            return;
        }

        var updateInput = Math.Abs((TimeRemaining?.TotalHours ?? 0.0) - TimeRemainingInput) < 0.1;
        TimeRemaining = WorkItem?.RemainingWork - UnexportedTime - SelectedTime;
        if (updateInput)
        {
            TimeRemainingInput = TimeRemaining?.TotalHours.ToNearest(0.1) ?? 0.0;
        }
    }

    public TimeSpan SelectedTime =>
        EntriesUnderEdit
            .Where(entry => IsActive[entry])
            .Select(entry => entry.TotalTime)
            .Sum();

    #endregion TimeRemaining

    #region Create New Task

    public string? NewTaskTitleInput { get; set; }
    public string? NewTaskTitleInputValidationErrorMessage { get; set; }
    
    #endregion Create New Task

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

    #region Validation

    protected override void OnHasChanged()
    {
        var stateValidationMessage = string.Empty;
        var timeRemainingInputValidationMessage = string.Empty;
        var isTaskMine = false;

        try
        {
            if (WorkItem == null || WorkItem.Type != WorkItemType.Task)
            {
                return;
            }

            stateValidationMessage = State == ScrumState.ToDo 
                ? "It is not recommended that time is entered against tasks that are still 'To Do'.  Change the state to In Progress" 
                : string.Empty;

            if (State.IsIn(ScrumState.ToDo, ScrumState.InProgress) && TimeRemainingInput <= 0.0)
            {
                timeRemainingInputValidationMessage = "Enter a current estimate for the time remaining, or mark the task as Done";
            }
            else
            {
                timeRemainingInputValidationMessage = string.Empty;
            }

            isTaskMine = WorkItem.AssignedTo == Person.Me;

            base.OnHasChanged();
        }
        finally
        {
            StateValidationMessage = stateValidationMessage;
            TimeRemainingInputValidationMessage = timeRemainingInputValidationMessage;
            IsTaskMine = isTaskMine;
            base.OnHasChanged();
        }
    }

    public bool AttentionRequired => 
        !string.IsNullOrEmpty(StateValidationMessage)
        || !string.IsNullOrEmpty(TimeRemainingInputValidationMessage)
        || !IsTaskMine;

    public string StateValidationMessage { get; set; } = string.Empty;
    public string TimeRemainingInputValidationMessage { get; set; } = string.Empty;
    public bool IsTaskMine { get; set; }

    #endregion Validation

}