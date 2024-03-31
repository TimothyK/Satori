using Pscl.Linq;
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

    #endregion Child Tasks

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

    #endregion

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