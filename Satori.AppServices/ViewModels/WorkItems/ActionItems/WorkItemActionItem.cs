using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.AppServices.ViewModels.WorkItems.ActionItems;
public abstract class WorkItemActionItem(WorkItem workItem, string actionDescription, params Person[] people)
    : ActionItem(actionDescription, people.DefaultIfEmpty(workItem.AssignedTo).ToArray())
{
    public WorkItem WorkItem { get; set; } = workItem ?? throw new ArgumentNullException(nameof(workItem));
}