using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.PullRequests.ActionItems;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AppServices.ViewModels.WorkItems.ActionItems;
using Satori.AzureDevOps.Models;
using Satori.Kimai;
using System.Diagnostics.CodeAnalysis;
using Satori.Kimai.Utilities;
using Satori.Kimai.ViewModels;
using KimaiProject = Satori.Kimai.ViewModels.Project;
using KimaiActivity = Satori.Kimai.ViewModels.Activity;
using PullRequest = Satori.AppServices.ViewModels.PullRequests.PullRequest;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services.Converters;

public static class WorkItemExtensions
{
    public static void ResetCache()
    {
        Customers = null;
    }

    public static async Task InitializeCustomersForWorkItems(this IKimaiServer kimai)
    {
        if (Customers != null)
        {
            return;
        }

        Customers = await kimai.GetCustomersAsync();
    }

    private static Customer[]? Customers { get; set; }

    private static KimaiProject? FindKimaiProject(string? rawProjectCode)
    {
        if (string.IsNullOrWhiteSpace(rawProjectCode))
        {
            return null;
        }
        var projectCode = ProjectCodeParser.GetProjectCode(rawProjectCode);
        return Customers
            ?.SelectMany(customers => customers.Projects)
            .FirstOrDefault(project => project.ProjectCode.Equals(projectCode, StringComparison.CurrentCultureIgnoreCase));
    }

    private static KimaiActivity? FindKimaiActivity(KimaiProject? project, string? rawProjectCode)
    {
        if (project == null)
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(rawProjectCode))
        {
            return null;
        }

        var afterFirstPeriod = rawProjectCode.Contains('.')
            ? rawProjectCode[(rawProjectCode.IndexOf('.') + 1)..]
            : rawProjectCode;
        var activityCode = ProjectCodeParser.GetActivityCode(afterFirstPeriod);

