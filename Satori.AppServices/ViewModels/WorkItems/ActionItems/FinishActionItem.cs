using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.AppServices.ViewModels.WorkItems.ActionItems;

public class FinishActionItem : ActionItem
{
    internal FinishActionItem(WorkItem workItem) : base($"Finish this {workItem.Type}", workItem.AssignedTo)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; set; }

}