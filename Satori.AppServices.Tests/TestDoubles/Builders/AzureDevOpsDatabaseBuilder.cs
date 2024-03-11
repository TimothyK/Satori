using Satori.AppServices.Tests.TestDoubles.Database;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.Builders;

/// <summary>
/// Provides methods to build objects that are returned from the <see cref="AzureDevOps.IAzureDevOpsServer"/>.
/// These objects are automatically added to the database provided in the constructor.
/// </summary>
/// <param name="database"></param>
internal class AzureDevOpsDatabaseBuilder(IAzureDevOpsDatabaseWriter database)
{
    public PullRequestBuilder BuildPullRequest()
    {
        return BuildPullRequest(out _);
    }
    public PullRequestBuilder BuildPullRequest(out PullRequest pullRequest)
    {
        var builder = new PullRequestBuilder(database);
        pullRequest = builder.PullRequest;
        return builder;
    }

    public TeamBuilder BuildTeam()
    {
        return BuildTeam(out _);
    }
    public TeamBuilder BuildTeam(out Team team)
    {
        var builder = new TeamBuilder(database);
        team = builder.Team;
        return builder;
    }

    public WorkItemBuilder BuildWorkItem() => BuildWorkItem(out _);
    public WorkItemBuilder BuildWorkItem(out WorkItem workItem)
    {
        var builder = new WorkItemBuilder(database);
        workItem = builder.WorkItem;
        return builder;
    }

}