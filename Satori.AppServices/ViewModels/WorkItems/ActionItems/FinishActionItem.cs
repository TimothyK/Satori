namespace Satori.AppServices.ViewModels.WorkItems.ActionItems;

public class FinishActionItem : WorkItemActionItem
{
    internal FinishActionItem(WorkItem workItem) : base(workItem, $"Finish this {workItem.Type}")
    {
    }
}