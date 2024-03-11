using Pscl.Linq;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.Database;

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

    public IEnumerable<WorkItem> GetWorkItemsById(IEnumerable<int> workItemIds)
    {
        return _workItems
            .Where(wi => wi.Id.IsIn(workItemIds))
            .ToArray();
    }

    public Team[] GetTeams() => [.. _teams];

    public Iteration? GetIterationForTeam(Team team)
    {
        _iterations.TryGetValue(team, out var iteration);
        return iteration;
    }

    public WorkItemRelation[] GetWorkItemsForIteration(IterationId iteration)
    {
        return _workItems
            .Where(wi => wi.Fields.IterationPath == iteration.IterationPath && wi.Fields.ProjectName == iteration.ProjectName)
            .Select(wi => new WorkItemRelation() { Target = new WorkItemId() { Id = wi.Id } })
            .ToArray();
    }
}

#endregion Read Access