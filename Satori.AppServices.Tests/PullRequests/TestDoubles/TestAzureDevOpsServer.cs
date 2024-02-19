using Moq;
using Pscl.Linq;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.PullRequests.TestDoubles;

internal interface IBuilderAccess
{
    void AddPullRequest(PullRequest pullRequest);
    void LinkWorkItem(PullRequest pullRequest, WorkItem workItem);
}

internal class TestAzureDevOpsServer : IBuilderAccess
{
    private const string AzureDevOpsRootUrl = "http://devops.test/Org";

    private readonly List<PullRequest> _pullRequests = [];
    private readonly List<(int PullRequestId, WorkItem WorkItem)> _pullRequestWorkItems = [];
    private readonly Mock<IAzureDevOpsServer> _mock;

    public TestAzureDevOpsServer()
    {
        _mock = new Mock<IAzureDevOpsServer>(MockBehavior.Strict);
        _mock.Setup(srv => srv.ConnectionSettings)
            .Returns(new ConnectionSettings { Url = new Uri(AzureDevOpsRootUrl), PersonalAccessToken = "token" });

        _mock.Setup(srv => srv.GetPullRequestsAsync())
            .ReturnsAsync(() => [.. _pullRequests]);

        _mock.Setup(srv => srv.GetPullRequestWorkItemIdsAsync(It.IsAny<PullRequest>()))
            .ReturnsAsync((PullRequest pr) => GetWorkItemMap(pr));

        _mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((IEnumerable<int> workItemIds) => GetWorkItems(workItemIds));
        
        return;
        IdMap[] GetWorkItemMap(PullRequest pullRequest)
        {
            return _pullRequestWorkItems
                .Where(map => map.PullRequestId == pullRequest.PullRequestId)
                .Select(map => Builder.Builder<IdMap>.New().Build(idMap => idMap.Id = map.WorkItem.Id))
                .ToArray();
        }

        WorkItem[] GetWorkItems(IEnumerable<int> workItemIds)
        {
            return _pullRequestWorkItems
                .Select(map => map.WorkItem)
                .Distinct()
                .Where(wi => wi.Id.IsIn(workItemIds))
                .ToArray();
        }
    }

    public IAzureDevOpsServer AsInterface() => _mock.Object;

    public PullRequestBuilder AddPullRequest()
    {
        return AddPullRequest(out _);
    }
    public PullRequestBuilder AddPullRequest(out PullRequest pullRequest)
    {
        var builder = new PullRequestBuilder(this);
        pullRequest = builder.PullRequest;
        return builder;
    }

    void IBuilderAccess.AddPullRequest(PullRequest pullRequest)
    {
        _pullRequests.Add(pullRequest);
    }

    void IBuilderAccess.LinkWorkItem(PullRequest pullRequest, WorkItem workItem)
    {
        _pullRequestWorkItems.Add((pullRequest.PullRequestId, workItem));
    }

}

internal class PullRequestBuilder
{
    private readonly IBuilderAccess _server;

    public PullRequestBuilder(IBuilderAccess server)
    {
        _server = server;
        PullRequest = BuildPullRequest();
        _server.AddPullRequest(PullRequest);
    }

    public PullRequest PullRequest { get; }

    private static PullRequest BuildPullRequest()
    {
        var pr = Builder.Builder<PullRequest>.New().Build(int.MaxValue);
        pr.Reviewers = [];
        return pr;
    }

    /// <summary>
    /// Builds a new WorkItem and associates it to the pull request
    /// </summary>
    /// <param name="workItem"></param>
    /// <returns></returns>
    public PullRequestBuilder WithWorkItem(out WorkItem workItem)
    {
        workItem = BuildWorkItem();
        return WithWorkItem(workItem);
    }

    /// <summary>
    /// Adds an existing WorkItem to the pull request
    /// </summary>
    /// <param name="workItem"></param>
    /// <returns></returns>
    public PullRequestBuilder WithWorkItem(WorkItem workItem)
    {
        _server.LinkWorkItem(PullRequest, workItem);
        return this;
    }

    private static WorkItem BuildWorkItem()
    {
        var expected = Builder.Builder<WorkItem>.New().Build(int.MaxValue);
        return expected;
    }
}