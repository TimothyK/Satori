using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Database;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;

internal class PullRequestBuilder
{
    private readonly IAzureDevOpsDatabaseWriter _database;

    public PullRequestBuilder(IAzureDevOpsDatabaseWriter database)
    {
        _database = database;
        PullRequest = BuildPullRequest();
        _database.AddPullRequest(PullRequest);
    }

    public PullRequest PullRequest { get; }

    private static PullRequest BuildPullRequest()
    {
        var pr = Builder.Builder<PullRequest>.New().Build(int.MaxValue);
        pr.Reviewers = [];
        pr.CreatedBy.ImageUrl = $"http://devops.test/Org/_api/_common/identityImage?id={pr.CreatedBy.Id}";
        pr.Url = $"http://devops.test/Org/{pr.Repository.Project.Name}/_apis/git/repositories/{pr.Repository.Name}/pullRequests/{pr.PullRequestId}";
        pr.Status = "active";
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
        _database.LinkWorkItem(PullRequest, workItem);
        return this;
    }

    private WorkItem BuildWorkItem()
    {
        var builder = new WorkItemBuilder(_database);
        return builder.WorkItem;
    }
}