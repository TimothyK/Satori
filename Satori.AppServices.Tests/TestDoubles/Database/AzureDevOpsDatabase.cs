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
    private readonly List<(int PullRequestId, WorkItem WorkItem)> _pullRequestWorkItems = [];

    #endregion Storage (the tables)

    #region Write Access

    void IAzureDevOpsDatabaseWriter.AddPullRequest(PullRequest pullRequest)
    {
        _pullRequests.Add(pullRequest);
    }

    void IAzureDevOpsDatabaseWriter.LinkWorkItem(PullRequest pullRequest, WorkItem workItem)
    {
        _pullRequestWorkItems.Add((pullRequest.PullRequestId, workItem));
    }

    #endregion Write Access

    #region Read Access

    public PullRequest[] GetPullRequests() => [.. _pullRequests];

    public IEnumerable<int> GetWorkItemIdsForPullRequestId(int pullRequestId)
    {
        return _pullRequestWorkItems
            .Where(map => map.PullRequestId == pullRequestId)
            .Select(map => map.WorkItem.Id)
            .ToArray();
    }

    public IEnumerable<WorkItem> GetWorkItemsById(IEnumerable<int> workItemIds)
    {
        return _pullRequestWorkItems
            .Select(map => map.WorkItem)
            .Distinct()
            .Where(wi => wi.Id.IsIn(workItemIds))
            .ToArray();
    }
}

#endregion Read Access