using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.ViewModels.Abstractions;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.Sprints;
using KimaiProject = Satori.Kimai.ViewModels.Project;
using KimaiActivity = Satori.Kimai.ViewModels.Activity;

namespace Satori.AppServices.ViewModels.WorkItems;

public class WorkItem
{
    public int Id { get; set; }
    public int Rev { get; set; }
    public string? Title { get; init; }
    /// <summary>
    /// Azure DevOps Team Project name
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is often used in the URL after the Organization name.
    /// e.g. https://dev.azure.com/{Organization}/{ProjectName}/...
    /// </para>
    /// </remarks>
    public required string ProjectName { get; init; }
    public required string Url { get; init; }
    public required string ApiUrl { get; init; }
    public required Person AssignedTo { get; init; }
    public List<Person> WithPeople { get; set; } = [];
    public List<ActionItem> ActionItems { get; set; } = [];
    public required Person CreatedBy { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public string? AreaPath { get; init; }
    public string? IterationPath { get; init; }
    public required WorkItemType Type { get; init; }
    public required ScrumState State { get; set; }
    public TriageState? Triage { get; init; }
    public DateTimeOffset? TargetDate { get; init; }

    public string TargetDateCssClass
    {
        get
        {
            if (TargetDate == null)
            {
                return "hidden";
            }

            if (ScrumState.Done <= State)
            {
                return "target-date-normal";
            }

            if (TargetDate.Value < DateTimeOffset.UtcNow)
            {
                return "target-date-overdue";
            }

            if (TargetDate.Value < DateTimeOffset.UtcNow.AddDays(3))
            {
                return "target-date-soon";
            }

            return "target-date-normal";
        }
    }

    public bool Blocked { get; init; }
    public required List<string> Tags { get; init; }
    public TimeSpan? OriginalEstimate { get; set; }
    public TimeSpan? CompletedWork { get; set; }
    public TimeSpan? RemainingWork { get; set; }

    public KimaiProject? KimaiProject { get; set; }
    public KimaiActivity? KimaiActivity { get; set; }


    public WorkItem? Parent { get; set; }
    public List<WorkItem> Children { get; init; } = [];
    public List<WorkItem> Predecessors { get; init; } = [];
    public List<WorkItem> Successors { get; init; } = [];

    public List<PullRequest> PullRequests { get; init; } = [];
    public Sprint? Sprint { get; set; }
    public int? SprintPriority { get; set; }
    public double AbsolutePriority { get; internal set; }

    public override string ToString() => $"D#{Id} {Title}";

    public string ToKimaiDescription() =>
        Parent == null || Parent.Type.IsNotIn(WorkItemType.BoardTypes)
            ? $"D#{Id} {Title}"
            : $"D#{Parent.Id} {Parent.Title} » D#{Id} {Title}";

    public string? StatusLabel
    {
        get
        {
            if (State == ScrumState.New)
            {
                return Triage == TriageState.Pending ? "Triage Pending"
                    : Triage == TriageState.MoreInfo ? "Triage waiting for info"
                    : Triage == TriageState.InfoReceived ? "Triaging"
                    : Triage == TriageState.Triaged ? "Triaged, waiting for approval"
                    : "New";
            }
            if (State == ScrumState.Open)
            {
                return "Open";
            }
            if (State == ScrumState.ToDo)
            {
                return "⏳ To Do" + (
                    RemainingWork != null ? $" (~{RemainingWork.Value.TotalHours:0.0} hr)"
                    : OriginalEstimate != null ? $" (~{OriginalEstimate.Value.TotalHours:0.0} hr)" : string.Empty
                );
            }

            if (State == ScrumState.InProgress)
            {
                return "⌛ In Progress" + (
                    RemainingWork != null ? $" ({RemainingWork.Value.TotalHours:0.0} hr)"
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
            if (State == ScrumState.Done)
            {
                return "✔️ Done";
            }
            if (State == ScrumState.Closed)
            {
                return "✔️ Closed";
            }

            return null;
        }
    }

    public string? StatusCssClass =>
        State == ScrumState.Done ? "status-done"
        : State == ScrumState.InProgress ? "status-in-progress"
        : State == ScrumState.ToDo ? "status-to-do"
        : State == ScrumState.Closed ? "status-done"
        : null;
}