using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels;
using Satori.Kimai.Utilities;
using Satori.Kimai.ViewModels;
using Shouldly;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;
using WorkItemModel = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.WorkItemUpdateTests;

[TestClass]
public class UpdateProjectCodeTests
{
    public WorkItemUpdateService Server { get; set; }
    private TestAzureDevOpsServer AzureDevOps { get; } = new();
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }
    private protected TestKimaiServer Kimai { get; } = new();

    public UpdateProjectCodeTests()
    {
        //Person.Me = null;  //Clear cache
        AzureDevOps.RequireRecordLocking = false;

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
    private async Task<WorkItem> BuildWorkItemAsync(Action<WorkItemModel>? arrangeWorkItem = null)
    {
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);

        arrangeWorkItem?.Invoke(workItem);

        return await workItem.ToViewModelAsync(Kimai.AsInterface());
    }

    private async Task<Activity> BuildActivityAsync()
    {
        var projectModel = Kimai.AddProject();
        var activityModel = Kimai.AddActivity(projectModel);

        //Let the Kimai server turn convert to ViewModels
        var customers = await Kimai.AsInterface().GetCustomersAsync();
        var projectViewModel = customers.SelectMany(customer => customer.Projects).Single(p => p.Id == projectModel.Id);
        var activityViewModel = projectViewModel.Activities.Single(a => a.Id == activityModel.Id);

        return activityViewModel;
    }

    #endregion Arrange

    #region Act

    private async Task UpdateProjectCodeAsync(WorkItem workItem, Project? project, Activity? activity = null)
    {
        await Server.UpdateProjectCodeAsync(workItem, project, activity);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest_AzureDevOpsUpdated()
    {
        //Arrange
        var workItem = await BuildWorkItemAsync();

        //Verify BuildTask behaviour
        var originalRev = workItem.Rev;

        var activity = await BuildActivityAsync();

        //Act
        await UpdateProjectCodeAsync(workItem, activity.Project, activity);

        //Assert
        var actual = (await AzureDevOps.AsInterface().GetWorkItemsAsync(workItem.Id)).Single();

        actual.Fields.ProjectCode.ShouldNotBeNull();
        actual.Fields.ProjectCode.ShouldBe($"{activity.Project.ProjectCode}.{activity.ActivityCode}");

        actual.Rev.ShouldBe(originalRev + 1);
    }

    [TestMethod]
    public async Task ASmokeTest_ViewModelUpdated()
    {
        //Arrange
        var workItem = await BuildWorkItemAsync();

        //Verify BuildTask behaviour
        var originalRev = workItem.Rev;

        var activity = await BuildActivityAsync();

        //Act
        await UpdateProjectCodeAsync(workItem, activity.Project, activity);

        //Assert
        workItem.KimaiActivity.ShouldNotBeNull();
        workItem.KimaiActivity.Id.ShouldBe(activity.Id);

        workItem.KimaiProject.ShouldNotBeNull();
        workItem.KimaiProject.Id.ShouldBe(activity.Project.Id);

        workItem.Rev.ShouldBe(originalRev + 1);
    }

    [TestMethod]
    public async Task OnlyProject()
    {
        //Arrange
        var workItem = await BuildWorkItemAsync();
        var activity = await BuildActivityAsync();

        //Act
        await UpdateProjectCodeAsync(workItem, activity.Project);

        //Assert
        var actual = (await AzureDevOps.AsInterface().GetWorkItemsAsync(workItem.Id)).Single();
        actual.Fields.ProjectCode.ShouldNotBeNull();
        actual.Fields.ProjectCode.ShouldBe($"{activity.Project.ProjectCode}");

        workItem.KimaiProject.ShouldNotBeNull();
        workItem.KimaiProject.Id.ShouldBe(activity.Project.Id);
    }
    
    [TestMethod]
    public async Task ClearProjectCode()
    {
        //Arrange
        var workItem = await BuildWorkItemAsync();

        //Act
        await UpdateProjectCodeAsync(workItem, null);

        //Assert
        var actual = (await AzureDevOps.AsInterface().GetWorkItemsAsync(workItem.Id)).Single();
        actual.Fields.ProjectCode.ShouldBeNullOrEmpty();

        workItem.KimaiProject.ShouldBeNull();
    }
}