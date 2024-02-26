using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles;

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