using Microsoft.Extensions.DependencyInjection;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.Kimai;
using Shouldly;

namespace Satori.AppServices.Tests.WorkItemUpdateTests;

[TestClass]
public class CreateDependencyLinkTests
{
    private readonly ServiceProvider _serviceProvider;

    public CreateDependencyLinkTests()
    {
        Person.Me = null;  // Clear cache

        var services = new SatoriServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        Server = _serviceProvider.GetRequiredService<WorkItemUpdateService>();
        _serviceProvider.GetRequiredService<TestAzureDevOpsServer>().RequireRecordLocking = false;
    }

    #region Helpers

    public WorkItemUpdateService Server { get; set; }

    private Task<WorkItem> BuildTaskAsync(string? title = null)
    {
        var builder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
        builder.BuildWorkItem().AddChild(out var task);

        task.Fields.State = ScrumState.InProgress.ToApiValue();
        task.Fields.Title = title ?? "Task " + RandomGenerator.String(5);

        var kimai = _serviceProvider.GetRequiredService<IKimaiServer>();
        return task.ToViewModelAsync(kimai);
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
        var azureDevOpsServer = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        var workItems = await azureDevOpsServer.GetWorkItemsAsync(predecessor.Id, successor.Id);

        var predecessorDto = workItems.Single(wi => wi.Id == predecessor.Id);
        predecessorDto.Relations.Single(r => r.RelationType == LinkType.IsPredecessorOf.ToApiValue()).Url.ShouldBe(successor.ApiUrl);

        var successorDto = workItems.Single(wi => wi.Id == successor.Id);
        successorDto.Relations.Single(r => r.RelationType == LinkType.IsSuccessorOf.ToApiValue()).Url.ShouldBe(predecessor.ApiUrl);
    }

}
