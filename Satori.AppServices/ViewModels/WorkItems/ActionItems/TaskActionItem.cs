using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.AppServices.ViewModels.WorkItems.ActionItems;

public class TaskActionItem(WorkItem task) 
    : ActionItem(GetActionDescription(task), GetPerson(task))
{
    private static string GetActionDescription(WorkItem task)
    {
        return task.AssignedTo == Person.Empty ? "Assign"
            : task.State == ScrumState.ToDo ? "Start"
            : task.State == ScrumState.InProgress ? "Resume"
            : "Work on";
    }

    private static Person GetPerson(WorkItem task)
    {
        if (task.AssignedTo == Person.Empty)
        {
            return task.Parent?.AssignedTo ?? Person.Empty;
        }
        return task.AssignedTo;
    }

    public WorkItem Task { get; set; } = task;
}