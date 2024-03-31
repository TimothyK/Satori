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
    public required ScrumState State { get; init; }
    public List<string> Tags { get; init; }
    public TimeSpan? OriginalEstimate { get; set; }
    public TimeSpan? CompletedWork { get; set; }
    public TimeSpan? RemainingWork { get; set; }
    public string? ProjectCode { get; init; }

    public WorkItem? Parent { get; set; }
    public List<WorkItem> Children { get; } = [];
    public Sprint? Sprint { get; set; }
    public int? SprintPriority { get; set; }
    public double AbsolutePriority { get; set; }

    public string? StatusLabel
    {
        get
        {
            if (State == ScrumState.Done)
            {
                return "✔️ Done";
            }
            if (State == ScrumState.InProgress)
            {
                return "⌛ In Progress" + (
                    RemainingWork != null ? $" ({RemainingWork.Value.TotalHours:0.0} hr)"
                        : OriginalEstimate != null ? $" (~{OriginalEstimate.Value.TotalHours:0.0} hr)" : string.Empty
                        );
            }
            if (State == ScrumState.ToDo)
            {
                return "⏳ To Do" + (
                    RemainingWork != null ? $" (~{RemainingWork.Value.TotalHours:0.0} hr)"
                    : OriginalEstimate != null ? $" (~{OriginalEstimate.Value.TotalHours:0.0} hr)" : string.Empty
                );
            }
            if (State == ScrumState.Approved)
            {
                return "Approved by Product Owner";
            }
            if (State == ScrumState.Committed)
            {
                return "Committed by Team";
            }
            return null;
        }
    }

    public string? StatusCssClass =>
        State == ScrumState.Done ? "status-done"
        : State == ScrumState.InProgress ? "status-in-progress"
        : State == ScrumState.ToDo ? "status-to-do"
        : null;
}