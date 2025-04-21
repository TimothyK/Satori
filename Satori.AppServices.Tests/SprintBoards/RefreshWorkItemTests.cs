using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class RefreshWorkItemTests
{
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private readonly TestTimeServer _timeServer = new();
    private readonly TestAlertService _alertService = new();
    private readonly SprintBoardService _sprintBoardService;

    public RefreshWorkItemTests()
    {
        var azureDevOpsServer = new TestAzureDevOpsServer();
        _builder = azureDevOpsServer.CreateBuilder();

        _sprintBoardService = new SprintBoardService(azureDevOpsServer.AsInterface(), _timeServer, _alertService, new NullLoggerFactory());
    }

    #region Helpers

    #region Arrange

    private readonly List<Sprint> _sprints = [];

    private Sprint BuildSprint()
    {
        var sprint = Builder.Builder<Sprint>.New().Build(int.MaxValue);
        _sprints.Add(sprint);
        return sprint;
    }
    
    #endregion Arrange

    #region Act

    /// <summary>
    /// Refreshes a work item.  Returns the work item before and after the refresh.
    /// </summary>
    /// <param name="workItem"></param>
    /// <param name="changeWorkItem"></param>
    /// <remarks>
    /// <para>
    /// The two methods <see cref="SprintBoardService.GetWorkItemsAsync"/> and <see cref="SprintBoardService.RefreshWorkItemAsync"/>
    /// should set all the properties of the Work Item the same.
    /// However, they do use different methods to load these work items.
    /// </para>
    /// </remarks>
    /// <returns></returns>
    private async Task<(WorkItem original, WorkItem? actual)> RefreshWorkItemAsync(AzureDevOps.Models.WorkItem workItem, Action? changeWorkItem = null)
    {
        //Arrange
        var workItems = (await _sprintBoardService.GetWorkItemsAsync(_sprints.ToArray())).ToList();
        var original = workItems.Single(wi => wi.Id == workItem.Id);
        if (changeWorkItem != null)
        {
            changeWorkItem.Invoke();
            workItem.Rev++;
        }

        //Act
        await _sprintBoardService.RefreshWorkItemAsync(workItems, original);

        //Assert
        var updatedWorkItem = workItems.SingleOrDefault(wi => wi.Id == workItem.Id);
        updatedWorkItem.ShouldNotBe(original);
        return (original, updatedWorkItem);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem);

        //Assert
        original.Id.ShouldBe(workItem.Id);
        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(workItem.Id);
    }
    


    #region Sprint

    [TestMethod]
    public async Task SprintUnchanged()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem);

        //Assert
        original.Sprint.ShouldBe(sprint);
        actual.ShouldNotBeNull();
        actual.Sprint.ShouldBe(sprint);
    }

    [TestMethod]
    public async Task ChangedSprint()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem,
            () => workItem.Fields.IterationPath += "Next");

        //Assert
        original.Id.ShouldBe(workItem.Id);
        actual.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task ChangedTeam()
    {
        //Arrange
        var sprint1 = BuildSprint();
        _builder.BuildWorkItem(out var workItem1).WithSprint(sprint1);
        var sprint2 = BuildSprint();
        _builder.BuildWorkItem(out var workItem2).WithSprint(sprint2);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem1,
            () =>
            {
                workItem1.Fields.ProjectName = sprint2.ProjectName;
                workItem1.Fields.AreaPath = workItem2.Fields.AreaPath;
                workItem1.Fields.IterationPath = sprint2.IterationPath;
            });

        //Assert
        original.IterationPath.ShouldBe(sprint1.IterationPath);
        actual.ShouldNotBeNull();
        actual.Sprint.ShouldBe(sprint2);
    }

    #endregion Sprint

    #region Sprint Priority

    [TestMethod]
    public async Task PriorityChange()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem1).WithSprint(sprint);
        _builder.BuildWorkItem(out var workItem2).WithSprint(sprint);
        workItem1.Fields.BacklogPriority = workItem2.Fields.BacklogPriority - 1.0;

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem1,
            () =>
            {
                workItem1.Fields.BacklogPriority = workItem2.Fields.BacklogPriority + 1.0;
            });

        //Assert
        original.SprintPriority.ShouldBe(1);
        actual.ShouldNotBeNull();
        actual.SprintPriority.ShouldBe(2);
    }

    #endregion Sprint Priority

    #region Children

    [TestMethod]
    public async Task HasChild()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem);

        //Assert
        original.Children.Single().Id.ShouldBe(task.Id);
        actual.ShouldNotBeNull();
        actual.Children.Single().Id.ShouldBe(task.Id);
    }
    
    [TestMethod]
    public async Task Child_Removed()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem,
            () => task.Fields.State = ScrumState.Removed.ToApiValue());

        //Assert
        original.Children.Single().Id.ShouldBe(task.Id);
        actual.ShouldNotBeNull();
        actual.Children.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task Child_MovedToDifferentSprint_Removed()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem,
            () => task.Fields.IterationPath += "Next");

        //Assert
        original.Children.Single().Id.ShouldBe(task.Id);
        actual.ShouldNotBeNull();
        actual.Children.ShouldBeEmpty();
    }

    #endregion Children

    #region PullRequests

    [TestMethod]
    public async Task HasPullRequest()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem);

        //Assert
        
        //This test runner does not load the PRs.
        //That is a separate call so tha the page view can show intermediate results while the PRs are still loading.
        original.PullRequests.ShouldNotBeEmpty();
        original.PullRequests.Single().Id.ShouldBe(pullRequest.PullRequestId);

        //The RefreshWorkItemAsync method does load the PRs, because it only takes a few milliseconds when invoked for a single work item.
        actual.ShouldNotBeNull();
        actual.PullRequests.ShouldNotBeEmpty();
        actual.PullRequests.Single().Id.ShouldBe(pullRequest.PullRequestId);
    }
    
    [TestMethod]
    public async Task HasPullRequest_LoadsTitle()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);

        //Act
        var (original, actual) = await RefreshWorkItemAsync(workItem);

        //Assert
        
        //This test runner does not load all the properties PRs on the original work item.
        //That is a separate call so tha the page view can show intermediate results while the PRs are still loading.
        original.PullRequests.ShouldNotBeEmpty();
        original.PullRequests.Single().Id.ShouldBe(pullRequest.PullRequestId);
        //original.PullRequests.Single().Title.ShouldNotBe(pullRequest.Title);

        //The RefreshWorkItemAsync method does load the PRs, because it only takes a few milliseconds when invoked for a single work item.
        actual.ShouldNotBeNull();
        actual.PullRequests.ShouldNotBeEmpty();
        actual.PullRequests.Single().Title.ShouldBe(pullRequest.Title);
    }

    [TestMethod]
    public async Task Child_HasPullRequest()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(task);

        //Act
        var (_, actual) = await RefreshWorkItemAsync(workItem);

        //Assert
        actual.ShouldNotBeNull();
        actual.Children.Single().PullRequests.ShouldNotBeEmpty();
        actual.Children.Single().PullRequests.Single().Id.ShouldBe(pullRequest.PullRequestId);
    }

    #endregion PullRequests

    #region People

    [TestMethod]
    public async Task Child_PeopleAreSet()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);

        //Act
        var (_, actual) = await RefreshWorkItemAsync(workItem);

        //Assert
        actual.ShouldNotBeNull();
        actual.WithPeople.ShouldNotBeEmpty();
        actual.WithPeople.Count.ShouldBe(3);
        actual.WithPeople.ShouldContain(p => p.AzureDevOpsId == workItem.Fields.AssignedTo.Id);
        actual.WithPeople.ShouldContain(p => p.AzureDevOpsId == task.Fields.AssignedTo.Id);
        actual.WithPeople.ShouldContain(p => p.AzureDevOpsId == pullRequest.CreatedBy.Id);
    }

    #endregion People

}