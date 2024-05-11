namespace Satori.AppServices.ViewModels.WorkItems;

public class ReorderRequest(WorkItem[] allWorkItems, int[] workItemIdsToMove, bool targetBelow, WorkItem? target)
{
    public WorkItem[] AllWorkItems { get; } = allWorkItems;

    public int[] WorkItemIdsToMove { get; } = workItemIdsToMove;

    public bool TargetBelow { get; } = targetBelow;
    public WorkItem? Target { get; } = target;
}