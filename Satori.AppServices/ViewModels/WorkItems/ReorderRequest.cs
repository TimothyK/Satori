namespace Satori.AppServices.ViewModels.WorkItems;

public class ReorderRequest(
    WorkItem[] allWorkItems,
    WorkItem[] workItemsToMove,
    RelativePosition position,
    WorkItem? target)
{
    public ReorderRequest(WorkItem[] allWorkItems,
        WorkItem workItemToMove,
        RelativePosition position,
        WorkItem? target) : this(allWorkItems, [workItemToMove], position, target)
    {
    }

    public WorkItem[] AllWorkItems { get; } = allWorkItems;

    public WorkItem[] WorkItemsToMove { get; } = workItemsToMove;

    public RelativePosition RelativeToTarget { get; set; } = position;
    public WorkItem? Target { get; } = target;

    public override string ToString()
    {
        var relativeToMsg = Target == null
            ? RelativeToTarget == RelativePosition.Below ? "Bottom" : "Top"
            : $"{RelativeToTarget.ToString()} {Target.Id}";

        return $"ReorderRequest to move {string.Join(",", WorkItemsToMove.Select(wi => wi.Id))} to {relativeToMsg}";
    }
}

public enum RelativePosition
{
    Above,
    Below
}
