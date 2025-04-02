namespace Satori.AppServices.ViewModels.WorkItems;

public class ReorderRequest(
    WorkItem[] allWorkItems,
    int[] workItemIdsToMove,
    RelativePosition position,
    WorkItem? target)
{
    public WorkItem[] AllWorkItems { get; } = allWorkItems;

    public int[] WorkItemIdsToMove { get; } = workItemIdsToMove;

    public RelativePosition RelativeToTarget { get; set; } = position;
    public WorkItem? Target { get; } = target;

    public override string ToString()
    {
        var relativeToMsg = Target == null
            ? RelativeToTarget == RelativePosition.Below ? "Bottom" : "Top"
            : $"{RelativeToTarget.ToString()} {Target.Id}";

        return $"ReorderRequest to move {string.Join(",", WorkItemIdsToMove)} to {relativeToMsg}";
    }
}

public enum RelativePosition
{
    Above,
    Below
}
