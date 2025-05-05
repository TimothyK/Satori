using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.ViewModels.Sprints;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels.Abstractions;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.PullRequests.ActionItems;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AppServices.ViewModels.WorkItems.ActionItems;
using Shouldly;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class ActionItemTests
{
    private readonly TestAzureDevOpsServer _azureDevOpsServer;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private readonly TestTimeServer _timeServer = new();

    public ActionItemTests()
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

    private async Task<ActionItem[]> GetActionItems(params Sprint[] sprints)
    {
        var workItems = await GetWorkItemsAsync(sprints);

        return workItems.SelectMany(wi => wi.ActionItems)
            .Concat(workItems.SelectMany(wi => wi.PullRequests.SelectMany(pr => pr.ActionItems)))
            .ToArray();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.AssignedTo = People.Alice;

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        var actual = actionItems.Single() as FinishActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Alice.Id);
        actual.Message.ShouldBe($"This {WorkItemType.FromApiValue(workItem.Fields.WorkItemType)} can be marked as Done or have more tasks added");
        actual.WorkItem.Id.ShouldBe(workItem.Id);
    }
    
    [TestMethod]
    public async Task Done()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.State = ScrumState.Done.ToApiValue();

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task Task_ToDo()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = People.Bob;
        task.Fields.State = ScrumState.ToDo.ToApiValue();

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        var actual = actionItems.Single() as TaskActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Bob.Id);
        actual.Message.ShouldBe("This task can be started");
        actual.Task.Id.ShouldBe(task.Id);
    }
    
    [TestMethod]
    public async Task Task_InProgress()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = People.Bob;
        task.Fields.State = ScrumState.InProgress.ToApiValue();

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        var actual = actionItems.Single() as TaskActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Bob.Id);
        actual.Message.ShouldBe("This task can be resumed");
        actual.Task.Id.ShouldBe(task.Id);
    }
    
    [TestMethod]
    public async Task Task_Done()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = People.Bob;
        task.Fields.State = ScrumState.Done.ToApiValue();

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        var actual = actionItems.Single() as FinishActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Alice.Id);
        actual.Message.ShouldBe($"This {WorkItemType.FromApiValue(workItem.Fields.WorkItemType)} can be marked as Done or have more tasks added");
        actual.WorkItem.Id.ShouldBe(workItem.Id);
    }
    
    [TestMethod]
    public async Task PullRequest_Draft()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.IsDraft = true;
        
        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        var actual = actionItems.Single() as PublishActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Bob.Id);
        actual.Message.ShouldBe("The draft PR needs published");
        actual.PullRequest.Id.ShouldBe(pullRequest.PullRequestId);
    }
    
    [TestMethod]
    public async Task PullRequest_DraftWithReviewer()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.IsDraft = true;
        pullRequest.AddReviewer(People.Cathy);

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        var actual = actionItems.Single() as PublishActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Bob.Id);
        actual.Message.ShouldBe("The draft PR needs published");
        actual.PullRequest.Id.ShouldBe(pullRequest.PullRequestId);
    }

    [TestMethod]
    public async Task PullRequest_NeedsReviewer()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.Single().ShouldBeOfType<CompleteActionItem>();
        var actual = actionItems.Single() as CompleteActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Bob.Id);
        actual.Message.ShouldBe("Complete the PR or add a reviewer");
        actual.PullRequest.Id.ShouldBe(pullRequest.PullRequestId);
    }

    [TestMethod]
    public async Task PullRequest_NeedsReview()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy);

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.Single().ShouldBeOfType<ReviewActionItem>();
        var actual = actionItems.Single() as ReviewActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Cathy.Id);
        actual.Message.ShouldBe("The PR is ready for review");
        actual.PullRequest.Id.ShouldBe(pullRequest.PullRequestId);
    }
    
    [TestMethod]
    public async Task PullRequest_WaitsForAuthor()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.WaitingForAuthor;

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.Single().ShouldBeOfType<ReplyActionItem>();
        var actual = actionItems.Single() as ReplyActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Bob.Id);
        actual.Message.ShouldBe("A reply is needed for the reviewer's comment(s)");
        actual.PullRequest.Id.ShouldBe(pullRequest.PullRequestId);
    }
    
    [TestMethod]
    public async Task PullRequest_ReviewerRejects()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.Rejected;

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.Single().ShouldBeOfType<ReplyActionItem>();
        var actual = actionItems.Single() as ReplyActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Bob.Id);
        actual.Message.ShouldBe("A reply is needed for the reviewer's comment(s)");
        actual.PullRequest.Id.ShouldBe(pullRequest.PullRequestId);
    }
    
    [TestMethod]
    public async Task PullRequest_Approved()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.Approved;

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.Single().ShouldBeOfType<CompleteActionItem>();
        var actual = actionItems.Single() as CompleteActionItem;
        actual.ShouldNotBeNull();
        actual.On.Count.ShouldBe(1);
        actual.On.Single().AzureDevOpsId.ShouldBe(People.Bob.Id);
        actual.PullRequest.Id.ShouldBe(pullRequest.PullRequestId);
    }
}
