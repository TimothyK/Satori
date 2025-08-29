using Microsoft.Extensions.DependencyInjection;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Kimai;
using Shouldly;

namespace Satori.AppServices.Tests.WorkItemUpdateTests;

[TestClass]
public class CreateTaskTests
{
    private readonly ServiceProvider _serviceProvider;

    public CreateTaskTests()
    {
        var services = new SatoriServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        Server = _serviceProvider.GetRequiredService<WorkItemUpdateService>();

        AzureDevOpsBuilder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
    }

    #region Helpers

    #region Arrange

    public WorkItemUpdateService Server { get; set; }
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }

    #endregion Arrange

    #region Act

    private async Task<WorkItem> CreateTaskAsync(AzureDevOps.Models.WorkItem parent, string title, double estimate)
    {
        //Arrange

        //Act
        var kimai = _serviceProvider.GetRequiredService<IKimaiServer>();
        var viewModel = await parent.ToViewModelAsync(kimai);
        var task = await Server.CreateTaskAsync(viewModel, title, estimate);

        //Assert
        task.Parent.ShouldBeSameAs(viewModel);
        return task;
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
        var task = await CreateTaskAsync(parent, title, estimate);

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
        var task = await CreateTaskAsync(parent, title, estimate);

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
        var task = await CreateTaskAsync(parent, title, estimate);

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
        var task = await CreateTaskAsync(parent, title, estimate);

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
        var task = await CreateTaskAsync(parent, title, estimate);

        //Assert
        task.State.ShouldBe(ScrumState.InProgress);
    }
    
    [TestMethod]
    public async Task ParentChildLinked()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var parent);
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent, title, estimate);

        //Assert
        task.Parent.ShouldNotBeNull();
        task.Parent.Children.ShouldContain(task);
    }
    
    [TestMethod]
    public async Task AreaPath()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var parent);
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent, title, estimate);

        //Assert
        task.Parent.ShouldNotBeNull();
        task.AreaPath.ShouldBe(task.Parent.AreaPath);
    }
    
    [TestMethod]
    public async Task Iteration()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var parent);
        var title = RandomGenerator.String();
        var estimate = RandomGenerator.Number(2.5);

        //Act
        var task = await CreateTaskAsync(parent, title, estimate);

        //Assert
        task.Parent.ShouldNotBeNull();
        task.IterationPath.ShouldBe(task.Parent.IterationPath);
    }
}