using CodeMonkeyProjectiles.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
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
using Satori.TimeServices;
using Shouldly;
using PullRequest = Satori.AzureDevOps.Models.PullRequest;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class ActionItemTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private readonly TestAlertService _alertService = new();
    private readonly TestTimeServer _timeServer = new();
    private readonly TestKimaiServer _kimai;

    public ActionItemTests()
    {
        var azureDevOpsServer = new TestAzureDevOpsServer();
        _builder = azureDevOpsServer.CreateBuilder();

        _kimai = new TestKimaiServer();

        var services = new ServiceCollection();
        services.AddSingleton(azureDevOpsServer.AsInterface());
        services.AddSingleton(_kimai.AsInterface());
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IAlertService>(_alertService);
        services.AddSingleton<ITimeServer>(_timeServer);
        services.AddTransient<SprintBoardService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    #region Helpers

    #region Arrange

    private static Sprint? DefaultSprint { get; set; }

    private static Sprint BuildSprint()
    {
        return Builder.Builder<Sprint>.New().Build(int.MaxValue);
    }

    private WorkItemBuilder BuildWorkItem(out AzureDevOps.Models.WorkItem workItem)
    {
        var sprint = DefaultSprint ??= BuildSprint();
        var workItemBuilder = _builder.BuildWorkItem(out workItem).WithSprint(sprint);

        var project = _kimai.AddProject();
        workItem.Fields.ProjectCode = project.ProjectCode;

        return workItemBuilder;
    }

    #endregion Arrange

    #region Act

    private async Task<ActionItem[]> GetActionItems()
    {
        if (DefaultSprint == null) throw new InvalidOperationException();

        return await GetActionItems(DefaultSprint);
    }

    private async Task<ActionItem[]> GetActionItems(params Sprint[] sprints)
    {
        var workItems = await GetWorkItemsAsync(sprints);

        return workItems.SelectMany(wi => wi.ActionItems)
            .Concat(workItems.SelectMany(wi => wi.PullRequests.SelectMany(pr => pr.ActionItems)))
            .ToArray();
    }

    private async Task<WorkItem[]> GetWorkItemsAsync(params Sprint[] sprints)
    {
        //Arrange
        WorkItemExtensions.ResetCache();

        //Act
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
        BuildWorkItem(out var workItem);
        workItem.Fields.AssignedTo = People.Alice;
        
        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        workItem.Fields.State = ScrumState.Done.ToApiValue();

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.ShouldBeEmpty();
    }

    #region Tasks

    [TestMethod]
    public async Task Task_ToDo()
    {
        //Arrange
        BuildWorkItem(out var workItem)
            .AddChild(out var task);
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = People.Bob;
        task.Fields.State = ScrumState.ToDo.ToApiValue();

        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem)
            .AddChild(out var task);
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = People.Bob;
        task.Fields.State = ScrumState.InProgress.ToApiValue();

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(task)
            .ShouldHaveActionDescription("Resume");
    }
    
    [TestMethod]
    public async Task Task_Unassigned()
    {
        //Arrange
        BuildWorkItem(out var workItem)
            .AddChild(out var task);
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = null;
        task.Fields.State = ScrumState.InProgress.ToApiValue();

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Alice)
            .ShouldBeFor(task)
            .ShouldHaveActionDescription("Assign");
    }

    [TestMethod]
    public async Task Task_Done()
    {
        //Arrange
        BuildWorkItem(out var workItem)
            .AddChild(out var task);
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = People.Bob;
        task.Fields.State = ScrumState.Done.ToApiValue();

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<FinishActionItem>()
            .ShouldBeOn(People.Alice)
            .ShouldBeFor(workItem);
    }

    #endregion Tasks

    #region Fund

    [TestMethod]
    public async Task Fund()
    {
        //Arrange
        BuildWorkItem(out var workItem);
        workItem.Fields.AssignedTo = People.Alice;
        workItem.Fields.ProjectCode = null;

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<FundActionItem>()
            .ShouldBeOn(People.Alice)
            .ShouldBeFor(workItem);
    }

    #endregion Fund

    #region Predecessor/Successor

    [TestMethod]
    public async Task Tasks_NoPredecessors_AllReadyToStartInParallel()
    {
        //Arrange
        var workItemBuilder = BuildWorkItem(out var workItem);
        workItemBuilder.AddChild(out var coding);
        workItemBuilder.AddChild(out var testing);
        workItem.Fields.AssignedTo = People.Alice;
        coding.Fields.AssignedTo = People.Bob;
        coding.Fields.State = ScrumState.ToDo.ToApiValue();
        testing.Fields.AssignedTo = People.Cathy;
        testing.Fields.State = ScrumState.ToDo.ToApiValue();

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(coding)
            .ShouldHaveActionDescription("Start");
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Cathy)
            .ShouldBeFor(testing)
            .ShouldHaveActionDescription("Start");
    }
    
    [TestMethod]
    public async Task Successor_PredecessorInProgress_NoActionItemForSuccessor()
    {
        //Arrange
        var workItemBuilder = BuildWorkItem(out var workItem);
        workItemBuilder.AddChild(out var coding);
        workItemBuilder.AddChild(out var testing);
        workItem.Fields.AssignedTo = People.Alice;
        coding.Fields.AssignedTo = People.Bob;
        coding.Fields.State = ScrumState.InProgress.ToApiValue();
        testing.Fields.AssignedTo = People.Cathy;
        testing.Fields.State = ScrumState.ToDo.ToApiValue();
        _builder.AddLink(coding, LinkType.IsPredecessorOf, testing);

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(coding)
            .ShouldHaveActionDescription("Resume");
        actionItems.Length.ShouldBe(1);
    }
    
    [TestMethod]
    public async Task Successor_PredecessorIsDone_SuccessorShouldStart()
    {
        //Arrange
        var workItemBuilder = BuildWorkItem(out var workItem);
        workItemBuilder.AddChild(out var coding);
        workItemBuilder.AddChild(out var testing);
        workItem.Fields.AssignedTo = People.Alice;
        coding.Fields.AssignedTo = People.Bob;
        coding.Fields.State = ScrumState.Done.ToApiValue();
        testing.Fields.AssignedTo = People.Cathy;
        testing.Fields.State = ScrumState.ToDo.ToApiValue();
        _builder.AddLink(coding, LinkType.IsPredecessorOf, testing);

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Cathy)
            .ShouldBeFor(testing)
            .ShouldHaveActionDescription("Start");
        actionItems.Length.ShouldBe(1);
    }
    
    [TestMethod]
    public async Task Successor_InProgress_ShouldResume()
    {
        //Arrange
        var workItemBuilder = BuildWorkItem(out var workItem);
        workItemBuilder.AddChild(out var coding);
        workItemBuilder.AddChild(out var testing);
        workItem.Fields.AssignedTo = People.Alice;
        coding.Fields.AssignedTo = People.Bob;
        coding.Fields.State = ScrumState.InProgress.ToApiValue();
        testing.Fields.AssignedTo = People.Cathy;
        testing.Fields.State = ScrumState.InProgress.ToApiValue();
        _builder.AddLink(coding, LinkType.IsPredecessorOf, testing);

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(coding)
            .ShouldHaveActionDescription("Resume");
        actionItems.ShouldBeOfType<TaskActionItem>()
            .ShouldBeOn(People.Cathy)
            .ShouldBeFor(testing)
            .ShouldHaveActionDescription("Resume");
    }

    #endregion Predecessor/Successor

    #region Pull Requests

    [TestMethod]
    public async Task PullRequest_Draft()
    {
        //Arrange
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.IsDraft = true;
        
        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.IsDraft = true;
        pullRequest.AddReviewer(People.Cathy);

        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;

        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy);

        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.WaitingForAuthor;

        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.WaitingForAuthor;
        pullRequest.AddReviewer(People.Dave).Vote = (int)ReviewVote.WaitingForAuthor;

        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.Rejected;

        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.Approved;

        //Act
        var actionItems = await GetActionItems();

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
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).Vote = (int)ReviewVote.Approved;
        pullRequest.Status = Status.Complete.ToApiValue();

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<FinishActionItem>()
            .ShouldBeOn(People.Alice);
    }
    
    /// <summary>
    /// When a task is assigned to the PR, it should have a single action item.
    /// </summary>
    /// <remarks><para>https://github.com/TimothyK/Satori/issues/96</para></remarks>
    /// <returns></returns>
    [TestMethod]
    public async Task PullRequest_TaskAssigned_SingleActionItem()
    {
        //Arrange
        BuildWorkItem(out var workItem)
            .AddChild(out var task);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(task);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy);

        //Act
        var actionItems = (await GetActionItems())
            .OfType<PullRequestActionItem>().Cast<ActionItem>()
            .ToArray();

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<ReviewActionItem>()
            .ShouldBeOn(People.Cathy)
            .ShouldBeFor(pullRequest)
            .ShouldHaveActionDescription("Review");
    }

    [TestMethod]
    public async Task PullRequest_DeclinedReviewer()
    {
        //Arrange
        BuildWorkItem(out var workItem);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy).With(reviewer => reviewer.HasDeclined = true);

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.Length.ShouldBe(1);
        actionItems.ShouldBeOfType<CompleteActionItem>()
            .ShouldBeOn(People.Bob)
            .ShouldBeFor(pullRequest)
            .ShouldHaveActionDescription("Complete");
    }

    #endregion Pull Requests

    #region Priority

    [TestMethod]
    public async Task Priority()
    {
        //Arrange
        BuildWorkItem(out var workItem1);
        workItem1.Fields.AssignedTo = People.Alice;

        BuildWorkItem(out var workItem2);
        workItem2.Fields.AssignedTo = People.Bob;
        workItem2.Fields.BacklogPriority = workItem1.Fields.BacklogPriority + 1;

        BuildWorkItem(out var workItem3);
        workItem3.Fields.AssignedTo = People.Alice;
        workItem3.Fields.BacklogPriority = workItem2.Fields.BacklogPriority + 1;

        //Act
        var actionItems = await GetActionItems();

        //Assert
        actionItems.Length.ShouldBe(3);

        actionItems.ShouldBeOfType<FinishActionItem>()
            .Where(item => item.WorkItem.Id == workItem1.Id).ToArray()
            .ShouldBeOn(People.Alice, 1);
        actionItems.ShouldBeOfType<FinishActionItem>()
            .Where(item => item.WorkItem.Id == workItem2.Id).ToArray()
            .ShouldBeOn(People.Bob, 1);
        actionItems.ShouldBeOfType<FinishActionItem>()
            .Where(item => item.WorkItem.Id == workItem3.Id).ToArray()
            .ShouldBeOn(People.Alice, 2);
    }

    #endregion Priority
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
            .Where(actionItem => actionItem.On.Select(x => x.Person.AzureDevOpsId).Contains(user.Id))
            .ToArray();
        matches.ShouldNotBeEmpty($"No action items were found for {user.DisplayName}.  They were {string.Join(", ", actionItems.SelectMany(actionItem => actionItem.On.Select(x => x.Person.DisplayName)))}");

        return matches;
    }
    
    public static T[] ShouldBeOn<T>(this T[] actionItems, User user, int priority) where T : ActionItem
    {
        actionItems.ShouldNotBeEmpty();
        
        var matches = actionItems
            .Where(actionItem => actionItem.On.Select(x => x.Person.AzureDevOpsId).Contains(user.Id))
            .Where(actionItem => actionItem.On.Select(x => x.Priority).Contains(priority))
            .ToArray();
        matches.ShouldNotBeEmpty($"No action items were found for {user.DisplayName} with priority {priority}.  They were {string.Join(", ", actionItems.SelectMany(actionItem => actionItem.On.Select(x => $"{x.Person.DisplayName}-{x.Priority}")))}");

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
    
    public static T[] ShouldBeFor<T>(this T[] actionItems, AzureDevOps.Models.WorkItem workItem) where T : WorkItemActionItem
    {
        actionItems.ShouldNotBeEmpty();
        
        var matches = actionItems
            .Where(x => x.WorkItem.Id == workItem.Id)
            .ToArray();
        matches.ShouldNotBeEmpty($"No action items were found for work item {workItem.Id}.  They were for work item(s) {string.Join(", ", actionItems.Select(x => x.WorkItem.Id))}");

        return matches;
    }
}