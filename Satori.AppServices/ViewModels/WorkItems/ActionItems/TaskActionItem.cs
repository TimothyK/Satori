using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.AppServices.ViewModels.WorkItems.ActionItems;

public class TaskActionItem(WorkItem task) : ActionItem(GetActionDescription(task), task.AssignedTo)
{
    private static string GetActionDescription(WorkItem task)
    {
        return task.State == ScrumState.ToDo ? "Start"
            : task.State == ScrumState.InProgress ? "Resume"
            : "Work on";
    }

    public WorkItem Task { get; set; } = task;
}