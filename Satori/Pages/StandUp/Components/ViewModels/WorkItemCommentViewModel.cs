using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Pages.StandUp.Components.ViewModels.Models;

namespace Satori.Pages.StandUp.Components.ViewModels;

public class WorkItemCommentViewModel : CommentViewModel
{
    private WorkItemCommentViewModel(WorkItem? workItem, TimeEntry[] allTimeEntries, IEnumerable<TimeEntry> activeTimeEntries)
        : base(
            CommentType.WorkItem
            , CommentType.WorkItem.GetComment(allTimeEntries.FirstOrDefault(entry => entry.Task == workItem))
            , allTimeEntries
            , activeTimeEntries)
    {
        WorkItem = workItem;
    }

    public static WorkItemCommentViewModel FromNew(TimeEntry[] timeEntries)
    {
        var vm = new WorkItemCommentViewModel(null, timeEntries, []);
        return vm;
    }
    public static WorkItemCommentViewModel FromExisting(WorkItem workItem, TimeEntry[] timeEntries)
    {
        var vm = new WorkItemCommentViewModel(workItem, timeEntries, timeEntries.Where(entry => entry.Task == workItem));
        return vm;
    }

    public event EventHandler<CancelEventArgs>? WorkItemActivating;
    public event EventHandler? WorkItemActivated;

    protected virtual CancelEventArgs OnWorkItemActivating()
    {
        var e = new CancelEventArgs();
        WorkItemActivating?.Invoke(this, e);
        return e;
    }

    protected virtual void OnWorkItemActivated()
    {
        WorkItemActivated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The ToggleActive button also serves as the Add button to add a new work item.
    /// The Add functionality is handled by <see cref="OnWorkItemActivating"/>.
    /// </summary>
    /// <param name="timeEntry"></param>
    public override void ToggleActive(TimeEntry timeEntry)
    {
        if (!IsActive[timeEntry])
        {
            var e = OnWorkItemActivating();
            if (e.Cancel)
            {
                return;
            }
        }

        base.ToggleActive(timeEntry);

        if (IsActive[timeEntry])
        {
            OnWorkItemActivated();
        }
    }

    public WorkItem? WorkItem { get; set; }
}