using Microsoft.Extensions.DependencyInjection;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AzureDevOps;
using Satori.Kimai;
using Satori.Kimai.ViewModels;
using Shouldly;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;
using WorkItemModel = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.WorkItemUpdateTests;

[TestClass]
public class UpdateProjectCodeTests
{
    private readonly ServiceProvider _serviceProvider;
    public WorkItemUpdateService Server { get; set; }
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }

    public UpdateProjectCodeTests()
    {
        var services = new SatoriServiceCollection();
        services.AddTransient<UserService>();
        services.AddTransient<WorkItemUpdateService>();
        _serviceProvider = services.BuildServiceProvider();

        Server = _serviceProvider.GetRequiredService<WorkItemUpdateService>();

        AzureDevOpsBuilder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
        _serviceProvider.GetRequiredService<TestAzureDevOpsServer>().RequireRecordLocking = false;

    }

    #region Helpers

    #region Arrange
    private async Task<WorkItem> BuildWorkItemAsync(Action<WorkItemModel>? arrangeWorkItem = null)
    {
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);

        arrangeWorkItem?.Invoke(workItem);

        var kimai = _serviceProvider.GetRequiredService<IKimaiServer>();
        return await workItem.ToViewModelAsync(kimai);
    }

    private async Task<Activity> BuildActivityAsync()
    {
        var kimai = _serviceProvider.GetRequiredService<TestKimaiServer>();
        var projectModel = kimai.AddProject();
        var activityModel = kimai.AddActivity(projectModel);

        //Let the Kimai server turn convert to ViewModels
        var customers = await _serviceProvider.GetRequiredService<IKimaiServer>().GetCustomersAsync();
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
        var azureDevOpsServer = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        var actual = (await azureDevOpsServer.GetWorkItemsAsync(workItem.Id)).Single();

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
        var azureDevOpsServer = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        var actual = (await azureDevOpsServer.GetWorkItemsAsync(workItem.Id)).Single();
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
        var azureDevOpsServer = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        var actual = (await azureDevOpsServer.GetWorkItemsAsync(workItem.Id)).Single();
        actual.Fields.ProjectCode.ShouldBeNullOrEmpty();

        workItem.KimaiProject.ShouldBeNull();
    }
}