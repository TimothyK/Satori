using Pscl.Linq;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles
{
    /// <summary>
    /// An in-memory database of objects to be returned by a TestAzureDevOpsServer
    /// </summary>
    internal class AzureDevOpsDatabase : IBuilderAccess
    {
        private readonly List<PullRequest> _pullRequests = [];
        private readonly List<(int PullRequestId, WorkItem WorkItem)> _pullRequestWorkItems = [];

        void IBuilderAccess.AddPullRequest(PullRequest pullRequest)
        {
            _pullRequests.Add(pullRequest);
        }

        void IBuilderAccess.LinkWorkItem(PullRequest pullRequest, WorkItem workItem)
        {
            _pullRequestWorkItems.Add((pullRequest.PullRequestId, workItem));
        }

        public PullRequest[] GetPullRequests() => _pullRequests.ToArray();

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
}
