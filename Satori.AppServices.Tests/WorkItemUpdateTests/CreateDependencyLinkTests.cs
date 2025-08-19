using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
public class CreateDependencyLinkTests
{
    public CreateDependencyLinkTests()
    {
        Person.Me = null;  // Clear cache

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

    public WorkItemUpdateService Server { get; set; }
    private TestAzureDevOpsServer AzureDevOps { get; } = new();
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }
    private protected TestKimaiServer Kimai { get; } = new();

    private Task<WorkItem> BuildTaskAsync(string? title = null)
    {
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);

        task.Fields.State = ScrumState.InProgress.ToApiValue();
        task.Fields.Title = title ?? "Task " + RandomGenerator.String(5);

        return task.ToViewModelAsync(Kimai.AsInterface());
    }

    #region Act
    
    private async Task CreateDependencyLinkAsync(WorkItem predecessor, WorkItem successor)
    {
        await Server.CreateDependencyLinkAsync(predecessor, successor);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task CreatesDependencyLinkBetweenTasks()
    {
        // Arrange
        var predecessor = await BuildTaskAsync("Predecessor");
        var successor = await BuildTaskAsync("Successor");

        // Act
        await CreateDependencyLinkAsync(predecessor, successor);

        // Assert
        var srv = AzureDevOps.Mock.Object;
        var workItems = await srv.GetWorkItemsAsync(predecessor.Id, successor.Id);

        var predecessorDto = workItems.Single(wi => wi.Id == predecessor.Id);
        predecessorDto.Relations.Single(r => r.RelationType == LinkType.IsPredecessorOf.ToApiValue()).Url.ShouldBe(successor.ApiUrl);

        var successorDto = workItems.Single(wi => wi.Id == successor.Id);
        successorDto.Relations.Single(r => r.RelationType == LinkType.IsSuccessorOf.ToApiValue()).Url.ShouldBe(predecessor.ApiUrl);
    }

}
