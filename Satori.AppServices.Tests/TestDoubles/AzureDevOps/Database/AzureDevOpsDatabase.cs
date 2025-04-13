using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps.Models;
using NotSupportedException = System.NotSupportedException;
using WorkItem = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.TestDoubles.AzureDevOps.Database;

/// <summary>
/// An in-memory database of objects to be used by a TestAzureDevOpsServer
/// </summary>
/// <remarks>
/// <para>
/// The public interface for this call has a readonly interface to the data.
/// For write access, cast this object as a <see cref="IAzureDevOpsDatabaseWriter"/>.
/// </para>
/// </remarks>
internal class AzureDevOpsDatabase : IAzureDevOpsDatabaseWriter
{
    #region Storage (the tables)

    private readonly List<PullRequest> _pullRequests = [];
    private readonly List<(int PullRequestId, int WorkItemId)> _pullRequestWorkItems = [];
    private readonly List<Team> _teams = [];
    private readonly List<WorkItem> _workItems = [];
    private readonly Dictionary<Team, Iteration> _iterations = [];
    private readonly Dictionary<int, int> _parentWorkItemId = [];

    #endregion Storage (the tables)

    #region Write Access

    void IAzureDevOpsDatabaseWriter.AddPullRequest(PullRequest pullRequest)
    {
        _pullRequests.Add(pullRequest);
    }

    void IAzureDevOpsDatabaseWriter.LinkWorkItem(PullRequest pullRequest, WorkItem workItem)
    {
        AddWorkItem(workItem);
        _pullRequestWorkItems.Add((pullRequest.PullRequestId, workItem.Id));
        AddWorkItemLink(workItem, pullRequest);
    }

    void IAzureDevOpsDatabaseWriter.AddTeam(Team team)
    {
        _teams.Add(team);
    }
    void IAzureDevOpsDatabaseWriter.LinkIteration(Team team, Iteration iteration)
    {
        _iterations[team] = iteration;
    }

    public void AddWorkItem(WorkItem workItem)
    {
        if (!_workItems.Contains(workItem))
        {
            _workItems.Add(workItem);
        }
    }

    private static void AddWorkItemLink(WorkItem workItem, PullRequest pullRequest)
    {
        var relation = new WorkItemRelation
        {
            Attributes = new Dictionary<string, object>
            {
                { "resourceCreatedDate", pullRequest.CreationDate.ToString("o") }, 
                { "name", "Pull Request" },
            },
            RelationType = "ArtifactLink",
            Url = $"vstfs:///Git/PullRequestId/{pullRequest.Repository.Project.Id}%2F{pullRequest.Repository.Id}%2F{pullRequest.PullRequestId}"
        };

        workItem.Relations.Add(relation);
    }

    public void AddWorkItemLink(WorkItem leftWorkItem, LinkType linkType, WorkItem rightWorkItem)
    {
        if (linkType == LinkType.IsParentOf)
        {
            _parentWorkItemId[rightWorkItem.Id] = leftWorkItem.Id;
            rightWorkItem.Fields.Parent = leftWorkItem.Id;
            rightWorkItem.Relations.Add(new WorkItemRelation
            {
                Attributes = new Dictionary<string, object>()
                {
                    { "isLocked", false },
                    { "name", "Parent" }
                },
                RelationType = "System.LinkTypes.Hierarchy-Reverse",
                Url = $"http://devops.test/Org/{leftWorkItem.Fields.ProjectName}/_apis/wit/workItems/{leftWorkItem.Id}"
            });
            leftWorkItem.Relations.Add(new WorkItemRelation
            {
                Attributes = new Dictionary<string, object>()
                {
                    { "isLocked", false },
                    { "name", "Child" }
                },
                RelationType = "System.LinkTypes.Hierarchy-Forward",
                Url = $"http://devops.test/Org/{rightWorkItem.Fields.ProjectName}/_apis/wit/workItems/{rightWorkItem.Id}"
            });
            return;
        }
        if (linkType == LinkType.IsChildOf)
        {
            _parentWorkItemId[leftWorkItem.Id] = rightWorkItem.Id;
            leftWorkItem.Fields.Parent = rightWorkItem.Id;
            leftWorkItem.Relations.Add(new WorkItemRelation
            {
                Attributes = new Dictionary<string, object>()
                {
                    { "isLocked", false },
                    { "name", "Parent" }
                },
                RelationType = "System.LinkTypes.Hierarchy-Reverse",
                Url = $"http://devops.test/Org/{rightWorkItem.Fields.ProjectName}/_apis/wit/workItems/{rightWorkItem.Id}"
            });
            rightWorkItem.Relations.Add(new WorkItemRelation
            {
                Attributes = new Dictionary<string, object>()
                {
                    { "isLocked", false },
                    { "name", "Child" }
                },
                RelationType = "System.LinkTypes.Hierarchy-Forward",
                Url = $"http://devops.test/Org/{leftWorkItem.Fields.ProjectName}/_apis/wit/workItems/{leftWorkItem.Id}"
            });
            return;
        }

        throw new NotSupportedException();
    }

    #endregion Write Access

    #region Read Access

    public PullRequest[] GetPullRequests() => [.. _pullRequests];

    public IEnumerable<int> GetWorkItemIdsForPullRequestId(int pullRequestId)
    {
        return _pullRequestWorkItems
            .Where(map => map.PullRequestId == pullRequestId)
            .Select(map => map.WorkItemId)
            .ToArray();
    }

    public IEnumerable<WorkItem> GetWorkItems() => _workItems;

    public IEnumerable<WorkItem> GetWorkItemsById(IEnumerable<int> workItemIds)
    {
        var foundWorkItems = _workItems
            .Where(wi => wi.Id.IsIn(workItemIds))
            .ToArray();

        var notFoundIds = workItemIds.Where(id => id.IsNotIn(foundWorkItems.Select(wi => wi.Id))).ToArray();
        if (notFoundIds.Any())
        {
            throw new InvalidOperationException($"TF401232: Work item {notFoundIds.First()} does not exist, or you do not have permissions to read it.");
        }

        return foundWorkItems;
    }

    public Team[] GetTeams() => [.. _teams];

    public Iteration? GetIterationForTeam(Team team)
    {
        _iterations.TryGetValue(team, out var iteration);
        return iteration;
    }

    public WorkItemLink[] GetWorkItemsForIteration(IterationId iteration)
    {
        var workItems = _workItems
            .Where(wi => wi.Fields.IterationPath == iteration.IterationPath && wi.Fields.ProjectName == iteration.ProjectName).ToArray();
        var parents = workItems
            .Select(t => _parentWorkItemId.TryGetValue(t.Id, out var parentId) ? _workItems.FirstOrDefault(wi => wi.Id == parentId) : null);

        return workItems.Union(parents)
            .Where(wi => wi != null).Select(wi => wi ?? throw new ArgumentNullException(nameof(wi)))
            .Select(wi => new WorkItemLink()
            {
                Source = GetParentSource(wi),
                Target = new WorkItemReference() { Id = wi.Id }
            })
            .ToArray();

        WorkItemReference? GetParentSource(WorkItem childWorkItem)
        {
            if (_parentWorkItemId.TryGetValue(childWorkItem.Id, out var parentId))
            {
                return new WorkItemReference() { Id = parentId };
            }

            return null;
        }

    }
}

#endregion Read Access