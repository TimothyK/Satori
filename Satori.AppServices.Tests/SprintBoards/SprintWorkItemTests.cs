using CodeMonkeyProjectiles.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps.Models;
using Shouldly;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class SprintWorkItemTests
{
    private readonly TestAzureDevOpsServer _azureDevOpsServer;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private readonly TestTimeServer _timeServer = new();

    public SprintWorkItemTests()
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

    private WorkItem[] GetWorkItems(params Sprint[] sprints)
    {
        var srv = new SprintBoardService(_azureDevOpsServer.AsInterface(), _timeServer, new AlertService(), new NullLoggerFactory());

        return srv.GetWorkItemsAsync(sprints).Result.ToArray();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Length.ShouldBe(1);
        workItems.Single().Id.ShouldBe(workItem.Id);
    }

    #region Sprint

    [TestMethod]
    public void NoSprint()
    {
        //Act
        var workItems = GetWorkItems();

        //Assert
        workItems.ShouldBeEmpty();
    }

    [TestMethod]
    public void SprintWithNoWorkItems()
    {
        //Arrange
        var sprint = BuildSprint();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.ShouldBeEmpty();
    }

    [TestMethod]
    public void WorkItemFromDifferentIteration_NotReported()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(BuildSprint());

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.ShouldBeEmpty();
    }

    [TestMethod]
    public void WorkItemHasSprint()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Length.ShouldBe(1);
        workItems.Single().Sprint.ShouldBe(sprint);
    }

    #endregion

    #region Child Tasks

    [TestMethod]
    public void ParentWithTask()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var parentWorkItem).WithSprint(sprint)
            .AddChild(out var task);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Length.ShouldBe(1);
        var workItem = workItems.Single();
        workItem.Id.ShouldBe(parentWorkItem.Id);
        workItem.Children.Count.ShouldBe(1);
        workItem.Children.Single().Id.ShouldBe(task.Id);
    }

    [TestMethod]
    public void ParentFromDifferentIteration_ReportedOnCurrentSprint()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var parentWorkItem).WithSprint(BuildSprint())
            .AddChild(out var task);
        task.Fields.ProjectName = sprint.ProjectName;
        task.Fields.IterationPath = sprint.IterationPath;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Length.ShouldBe(1);
        var workItem = workItems.Single();
        workItem.Id.ShouldBe(parentWorkItem.Id);
        workItem.Children.Count.ShouldBe(1);
        workItem.Children.Single().Id.ShouldBe(task.Id);
    }

    /// <summary>
    /// Support where a Bug has a parent of Product Backlog Item, not a Feature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is possible to set the parent of a Bug to be a PBI in AzDO.
    /// AzDO should prevent this.  It even has problems with this.
    /// When work items are linked like this, you are no longer allowed to reorder priority on the sprint.
    /// https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/resolve-backlog-reorder-issues?view=azure-devops
    /// </para>
    /// </remarks>
    [TestMethod]
    public void ParentBoardItemIsBoardItem_AzureDevOpsMockDatabaseReportsBadLink()
    {
        //Arrange
        var sprint = BuildSprint();
        var bug = _builder.BuildWorkItem().WithSprint(sprint).WorkItem
            .With(wi => wi.Fields.WorkItemType = WorkItemType.Bug.ToApiValue());
        _builder.BuildWorkItem(out var pbi).WithSprint(sprint)
            .With(builder => builder.WorkItem.Fields.WorkItemType = WorkItemType.ProductBacklogItem.ToApiValue())
            .AddChild(bug);

        //Act
        var srv = _azureDevOpsServer.AsInterface();
        var iterationId = (IterationId)sprint;
        var relations = srv.GetIterationWorkItemsAsync(iterationId).Result;

        //Assert
        relations.Any(r => r.Source?.Id == pbi.Id && r.Target.Id == bug.Id).ShouldBeTrue();
    }

    /// <summary>
    /// Support where a Bug has a parent of Product Backlog Item, not a Feature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is possible to set the parent of a Bug to be a PBI in AzDO.
    /// AzDO should prevent this.  It even has problems with this.
    /// When work items are linked like this, you are no longer allowed to reorder priority on the sprint.
    /// https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/resolve-backlog-reorder-issues?view=azure-devops
    /// </para>
    /// </remarks>
    [TestMethod]
    public void ParentBoardItemIsBoardItem_ReportedOnCurrentSprint()
    {
        //Arrange
        var sprint = BuildSprint();
        var bug = _builder.BuildWorkItem().WithSprint(sprint).WorkItem
            .With(wi => wi.Fields.WorkItemType = WorkItemType.Bug.ToApiValue());
        _builder.BuildWorkItem(out var pbi).WithSprint(sprint)
            .With(builder => builder.WorkItem.Fields.WorkItemType = WorkItemType.ProductBacklogItem.ToApiValue())
            .AddChild(bug);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Length.ShouldBe(2);
        workItems.ShouldContain(wi => wi.Id == bug.Id);
        workItems.ShouldContain(wi => wi.Id == pbi.Id);
    }

    #endregion Child Tasks

    #region Predecessor/Successor

    [TestMethod]
    public void PredecessorSuccessorLink()
    {
        //Arrange
        var sprint = BuildSprint();
        var parentBuilder = _builder.BuildWorkItem().WithSprint(sprint);
        parentBuilder.AddChild(out var codingChild);
        parentBuilder.AddChild(out var testingChild);
        codingChild.Fields.Title = "Coding";
        testingChild.Fields.Title = "Testing";
        _builder.AddLink(codingChild, LinkType.IsPredecessorOf, testingChild);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        var workItem = workItems.Single();
        var codingTask = workItem.Children.Single(wi => wi.Id == codingChild.Id);
        var testingTask = workItem.Children.Single(wi => wi.Id == testingChild.Id);
        codingTask.Predecessors.ShouldBeEmpty();
        codingTask.Successors.Count.ShouldBe(1);
        codingTask.Successors.ShouldContain(testingTask);
        testingTask.Predecessors.Count.ShouldBe(1);
        testingTask.Predecessors.ShouldContain(codingTask);
        testingTask.Successors.ShouldBeEmpty();
    }

    #endregion Predecessor/Successor

    #region Pull Requests

    [TestMethod]
    public void PullRequests_None()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Length.ShouldBe(1);
        var actual = workItems.Single();
        actual.Id.ShouldBe(workItem.Id);
        actual.PullRequests.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void PullRequests()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pr).WithWorkItem(workItem);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        var actual = workItems.Single().PullRequests.SingleOrDefault();
        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(pr.PullRequestId);
        actual.Project.ShouldBe(pr.Repository.Project.Id.ToString());
        actual.RepositoryName.ShouldBe(pr.Repository.Id.ToString());
    }

    #endregion Pull Requests

    #region Properties

    [TestMethod]
    public void Title()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Length.ShouldBe(1);
        workItems.Single().Title.ShouldBe(workItem.Fields.Title);
    }

    [TestMethod]
    public void Rev()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        var revision = RandomGenerator.Integer(100);
        workItem.Rev = revision;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Rev.ShouldBe(revision);
    }

    [TestMethod]
    public void AssignedTo()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.AssignedTo.ShouldNotBeNull();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        var vm = workItems.Single();
        vm.AssignedTo.ShouldNotBeNull();
        vm.AssignedTo.AzureDevOpsId.ShouldBe(workItem.Fields.AssignedTo.Id);
        vm.AssignedTo.DisplayName.ShouldBe(workItem.Fields.AssignedTo.DisplayName);
        vm.AssignedTo.AvatarUrl.ToString().ShouldBe(workItem.Fields.AssignedTo.ImageUrl);
    }

    [TestMethod]
    public void AssignedTo_Unassigned()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.AssignedTo = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        var vm = workItems.Single();
        vm.AssignedTo.ShouldBe(Person.Empty);
    }

    [TestMethod]
    public void NullPerson()
    {
        Person.Empty.AzureDevOpsId.ShouldBe(Guid.Empty);
        Person.Empty.DisplayName.ShouldBe("Unknown/Unassigned");
        Person.Empty.AvatarUrl.ToString().ShouldBe("/images/NullAvatar.png");
    }

    [TestMethod]
    public void CreatedBy()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        var vm = workItems.Single();
        vm.AssignedTo.ShouldNotBeNull();
        vm.CreatedBy.AzureDevOpsId.ShouldBe(workItem.Fields.CreatedBy.Id);
        vm.CreatedBy.DisplayName.ShouldBe(workItem.Fields.CreatedBy.DisplayName);
        vm.CreatedBy.AvatarUrl.ToString().ShouldBe(workItem.Fields.CreatedBy.ImageUrl);
    }

    [TestMethod]
    public void CreatedDate()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().CreatedDate.ShouldBe(workItem.Fields.SystemCreatedDate);
    }

    [TestMethod]
    public void AreaPath()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.AreaPath.ShouldNotBeNull();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().AreaPath.ShouldBe(workItem.Fields.AreaPath);
    }

    [TestMethod]
    public void IterationPath()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().IterationPath.ShouldBe(sprint.IterationPath);
    }

    [TestMethod]
    public void AbsolutePriority()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().AbsolutePriority.ShouldBe(workItem.Fields.BacklogPriority);
    }

    /// <summary>
    /// Zero priority in Azure DevOps should bottom priority, not top.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sometimes when a work item is added to the bottom of the board it gets priority zero.
    /// That's inconsistent with other priorities where normally, the lower priority number the higher it is on the board.
    /// These zero priorities are at the bottom of the board.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AbsolutePriority_Zero()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.BacklogPriority = 0;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().AbsolutePriority.ShouldBe(double.MaxValue);
    }

    [TestMethod]
    [DataRow("Product Backlog Item")]
    [DataRow("Bug")]
    [DataRow("Impediment")]
    public void Type(string type)
    {
        //Arrange
        var expected = WorkItemType.FromApiValue(type);
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.WorkItemType = type;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Type.ShouldBe(expected);
    }

    [TestMethod]
    public void ProjectName()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.ProjectName.ShouldNotBeNull();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().ProjectName.ShouldBe(workItem.Fields.ProjectName);
    }

    [TestMethod]
    public void ProjectCode()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.ProjectCode.ShouldNotBeNull();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().ProjectCode.ShouldBe(workItem.Fields.ProjectCode);
    }

    [TestMethod]
    public void ProjectCode_Missing()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.ProjectCode = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().ProjectCode.ShouldBeNullOrEmpty();
    }

    [TestMethod]
    public void Url()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Url.ShouldBe($"http://devops.test/Org/_workItems/edit/{workItem.Id}");
    }

    [TestMethod]
    public void ApiUrl()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Url.ShouldNotBeNull();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().ApiUrl.ShouldBe(workItem.Url);
    }

    #region Estimates

    [TestMethod]
    public void OriginalEstimate()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.OriginalEstimate.ShouldNotBeNull();
        var expected = TimeSpan.FromHours(task.Fields.OriginalEstimate.Value);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().OriginalEstimate.ShouldBe(expected);
    }

    [TestMethod]
    public void OriginalEstimate_Missing()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.OriginalEstimate = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().OriginalEstimate.ShouldBeNull();
    }

    [TestMethod]
    public void CompletedWork()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.CompletedWork.ShouldNotBeNull();
        var expected = TimeSpan.FromHours(task.Fields.CompletedWork.Value);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().CompletedWork.ShouldBe(expected);
    }

    [TestMethod]
    public void CompletedWork_Missing()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.CompletedWork = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().CompletedWork.ShouldBeNull();
    }

    [TestMethod]
    public void RemainingWork()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.RemainingWork.ShouldNotBeNull();
        var expected = TimeSpan.FromHours(task.Fields.RemainingWork.Value);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().RemainingWork.ShouldBe(expected);
    }

    [TestMethod]
    public void RemainingWork_Missing()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.RemainingWork = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().RemainingWork.ShouldBeNull();
    }

    #endregion

    #region Status

    [TestMethod]
    [DataRow("New")]
    [DataRow("Approved")]
    [DataRow("Committed")]
    [DataRow("Done")]
    [DataRow("Removed")]
    public void State(string state)
    {
        //Arrange
        var expected = ScrumState.FromApiValue(state);
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.State = state;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().State.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("To Do")]
    [DataRow("In Progress")]
    [DataRow("Done")]
    [DataRow("Removed")]
    public void TaskStatus(string state)
    {
        //Arrange
        var expected = ScrumState.FromApiValue(state);
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint)
            .AddChild(out var task);
        task.Fields.State = state;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().State.ShouldBe(expected);
    }

    [TestMethod]
    public void ToDo_NoEstimate()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.State = ScrumState.ToDo.ToApiValue();
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().StatusLabel.ShouldBe("⏳ To Do");
        workItems.Single().Children.Single().StatusCssClass.ShouldBe("status-to-do");
    }

    [TestMethod]
    public void ToDo_Estimate_NoRemaining()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.State = ScrumState.ToDo.ToApiValue();
        task.Fields.OriginalEstimate = 5.0;
        task.Fields.RemainingWork = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().StatusLabel.ShouldBe("⏳ To Do (~5.0 hr)");
        workItems.Single().Children.Single().StatusCssClass.ShouldBe("status-to-do");
    }

    [TestMethod]
    public void ToDo_Estimate_Remaining()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.State = ScrumState.ToDo.ToApiValue();
        task.Fields.OriginalEstimate = 10.0;
        task.Fields.RemainingWork = 9.9;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().StatusLabel.ShouldBe("⏳ To Do (~9.9 hr)");
        workItems.Single().Children.Single().StatusCssClass.ShouldBe("status-to-do");
    }

    [TestMethod]
    public void InProgress_NoEstimate_NoRemaining()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().StatusLabel.ShouldBe("⌛ In Progress");
        workItems.Single().Children.Single().StatusCssClass.ShouldBe("status-in-progress");
    }

    [TestMethod]
    public void InProgress_Estimate_NoRemaining()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        task.Fields.OriginalEstimate = 10.0;
        task.Fields.RemainingWork = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().StatusLabel.ShouldBe("⌛ In Progress (~10.0 hr)");
        workItems.Single().Children.Single().StatusCssClass.ShouldBe("status-in-progress");
    }

    [TestMethod]
    public void InProgress_Remaining()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        task.Fields.RemainingWork = 9.9;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().StatusLabel.ShouldBe("⌛ In Progress (9.9 hr)");
        workItems.Single().Children.Single().StatusCssClass.ShouldBe("status-in-progress");
    }

    [TestMethod]
    public void Done()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint).AddChild(out var task);
        task.Fields.State = ScrumState.Done.ToApiValue();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Children.Single().StatusLabel.ShouldBe("✔️ Done");
        workItems.Single().Children.Single().StatusCssClass.ShouldBe("status-done");
    }

    [TestMethod]
    public void StatusLabel_Approved()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.State = ScrumState.Approved.ToApiValue();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().StatusLabel.ShouldBe("Approved by Product Owner");
    }

    [TestMethod]
    public void StatusLabel_Committed()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.State = ScrumState.Committed.ToApiValue();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().StatusLabel.ShouldBe("Committed by Team");
    }

    [TestMethod]
    [DataRow("Open", "Open", null)]
    [DataRow("Closed", "✔️ Closed", "status-done")]
    public void Status_Impediment(string apiValue, string expectedStatusLabel, string? expectedStatusCssClass)
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.WorkItemType = WorkItemType.Impediment.ToApiValue();
        workItem.Fields.State = apiValue;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().StatusLabel.ShouldBe(expectedStatusLabel);
        workItems.Single().StatusCssClass.ShouldBe(expectedStatusCssClass);
    }

    [TestMethod]
    [DataRow("Pending")]
    [DataRow("More Info")]
    [DataRow("Info Received")]
    [DataRow("Triaged")]
    [DataRow("")]
    [DataRow(null)]
    public void Triage(string apiValue)
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.Triage = apiValue;
        var expected = TriageState.FromApiValue(apiValue);

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Triage.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(null, "New")]
    [DataRow("Pending", "Triage Pending")]
    [DataRow("More Info", "Triage waiting for info")]
    [DataRow("Info Received", "Triaging")]
    [DataRow("Triaged", "Triaged, waiting for approval")]
    public void Triage_StatusLabel(string? apiValue, string expected)
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.WorkItemType = WorkItemType.Bug.ToApiValue();
        workItem.Fields.State = ScrumState.New.ToApiValue();
        workItem.Fields.Triage = apiValue;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().StatusLabel.ShouldBe(expected);
    }

    #endregion

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Blocked(bool expected)
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.Blocked = expected;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Blocked.ShouldBe(expected);
    }

    [TestMethod]
    public void TargetDate_HasValue()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        var now = DateTimeOffset.Now;
        workItem.Fields.TargetDate = now;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().TargetDate.ShouldBe(now);
    }

    [TestMethod]
    public void TargetDate_Null()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.TargetDate = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().TargetDate.ShouldBeNull();
    }

    [TestMethod]
    public void Tags_Empty()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.Tags = null;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Tags.ShouldBeEmpty();
    }

    [TestMethod]
    public void Tags_OneTag()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.Tags = "Bug_Bounty";

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Tags.Count.ShouldBe(1);
        workItems.Single().Tags.Single().ShouldBe("Bug Bounty");
    }

    [TestMethod]
    public void Tags_TwoTags()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.Tags = "Bug_Bounty; DesignReview";

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single().Tags.Count.ShouldBe(2);
        workItems.Single().Tags.ShouldContain("Bug Bounty");
        workItems.Single().Tags.ShouldContain("DesignReview");
    }

    #endregion Properties

    #region Sprint Priority

    private List<AzureDevOps.Models.WorkItem> BuildWorkItems(Sprint sprint, int count)
    {
        var workItems = new List<AzureDevOps.Models.WorkItem>();

        for (var i = 0; i < count; i++)
        {
            _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
            workItems.Add(workItem);
        }

        return workItems;
    }

    [TestMethod]
    public void SprintPriority_SameAsBacklogProperty()
    {
        //Arrange
        var sprint = BuildSprint();
        var source = BuildWorkItems(sprint, 2);
        var firstWorkItem = source.SingleRandom();
        var secondWorkItem = source.Except(firstWorkItem.Yield()).Single();
        firstWorkItem.Fields.BacklogPriority = 5.0;
        secondWorkItem.Fields.BacklogPriority = 10.0;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single(wi => wi.Id == firstWorkItem.Id).SprintPriority.ShouldBe(1);
        workItems.Single(wi => wi.Id == secondWorkItem.Id).SprintPriority.ShouldBe(2);
    }

    [TestMethod]
    public void SprintPriority_ZeroBacklogPriority_LowestSprintPriority()
    {
        //Arrange
        var sprint = BuildSprint();
        var source = BuildWorkItems(sprint, 2);
        var firstWorkItem = source.SingleRandom();
        var secondWorkItem = source.Except(firstWorkItem.Yield()).Single();
        firstWorkItem.Fields.BacklogPriority = 5.0;
        secondWorkItem.Fields.BacklogPriority = 0.0;

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single(wi => wi.Id == firstWorkItem.Id).SprintPriority.ShouldBe(1);
        workItems.Single(wi => wi.Id == secondWorkItem.Id).SprintPriority.ShouldBe(2);
    }

    [TestMethod]
    public void DifferentSprints_SeparateSprintPrioritySequences()
    {
        //Arrange
        var sprint1 = BuildSprint();
        var source1 = BuildWorkItems(sprint1, 2);
        var firstWorkItem = source1.SingleRandom();
        var secondWorkItem = source1.Except(firstWorkItem.Yield()).Single();
        firstWorkItem.Fields.BacklogPriority = 5.0;
        secondWorkItem.Fields.BacklogPriority = 10.0;

        var sprint2 = BuildSprint();
        var source2 = BuildWorkItems(sprint2, 5).ToArray();
        for (var i = 0; i < source2.Length; i++)
        {
            source2[i].Fields.BacklogPriority = (i + 1) * 2.0;
        }

        //Act
        var workItems = GetWorkItems(sprint1, sprint2);

        //Assert
        workItems.Single(wi => wi.Id == firstWorkItem.Id).SprintPriority.ShouldBe(1);
        workItems.Single(wi => wi.Id == secondWorkItem.Id).SprintPriority.ShouldBe(2);
        workItems.Single(wi => wi.Id == source2[0].Id).SprintPriority.ShouldBe(1);
        workItems.Single(wi => wi.Id == source2[4].Id).SprintPriority.ShouldBe(5);
    }

    /// <summary>
    /// Done items do not get assigned a priority
    /// </summary>
    [TestMethod]
    public void SprintPriority_SkipDone()
    {
        //Arrange
        var sprint = BuildSprint();
        var source = BuildWorkItems(sprint, 2);
        var firstWorkItem = source.SingleRandom();
        var secondWorkItem = source.Except(firstWorkItem.Yield()).Single();
        firstWorkItem.Fields.BacklogPriority = 5.0;
        secondWorkItem.Fields.BacklogPriority = 10.0;

        firstWorkItem.Fields.State = ScrumState.Done.ToApiValue();

        //Act
        var workItems = GetWorkItems(sprint);

        //Assert
        workItems.Single(wi => wi.Id == firstWorkItem.Id).SprintPriority.ShouldBeNull();
        workItems.Single(wi => wi.Id == secondWorkItem.Id).SprintPriority.ShouldBe(1);
    }

    #endregion Sprint Priority
}