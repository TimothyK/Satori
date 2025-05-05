using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.AppServices.ViewModels.WorkItems.ActionItems;

public class TaskActionItem(WorkItem task) : ActionItem(GetMessage(task), task.AssignedTo)
{
    private static string GetMessage(WorkItem task)
    {
        var action = task.State == ScrumState.ToDo ? "started"
            : task.State == ScrumState.InProgress ? "resumed"
            : "worked on";
        return $"This task can be {action}";
    }

    public WorkItem Task { get; set; } = task;
}