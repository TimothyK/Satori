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
    private readonly RandomGenerator _random = new();

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
}