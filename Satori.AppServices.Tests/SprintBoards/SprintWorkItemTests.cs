using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles.Builders;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AppServices.Tests.TestDoubles.Services;
using Satori.AppServices.ViewModels.Sprints;
using Shouldly;

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
        var srv = new SprintBoardService(_azureDevOpsServer.AsInterface(), _timeServer);

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
        vm.AssignedTo.Id.ShouldBe(workItem.Fields.AssignedTo.Id);
        vm.AssignedTo.DisplayName.ShouldBe(workItem.Fields.AssignedTo.DisplayName);
        vm.AssignedTo.AvatarUrl.ShouldBe(workItem.Fields.AssignedTo.ImageUrl);
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
        vm.AssignedTo.ShouldBeNull();
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
        vm.CreatedBy.Id.ShouldBe(workItem.Fields.CreatedBy.Id);
        vm.CreatedBy.DisplayName.ShouldBe(workItem.Fields.CreatedBy.DisplayName);
        vm.CreatedBy.AvatarUrl.ShouldBe(workItem.Fields.CreatedBy.ImageUrl);
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

    #endregion Properties

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
}