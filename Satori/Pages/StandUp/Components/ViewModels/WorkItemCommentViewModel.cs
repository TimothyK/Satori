using Microsoft.VisualStudio.Threading;
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

        if (IsActive[timeEntry])
        {
            await OnWorkItemActivatedAsync();
        }
    }

    public WorkItem? WorkItem { get; set; }
}