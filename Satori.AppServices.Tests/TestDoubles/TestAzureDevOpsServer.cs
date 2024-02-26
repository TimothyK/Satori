using Moq;
using Pscl.Linq;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles;

internal class TestAzureDevOpsServer
{
    private const string AzureDevOpsRootUrl = "http://devops.test/Org";

    private readonly AzureDevOpsDatabase _database = new();

    private readonly Mock<IAzureDevOpsServer> _mock;

    public TestAzureDevOpsServer()
    {
        _mock = new Mock<IAzureDevOpsServer>(MockBehavior.Strict);
        _mock.Setup(srv => srv.ConnectionSettings)
            .Returns(new ConnectionSettings { Url = new Uri(AzureDevOpsRootUrl), PersonalAccessToken = "token" });

        _mock.Setup(srv => srv.GetPullRequestsAsync())
            .ReturnsAsync(() => _database.GetPullRequests());

        _mock.Setup(srv => srv.GetPullRequestWorkItemIdsAsync(It.IsAny<PullRequest>()))
            .ReturnsAsync((PullRequest pr) => GetWorkItemMap(pr));

        _mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((IEnumerable<int> workItemIds) => GetWorkItems(workItemIds));

        return;
        IdMap[] GetWorkItemMap(PullRequest pullRequest)
        {
            return _database.GetWorkItemIdsForPullRequestId(pullRequest.PullRequestId)
                .Select(workItemId => Builder.Builder<IdMap>.New().Build(idMap => idMap.Id = workItemId))
                .ToArray();
        }

        WorkItem[] GetWorkItems(IEnumerable<int> workItemIds)
        {
            return _database.GetWorkItemsById(workItemIds).ToArray();
        }
    }

    public IAzureDevOpsServer AsInterface() => _mock.Object;

    public PullRequestBuilder BuildPullRequest()
    {
        return BuildPullRequest(out _);
    }
    public PullRequestBuilder BuildPullRequest(out PullRequest pullRequest)
    {
        var builder = new PullRequestBuilder(_database);
        pullRequest = builder.PullRequest;
        return builder;
    }
}