        return project.Activities.FirstOrDefault(activity =>
            string.Equals(activity.ActivityCode, activityCode, StringComparison.CurrentCultureIgnoreCase));
    }

    public static WorkItem ToViewModel(this AzureDevOps.Models.WorkItem workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        if (Customers == null)
        {
            throw new InvalidOperationException($"Call {nameof(InitializeCustomersForWorkItems)} first");
        }

        try
        {
            return ToViewModelUnsafe(workItem);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Failed to build view model for work item {workItem.Id}.  {ex.Message}", ex);
        }
    }

    private static WorkItem ToViewModelUnsafe(this AzureDevOps.Models.WorkItem wi)
    {
        var id = wi.Id;
        var kimaiProject = FindKimaiProject(wi.Fields.ProjectCode);
        var kimaiActivity = FindKimaiActivity(kimaiProject, wi.Fields.ProjectCode);
        var workItem = new WorkItem()
        {
            Id = id,
            Rev = wi.Rev,
            Title = wi.Fields.Title,
            ProjectName = wi.Fields.ProjectName,
            AssignedTo = wi.Fields.AssignedTo,
            CreatedBy = wi.Fields.CreatedBy,
            CreatedDate = wi.Fields.SystemCreatedDate,
            AreaPath = wi.Fields.AreaPath,
            IterationPath = wi.Fields.IterationPath ?? string.Empty,
            AbsolutePriority = wi.Fields.BacklogPriority > 0.0 ? wi.Fields.BacklogPriority : double.MaxValue,
            Type = WorkItemType.FromApiValue(wi.Fields.WorkItemType),
            State = ScrumState.FromApiValue(wi.Fields.State),
            Triage = TriageState.FromApiValue(wi.Fields.Triage),
            TargetDate = wi.Fields.TargetDate,
            Blocked = wi.Fields.Blocked,
            Tags = ParseTags(wi.Fields.Tags),
            OriginalEstimate = wi.Fields.OriginalEstimate.HoursToTimeSpan(),
            CompletedWork = wi.Fields.CompletedWork.HoursToTimeSpan(),
            RemainingWork = wi.Fields.RemainingWork.HoursToTimeSpan(),
            KimaiProject = kimaiProject,
            KimaiActivity = kimaiActivity,
            Parent = CreateWorkItemPlaceholder(wi.Fields.Parent, UriParser.GetAzureDevOpsOrgUrl(wi.Url)),
            Url = UriParser.GetAzureDevOpsOrgUrl(wi.Url)
                .AppendPathSegment("_workItems/edit")
                .AppendPathSegment(id),
            ApiUrl = wi.Url,
            Children = GetRelatedWorkItemPlaceholders(wi.Relations, LinkType.IsChildOf),
            Predecessors = GetRelatedWorkItemPlaceholders(wi.Relations, LinkType.IsPredecessorOf),
            Successors = GetRelatedWorkItemPlaceholders(wi.Relations, LinkType.IsSuccessorOf),
            PullRequests = GetPullRequests(wi.Relations, UriParser.GetAzureDevOpsOrgUrl(wi.Url)),
        };

        workItem.ResetPeopleRelations();

        return workItem;
    }

    public static void ResetPeopleRelations(this IEnumerable<WorkItem> workItems)
    {
        var personPriority = new Dictionary<Person, int>();

        foreach (var workItem in workItems.OrderBy(wi => wi.AbsolutePriority))
        {
            workItem.ResetPeopleRelations();

            foreach (var assignment in workItem.ActionItems.SelectMany(actionItem => actionItem.On))
            {
                if (personPriority.TryGetValue(assignment.Person, out var priority))
                {
                    priority++;
                }
                else
                {
                    priority = 1;
                }
                personPriority[assignment.Person] = priority;
                assignment.Priority = priority;
            }
        }
    }

    public static void ResetPeopleRelations(this WorkItem workItem)
    {
        foreach (var child in workItem.Children)
        {
            child.ResetPeopleRelations();
        }

        workItem.WithPeople = workItem.AssignedTo.Yield()
            .Union(workItem.Children.SelectMany(task => task.WithPeople))
            .Union(workItem.PullRequests.Select(pr => pr.CreatedBy))
            .Union(workItem.PullRequests.SelectMany(pr => pr.Reviews.Select(review => review.Reviewer)))
            .Distinct()
            .ToList();

        ResetActionItems(workItem);
    }

    private static void ResetActionItems(WorkItem workItem)
    {
        var actionItems = workItem.Children.SelectMany(task => task.ActionItems).ToList();
        if (workItem.Type == WorkItemType.Task 
            && workItem.State < ScrumState.Done
            && (workItem.Predecessors.All(predecessor => ScrumState.Done <= predecessor.State) || workItem.State == ScrumState.InProgress))
        {
            actionItems.Add(new TaskActionItem(workItem));
        }

        var pullRequests = workItem.PullRequests
            .Where(pr => pr.Status != Status.Complete);
        foreach (var pr in pullRequests)
        {
            var prActionItems = new List<PullRequestActionItem>();

            if (pr.Status == Status.Draft)
            {
                prActionItems.Add(new PublishActionItem(pr));
            }
            else
            {
                var reviewActionItems = pr.Reviews
                    .Where(review => review is { Vote: ReviewVote.NoVote, HasDeclined: false })
                    .Select(review => new ReviewActionItem(pr, review.Reviewer));
                prActionItems.AddRange(reviewActionItems);
                if (pr.Reviews.Any(review => review.Vote <= ReviewVote.WaitingForAuthor))
                {
                    prActionItems.Add(new ReplyActionItem(pr));
                }
            }
            if (prActionItems.None())
            {
                actionItems.Add(new CompleteActionItem(pr));
            }

            actionItems.AddRange(prActionItems);
        }
        if (workItem.State < ScrumState.Done && actionItems.None() && workItem.Type != WorkItemType.Task)
        {
            actionItems.Add(new FinishActionItem(workItem));
        }
        workItem.ActionItems = actionItems;
    }

    private static List<PullRequest> GetPullRequests(List<WorkItemRelation> relations, Uri azureDevOpsOrgUrl)
    {
        var prRelations = relations.Where(r => r.RelationType == "ArtifactLink" && r.Attributes["name"].ToString() == "Pull Request");
        return prRelations.Select(relation => CreatePullRequestPlaceholder(relation, azureDevOpsOrgUrl)).ToList();

    }

    private static PullRequest CreatePullRequestPlaceholder(WorkItemRelation relation, Uri azureDevOpsOrgUrl)
    {
        var prParts = relation.Url.Split('/').Last().Split("%2F");
        var projectId = Guid.Parse(prParts[0]);
        var repoId = Guid.Parse(prParts[1]);
        var prId = int.Parse(prParts[2]);

        var url = azureDevOpsOrgUrl
            .AppendPathSegment(projectId)
            .AppendPathSegment("_git")
            .AppendPathSegment(repoId)
            .AppendPathSegment("PullRequest")
            .AppendPathSegment(prId);

        return new PullRequest
        {
            Project = projectId.ToString(),
            RepositoryName = repoId.ToString(),
            Id = prId,
            Title = $"PR#{prId}",
            Status = Status.Open,
            Url = url,
            CreatedBy = Person.Empty,
            Reviews = [],
            WorkItems = [],
            Labels = [],
        };
    }

    private static List<WorkItem> GetRelatedWorkItemPlaceholders(List<WorkItemRelation> relations, LinkType linkType)
    {
        return relations
            .Where(r => r.RelationType == linkType.ReverseLink.ToApiValue())
            .Select(r => CreateWorkItemPlaceholder(int.Parse(r.Url.Split('/').Last()), UriParser.GetAzureDevOpsOrgUrl(r.Url)))
            .ToList();
    }

    [return: NotNullIfNotNull(nameof(workItemId))]
    private static WorkItem? CreateWorkItemPlaceholder(int? workItemId, Uri azureDevOpsOrgUrl)
    {
        if (workItemId == null)
        {
            return null;
        }

        return new WorkItem
        {
            Id = workItemId.Value,
            Title = $"Work Item {workItemId}",
            ProjectName = "TeamProject",
            Url = azureDevOpsOrgUrl
                .AppendPathSegment("_workItems/edit")
                .AppendPathSegment(workItemId.Value),
            ApiUrl = azureDevOpsOrgUrl
                .AppendPathSegment("_apis/wit/workItems")
                .AppendPathSegment(workItemId),
            AssignedTo = Person.Empty,
            CreatedBy = Person.Empty,
            Type = WorkItemType.Unknown,
            State = ScrumState.Open,
            Tags = [],
        };
    }

    private static List<string> ParseTags(string? tagsString)
    {
        if (string.IsNullOrWhiteSpace(tagsString))
        {
            return [];
        }

        return tagsString.Split(";")
            .Select(tag => tag.Replace("_", " ").Trim())
            .ToList();
    }

    private static TimeSpan? HoursToTimeSpan(this double? value)
    {
        return value == null ? null : TimeSpan.FromHours(value.Value);
    }
}