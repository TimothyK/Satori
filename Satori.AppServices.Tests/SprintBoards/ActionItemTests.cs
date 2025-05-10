using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels.Abstractions;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.PullRequests.ActionItems;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AppServices.ViewModels.WorkItems.ActionItems;
using Satori.AzureDevOps.Models;
using Shouldly;
using PullRequest = Satori.AzureDevOps.Models.PullRequest;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

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
        actionItems.ShouldBeOfType<FinishActionItem>()
            .ShouldBeOn(People.Alice)
            .ShouldBeFor(workItem)
            .ShouldHaveActionDescription($"Finish this {WorkItemType.FromApiValue(workItem.Fields.WorkItemType)}");
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
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(task)
            .ShouldHaveActionDescription("Start");
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
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(task)
            .ShouldHaveActionDescription("Resume");
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
        actionItems.ShouldBeOfType<FinishActionItem>()
            .ShouldBeOn(People.Alice)
            .ShouldBeFor(workItem);
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
        actionItems.ShouldBeOfType<PublishActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(pullRequest)
            .ShouldHaveActionDescription("Publish");
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
        actionItems.ShouldBeOfType<PublishActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(pullRequest);
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
        actionItems.ShouldBeOfType<CompleteActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(pullRequest)
            .ShouldHaveActionDescription("Complete");
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
        actionItems.ShouldBeOfType<ReviewActionItem>()
            .ShouldBeOn(People.Cathy)
            .ShouldBeFor(pullRequest)
            .ShouldHaveActionDescription("Review");
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
        actionItems.ShouldBeOfType<ReplyActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(pullRequest)
            .ShouldHaveActionDescription("Reply");
    }

    [TestMethod]
    public async Task PullRequest_MultipleReviewersWaiting_OneReplyActionItem()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.WaitingForAuthor;
        pullRequest.AddReviewer(People.Dave).Vote = (int)ReviewVote.WaitingForAuthor;

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<ReplyActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(pullRequest)
            .ShouldHaveActionDescription("Reply");
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
        actionItems.ShouldBeOfType<ReplyActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(pullRequest)
            .ShouldHaveActionDescription("Reply");
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
        actionItems.ShouldBeOfType<CompleteActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(pullRequest);
    }
    
    [TestMethod]
    public async Task PullRequest_Completed()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.Approved;
        pullRequest.Status = Status.Complete.ToApiValue();

        //Act
        var actionItems = await GetActionItems(sprint);

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<FinishActionItem>()
            .ShouldBeOn(People.Alice);
    }
}

internal static class ActionItemAssertionExtensions
{
    public static T[] ShouldBeOfType<T>(this ActionItem[] actionItems) where T : ActionItem
    {
        actionItems.ShouldNotBeEmpty();
        
        var actionItemsOfType = actionItems.OfType<T>().ToArray();
        actionItemsOfType.ShouldNotBeEmpty($"No action items of type {typeof(T).Name} were found.  They were {string.Join(", ", actionItems.Select(x => x.GetType().Name))}");

        return actionItemsOfType;
    }

    public static T[] ShouldBeOn<T>(this T[] actionItems, User user) where T : ActionItem
    {
        actionItems.ShouldNotBeEmpty();
        
        var matches = actionItems
            .Where(x => x.On.Select(person => person.AzureDevOpsId).Contains(user.Id))
            .ToArray();
        matches.ShouldNotBeEmpty($"No action items were found for {user.DisplayName}.  They were {string.Join(", ", actionItems.Select(x => x.On.SelectMany(person => person.DisplayName)))}");

        return matches;
    }
    
    public static T[] ShouldHaveActionDescription<T>(this T[] actionItems, string expected) where T : ActionItem
    {
        actionItems.ShouldNotBeEmpty();

        foreach (var actionItem in actionItems)
        {
            actionItem.ActionDescription.ShouldBe(expected);
        }

        return actionItems;
    }

    public static T[] ShouldBeFor<T>(this T[] actionItems, PullRequest pr) where T : PullRequestActionItem
    {
        actionItems.ShouldNotBeEmpty();
        
        var matches = actionItems
            .Where(x => x.PullRequest.Id == pr.PullRequestId)
            .ToArray();
        matches.ShouldNotBeEmpty($"No action items were found for pull request {pr.PullRequestId}.  They were for pull request(s) {string.Join(", ", actionItems.Select(x => x.PullRequest.Id))}");

        return matches;
    }
    
    public static TaskActionItem[] ShouldBeFor(this TaskActionItem[] actionItems, AzureDevOps.Models.WorkItem task)
    {
        actionItems.ShouldNotBeEmpty();
        
        var matches = actionItems
            .Where(x => x.Task.Id == task.Id)
            .ToArray();
        matches.ShouldNotBeEmpty($"No action items were found for task {task.Id}.  They were for task(s) {string.Join(", ", actionItems.Select(x => x.Task.Id))}");

        return matches;
    }
    
    public static FinishActionItem[] ShouldBeFor(this FinishActionItem[] actionItems, AzureDevOps.Models.WorkItem workItem)
    {
        actionItems.ShouldNotBeEmpty();
        
        var matches = actionItems
            .Where(x => x.WorkItem.Id == workItem.Id)
            .ToArray();
        matches.ShouldNotBeEmpty($"No action items were found for task {workItem.Id}.  They were for task(s) {string.Join(", ", actionItems.Select(x => x.WorkItem.Id))}");

        return matches;
    }
}