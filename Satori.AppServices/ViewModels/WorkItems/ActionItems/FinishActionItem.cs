using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.AppServices.ViewModels.WorkItems.ActionItems;

public class FinishActionItem : ActionItem
{
    internal FinishActionItem(WorkItem workItem) : base($"This {workItem.Type} can be marked as Done or have more tasks added", workItem.AssignedTo)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; set; }

}