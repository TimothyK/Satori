using Builder;
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

namespace Satori.AppServices.Tests.WorkItemUpdateTests;

[TestClass]
public class CreateTaskTests
{
    public CreateTaskTests()
    {
        Person.Me = null;  //Clear cache

        var userService = new UserService(AzureDevOps.AsInterface(), Kimai.AsInterface(), new AlertService());
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

    #endregion Arrange

    #region Act

    private async Task<WorkItem> CreateTaskAsync(WorkItem parent, string title, double estimate)
    {
        return await Server.CreateTaskAsync(parent, title, estimate);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var parent);
        
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent.ToViewModel(), title, estimate);

        //Assert
        task.ShouldNotBeNull();
        task.Title.ShouldBe(title);
    }
    
    [TestMethod]
    public async Task Estimate_RoundedToDime()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var parent);
        
        var title = RandomGenerator.String();
        const double precision = 0.1;
        var expected = RandomGenerator.Number(2.5).ToNearest(precision);
        var estimate = expected + 0.034;

        //Act
        var task = await CreateTaskAsync(parent.ToViewModel(), title, estimate);

        //Assert
        task.OriginalEstimate.ShouldNotBeNull();
        task.OriginalEstimate.Value.TotalHours.ShouldBe(expected);
    }
    
    [TestMethod]
    public async Task Remaining_SameAsOriginalEstimate()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var parent);
        
        var title = RandomGenerator.String();
        const double precision = 0.1;
        var expected = RandomGenerator.Number(2.5).ToNearest(precision);
        var estimate = expected + 0.034;

        //Act
        var task = await CreateTaskAsync(parent.ToViewModel(), title, estimate);

        //Assert
        task.OriginalEstimate.ShouldNotBeNull();
        task.OriginalEstimate.Value.TotalHours.ShouldBe(expected);
        task.RemainingWork.ShouldNotBeNull();
        task.RemainingWork.Value.TotalHours.ShouldBe(expected);
    }

    [TestMethod]
    public async Task AssignedToMe()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var parent);
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent.ToViewModel(), title, estimate);

        //Assert
        task.AssignedTo.ShouldBe(Person.Me);
    }
    
    [TestMethod]
    public async Task InProgress()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var parent);
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent.ToViewModel(), title, estimate);

        //Assert
        task.State.ShouldBe(ScrumState.InProgress);
    }
    
    [TestMethod]
    public async Task ParentChildLinked()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);
        var parent = workItem.ToViewModel();
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent, title, estimate);

        //Assert
        task.Parent.ShouldBe(parent);
        parent.Children.ShouldContain(task);
    }
    
    [TestMethod]
    public async Task AreaPath()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);
        var parent = workItem.ToViewModel();
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent, title, estimate);

        //Assert
        task.AreaPath.ShouldBe(parent.AreaPath);
    }
    
    [TestMethod]
    public async Task Iteration()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);
        var parent = workItem.ToViewModel();
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent, title, estimate);

        //Assert
        task.IterationPath.ShouldBe(parent.IterationPath);
    }
}