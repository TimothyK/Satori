using Satori.AppServices.ViewModels.Sprints;

namespace Satori.AppServices.ViewModels.WorkItems;

public class WorkItem
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public required string Url { get; init; }
    public Person? AssignedTo { get; init; }
    public required Person CreatedBy { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public string? IterationPath { get; init; }
    public required WorkItemType Type { get; init; }
    public required string State { get; init; }
    public string? ProjectCode { get; init; }

    public WorkItem? Parent { get; set; }
    public List<WorkItem> Children { get; } = [];
    public Sprint? Sprint { get; set; }
    public int SprintPriority { get; set; }
    public int AbsolutePriority { get; set; }
}