using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.Pages;

public partial class EditWorkItem
{
}

public class WorkItemCommentViewModel(WorkItem? workItem, TimeEntry[] timeEntries)
    : CommentViewModel(
        CommentType.WorkItem
        , CommentType.WorkItem.GetComment(timeEntries.FirstOrDefault(entry => entry.Task == workItem))
        , timeEntries
        , timeEntries.Where(entry => entry.Task == workItem)
    )
{
    public event EventHandler? WorkItemActivated;

    protected virtual void OnWorkItemActivated()
    {
        WorkItemActivated?.Invoke(this, EventArgs.Empty);
    }

    public string? WorkItemIdToAdd { get; set; }
    public string? WorkItemIdToAddValidationErrorMessage { get; set; }

    /// <summary>
    /// The ToggleActive button also serves as the Add button to add a new work item.
    /// </summary>
    /// <param name="timeEntry"></param>
    public override void ToggleActive(TimeEntry timeEntry)
    {
        if (WorkItem == null)
        {
            if (int.TryParse(WorkItemIdToAdd, out var workItemId) && workItemId > 0)
            {
                //TODO: Lookup work item in AzDO
            }
            else
            {
                WorkItemIdToAddValidationErrorMessage = "Value must be positive integer";
                return;
            }
        }

        base.ToggleActive(timeEntry);
            
        if (IsActive[timeEntry])
        {
            OnWorkItemActivated();
        }
    }

    public WorkItem? WorkItem { get; set; } = workItem;
}