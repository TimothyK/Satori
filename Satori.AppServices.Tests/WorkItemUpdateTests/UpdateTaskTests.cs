using Builder;
using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Kimai.Models;
using Shouldly;
using AzureDevOpsWorkItem = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.WorkItemUpdateTests;

[TestClass]
public class UpdateTaskTests
{
    public UpdateTaskTests()
    {
        Person.Me = null;  //Clear cache

        var userService = new UserService(AzureDevOps.AsInterface(), Kimai.AsInterface());
        Server = new WorkItemUpdateService(AzureDevOps.AsInterface(), userService);

        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }

    #region Helpers

    #region Arrange

    public WorkItemUpdateService Server { get; set; }
    private TestAzureDevOpsServer AzureDevOps { get; } = new();
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }
    private protected TestKimaiServer Kimai { get; } = new() {CurrentUser = DefaultUser};

    protected static readonly User DefaultUser = Builder<User>.New().Build(user =>
    {
        user.Id = Sequence.KimaiUserId.Next();
        user.Enabled = true;
        user.Language = "en_CA";
    });

    private WorkItem BuildTask(Action<AzureDevOpsWorkItem>? arrangeWorkItem = null)
    {
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        
        var remaining = RandomGenerator.TimeSpan(TimeSpan.FromHours(2.5)).ToNearest(TimeSpan.FromMinutes(6));
        task.Fields.OriginalEstimate = remaining.TotalHours;
        task.Fields.RemainingWork = remaining.TotalHours;

        arrangeWorkItem?.Invoke(task);

        return task.ToViewModel();
    }

    #endregion Arrange

    #region Act

    private async Task<WorkItem> UpdateTaskAsync(WorkItem task, ScrumState state, TimeSpan? remaining = null)
    {
        return await Server.UpdateTaskAsync(task, state, remaining);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest_NoChanges_NotUpdated()
    {
        //Arrange
        var task = BuildTask();

        //Verify BuildTask behaviour
        task.State.ShouldBe(ScrumState.InProgress);
        task.RemainingWork.ShouldNotBeNull();

        //Act
        var actual = await UpdateTaskAsync(task, task.State, task.RemainingWork);

        //Assert
        actual.ShouldNotBeNull();
        actual.Rev.ShouldBe(task.Rev);
    }

    [TestMethod]
    public async Task Done_Updated()
    {
        //Arrange
        var task = BuildTask();

        //Act
        var actual = await UpdateTaskAsync(task, ScrumState.Done);

        //Assert
        actual.ShouldNotBeNull();
        actual.Rev.ShouldBe(task.Rev + 1);
        actual.State.ShouldBe(ScrumState.Done);
    }

    [TestMethod]
    public async Task NonTask_NotUpdated()
    {
        //Arrange
        var nonTask = WorkItemType.All().Except(WorkItemType.Task.Yield());
        var type = RandomGenerator.PickOne(nonTask);
        var task = BuildTask(t => t.Fields.WorkItemType = type.ToApiValue());

        //Act
        var actual = await UpdateTaskAsync(task, ScrumState.Done);

        //Assert
        actual.ShouldNotBeNull();
        actual.Rev.ShouldBe(task.Rev);
    }

    [TestMethod]
    public async Task RemainingWork_Updated()
    {
        //Arrange
        var task = BuildTask();
        var remaining = (task.RemainingWork ?? throw new InvalidOperationException())
            .Add(TimeSpan.FromHours(1.5));

        //Act
        var actual = await UpdateTaskAsync(task, task.State, remaining);

        //Assert
        actual.ShouldNotBeNull();
        actual.Rev.ShouldBe(task.Rev + 1);
        actual.State.ShouldBe(task.State);
        actual.RemainingWork.ShouldBe(remaining);
    }
    
    [TestMethod]
    public async Task RemainingWork_RoundedToDime()
    {
        //Arrange
        var task = BuildTask();
        var remaining = (task.RemainingWork ?? throw new InvalidOperationException())
            .Add(TimeSpan.FromHours(1.5))
            .Add(TimeSpan.FromMinutes(2.345));

        //Act
        var actual = await UpdateTaskAsync(task, task.State, remaining);

        //Assert
        actual.ShouldNotBeNull();
        actual.Rev.ShouldBe(task.Rev + 1);
        actual.State.ShouldBe(task.State);
        actual.RemainingWork.ShouldBe(remaining.ToNearest(TimeSpan.FromMinutes(6)));
    }

    [TestMethod]
    public async Task State_ToDo_Updated()
    {
        //Arrange
        var task = BuildTask();

        //Act
        var actual = await UpdateTaskAsync(task, ScrumState.ToDo);

        //Assert
        actual.ShouldNotBeNull();
        actual.Rev.ShouldBe(task.Rev + 1);
        actual.State.ShouldBe(ScrumState.ToDo);
    }
}