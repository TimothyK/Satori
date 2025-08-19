using CodeMonkeyProjectiles.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;
using AzureDevOpsWorkItem = Satori.AzureDevOps.Models.WorkItem;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Tests.WorkItemUpdateTests;

[TestClass]
public class UpdateTaskTests
{
    public UpdateTaskTests()
    {
        Person.Me = null;  //Clear cache

        var services = new ServiceCollection();
        services.AddSingleton(AzureDevOps.AsInterface());
        services.AddSingleton(Kimai.AsInterface());
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IAlertService>(new AlertService());
        services.AddTransient<UserService>();
        services.AddTransient<WorkItemUpdateService>();

        var serviceProvider = services.BuildServiceProvider();

        Server = serviceProvider.GetRequiredService<WorkItemUpdateService>();

        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }

    #region Helpers

    #region Arrange

    public WorkItemUpdateService Server { get; set; }
    private TestAzureDevOpsServer AzureDevOps { get; } = new();
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }
    private protected TestKimaiServer Kimai { get; } = new();

    private async Task<WorkItem> BuildTaskAsync(Action<AzureDevOpsWorkItem>? arrangeWorkItem = null)
    {
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        
        var remaining = RandomGenerator.TimeSpan(TimeSpan.FromHours(2.5)).ToNearest(TimeSpan.FromMinutes(6));
        task.Fields.OriginalEstimate = remaining.TotalHours;
        task.Fields.RemainingWork = remaining.TotalHours;
        task.Fields.AssignedTo = AzureDevOps.Identity.ToUser();

        arrangeWorkItem?.Invoke(task);

        return await task.ToViewModelAsync(Kimai.AsInterface());
    }

    #endregion Arrange

    #region Act

    private async Task UpdateTaskAsync(WorkItem task, ScrumState state, TimeSpan? remaining = null)
    {
        await Server.UpdateTaskAsync(task, state, remaining);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest_NoChanges_NotUpdated()
    {
        //Arrange
        var task = await BuildTaskAsync();

        //Verify BuildTask behaviour
        task.State.ShouldBe(ScrumState.InProgress);
        task.RemainingWork.ShouldNotBeNull();
        var originalRev = task.Rev;

        //Act
        await UpdateTaskAsync(task, task.State, task.RemainingWork);

        //Assert
        task.Rev.ShouldBe(originalRev);
    }

    [TestMethod]
    public async Task Done_Updated()
    {
        //Arrange
        var task = await BuildTaskAsync();
        var originalRev = task.Rev;

        //Act
        await UpdateTaskAsync(task, ScrumState.Done);

        //Assert
        task.Rev.ShouldBe(originalRev + 1);
        task.State.ShouldBe(ScrumState.Done);
    }

    [TestMethod]
    public async Task NonTask_NotUpdated()
    {
        //Arrange
        var nonTask = WorkItemType.All().Except(WorkItemType.Task.Yield());
        var type = RandomGenerator.PickOne(nonTask);
        var task = await BuildTaskAsync(t => t.Fields.WorkItemType = type.ToApiValue());
        var originalRev = task.Rev;

        //Act
        await UpdateTaskAsync(task, ScrumState.Done);

        //Assert
        task.Rev.ShouldBe(originalRev);
    }

    [TestMethod]
    public async Task RemainingWork_Updated()
    {
        //Arrange
        var task = await BuildTaskAsync();
        var remaining = (task.RemainingWork ?? throw new InvalidOperationException())
            .Add(TimeSpan.FromHours(1.5));
        var originalRev = task.Rev;

        //Act
        await UpdateTaskAsync(task, task.State, remaining);

        //Assert
        task.Rev.ShouldBe(originalRev + 1);
        task.State.ShouldBe(task.State);
        task.RemainingWork.ShouldBe(remaining);
    }
    
    [TestMethod]
    public async Task RemainingWork_RoundedToDime()
    {
        //Arrange
        var task = await BuildTaskAsync();
        var remaining = (task.RemainingWork ?? throw new InvalidOperationException())
            .Add(TimeSpan.FromHours(1.5))
            .Add(TimeSpan.FromMinutes(2.345));
        var originalRev = task.Rev;

        //Act
        await UpdateTaskAsync(task, task.State, remaining);

        //Assert
        task.Rev.ShouldBe(originalRev + 1);
        task.State.ShouldBe(task.State);
        task.RemainingWork.ShouldBe(remaining.ToNearest(TimeSpan.FromMinutes(6)));
    }

    [TestMethod]
    public async Task OriginalEstimate_Updated()
    {
        //Arrange
        var task = await BuildTaskAsync(t =>
        {
            t.Fields.State = ScrumState.New.ToApiValue();
            t.Fields.OriginalEstimate = null;
            t.Fields.RemainingWork = null;
        });
        var remaining = RandomGenerator.TimeSpan(TimeSpan.FromHours(2.5))
            .ToNearest(TimeSpan.FromMinutes(6));
        var originalRev = task.Rev;

        //Act
        await UpdateTaskAsync(task, task.State, remaining);

        //Assert
        task.Rev.ShouldBe(originalRev + 1);
        task.State.ShouldBe(task.State);
        task.OriginalEstimate.ShouldBe(remaining);
        task.RemainingWork.ShouldBe(remaining);
    }
    
    [TestMethod]
    public async Task OriginalEstimate_HasValue_NotUpdated()
    {
        //Arrange
        var task = await BuildTaskAsync();
        var expected = task.OriginalEstimate;
        var remaining = (task.RemainingWork ?? throw new InvalidOperationException())
            .Add(TimeSpan.FromHours(1.5));

        //Act
        await UpdateTaskAsync(task, task.State, remaining);

        //Assert
        task.OriginalEstimate.ShouldBe(expected);
    }

    [TestMethod]
    public async Task State_ToDo_Updated()
    {
        //Arrange
        var task = await BuildTaskAsync();
        var originalRev = task.Rev;

        //Act
        await UpdateTaskAsync(task, ScrumState.ToDo);

        //Assert
        task.Rev.ShouldBe(originalRev + 1);
        task.State.ShouldBe(ScrumState.ToDo);
    }
    
    [TestMethod]
    public async Task AssignToSomeoneElse_NotUpdated()
    {
        //Arrange
        var task = await BuildTaskAsync(t => t.Fields.AssignedTo = null);
        var originalRev = task.Rev;

        //Act
        await Should.ThrowAsync<InvalidOperationException>(() => UpdateTaskAsync(task, ScrumState.Done));

        //Assert
        task.Rev.ShouldBe(originalRev);
    }
}