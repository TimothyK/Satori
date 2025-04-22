using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class GetPullRequestsTests
{
    private readonly TestAzureDevOpsServer _azureDevOpsServer;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private readonly TestTimeServer _timeServer = new();

    public GetPullRequestsTests()
    {
        _azureDevOpsServer = new TestAzureDevOpsServer();
        _builder = _azureDevOpsServer.CreateBuilder();
    }

    #region Helpers

    #region Arrange

    private static Sprint BuildSprint()
    {
        return Builder.Builder<Sprint>.New().Build(int.MaxValue);
    }

    #endregion Arrange

    #region Act

    private async Task<WorkItem[]> GetWorkItemsAsync(params Sprint[] sprints)
    {
        var srv = new SprintBoardService(_azureDevOpsServer.AsInterface(), _timeServer, new AlertService(), new NullLoggerFactory());

        var workItems = (await srv.GetWorkItemsAsync(sprints)).ToArray();
        await srv.GetPullRequestsAsync(workItems);

        return workItems;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single().PullRequests.SingleOrDefault();
        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(pullRequest.PullRequestId);
        actual.Title.ShouldBe(pullRequest.Title);
    }
    
    [TestMethod]
    public async Task NoPullRequest_NotSet()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint);

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single().PullRequests.SingleOrDefault();
        actual.ShouldBeNull();
    }

    [TestMethod]
    public async Task TwoPullRequests()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem1).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest1).WithWorkItem(workItem1);

        _builder.BuildWorkItem(out var workItem2).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest2).WithWorkItem(workItem2);

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        workItems.Single(wi => wi.Id == workItem1.Id).PullRequests.Single().Title.ShouldBe(pullRequest1.Title);
        workItems.Single(wi => wi.Id == workItem2.Id).PullRequests.Single().Title.ShouldBe(pullRequest2.Title);
    }
    
    [TestMethod]
    public async Task TaskPullRequests()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(task);

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actualWorkItem = workItems.Single(wi => wi.Id == workItem.Id);
        actualWorkItem.PullRequests.ShouldBeEmpty();
        actualWorkItem.Children.Single().PullRequests.Single().Title.ShouldBe(pullRequest.Title);
    }

    [TestMethod]
    public async Task AbandonedPullRequest_Ignored()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        pullRequest.Status = Status.Abandoned.ToApiValue();

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        workItems.Single().PullRequests.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task CompletedPullRequest_Returned()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        pullRequest.Status = Status.Complete.ToApiValue();

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single().PullRequests.SingleOrDefault();
        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(pullRequest.PullRequestId);
        actual.Title.ShouldBe(pullRequest.Title);
    }
    
    [TestMethod]
    public async Task CompletedPullRequest_WithTag()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest()
            .WithWorkItem(workItem)
            .AddGitTag("v1.2.3");

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var pullRequest = workItems.Single().PullRequests.Single();
        pullRequest.VersionTags.Single().ShouldBe("v1.2.3");
    }
}