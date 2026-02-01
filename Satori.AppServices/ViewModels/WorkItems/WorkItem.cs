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
    public required WorkItemTypeObsolete Type { get; init; }
    public required ScrumStateObsolete State { get; set; }
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

            if (ScrumStateObsolete.Done <= State)
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
        Parent == null || Parent.Type.IsNotIn(WorkItemTypeObsolete.BoardTypes)
            ? $"D#{Id} {Title}"
            : $"D#{Parent.Id} {Parent.Title} » D#{Id} {Title}";

    public string? StatusLabel
    {
        get
        {
            if (State == ScrumStateObsolete.New)
            {
                return Triage == TriageState.Pending ? "Triage Pending"
                    : Triage == TriageState.MoreInfo ? "Triage waiting for info"
                    : Triage == TriageState.InfoReceived ? "Triaging"
                    : Triage == TriageState.Triaged ? "Triaged, waiting for approval"
                    : "New";
            }
            if (State == ScrumStateObsolete.Open)
            {
                return "Open";
            }
            if (State == ScrumStateObsolete.ToDo)
            {
                return "⏳ To Do" + (
                    RemainingWork != null ? $" (~{RemainingWork.Value.TotalHours:0.0} hr)"
                    : OriginalEstimate != null ? $" (~{OriginalEstimate.Value.TotalHours:0.0} hr)" : string.Empty
                );
            }

            if (State == ScrumStateObsolete.InProgress)
            {
                return "⌛ In Progress" + (
                    RemainingWork != null ? $" ({RemainingWork.Value.TotalHours:0.0} hr)"
                        : OriginalEstimate != null ? $" (~{OriginalEstimate.Value.TotalHours:0.0} hr)" : string.Empty
                        );
            }
            if (State == ScrumStateObsolete.Approved)
            {
                return "Approved by Product Owner";
            }
            if (State == ScrumStateObsolete.Committed)
            {
                return "Committed by Team";
            }
            if (State == ScrumStateObsolete.Done)
            {
                return "✔️ Done";
            }
            if (State == ScrumStateObsolete.Closed)
            {
                return "✔️ Closed";
            }

            return null;
        }
    }

    public string? StatusCssClass =>
        State == ScrumStateObsolete.Done ? "status-done"
        : State == ScrumStateObsolete.InProgress ? "status-in-progress"
        : State == ScrumStateObsolete.ToDo ? "status-to-do"
        : State == ScrumStateObsolete.Closed ? "status-done"
        : null;
}