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

namespace Satori.AppServices.Tests.WorkItemUpdateTests;

[TestClass]
public class CreateTaskTests
{
    public CreateTaskTests()
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

    #endregion Arrange

    #region Act

    private async Task<WorkItem> CreateTaskAsync(AzureDevOps.Models.WorkItem parent, string title, double estimate)
    {
        //Arrange
        await Kimai.AsInterface().InitializeCustomersForWorkItems();

        //Act
        var viewModel = parent.ToViewModel();
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