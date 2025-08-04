using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.TimeServices;
using Shouldly;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class GetPullRequestsTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private readonly TestTimeServer _timeServer = new();

    public GetPullRequestsTests()
    {
        var azureDevOpsServer = new TestAzureDevOpsServer();
        _builder = azureDevOpsServer.CreateBuilder();

        var kimai = new TestKimaiServer();

        var services = new ServiceCollection();
        services.AddSingleton(azureDevOpsServer.AsInterface());
        services.AddSingleton(kimai.AsInterface());
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IAlertService>(new AlertService());
        services.AddSingleton<ITimeServer>(_timeServer);
        services.AddTransient<SprintBoardService>();

        _serviceProvider = services.BuildServiceProvider();
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
        var srv = _serviceProvider.GetRequiredService<SprintBoardService>();

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
    public async Task AbandonedDraftPullRequest_Ignored()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        pullRequest.Status = Status.Abandoned.ToApiValue();
        pullRequest.IsDraft = true;

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        workItems.Single().PullRequests.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task RestrictedPullRequest_Ignored()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        pullRequest.Title = "throw TF401180";

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