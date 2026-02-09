using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Satori.AzureDevOps.Tests.Processes.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Processes;

[TestClass]
public class BacklogConfigurationTests
{
    private readonly ServiceProvider _serviceProvider;

    public BacklogConfigurationTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
        _mockHttp.Clear();
    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;

    private Url GetUrl(string projectName, string teamName) =>
        _connectionSettings.Url
            .AppendPathSegment(projectName)
            .AppendPathSegment(teamName)
            .AppendPathSegment("_apis/work/backlogConfiguration")
            .AppendQueryParam("api-version", "7.1");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, string response)
    {
        _mockHttp.When(url).Respond("application/json", response);
    }

    #endregion Arrange

    #region Act

    private async Task<BacklogConfiguration> GetBacklogConfigurationAsync()
    {
        const string projectName = "ScrumProject";
        const string teamName = "CoreTeam";

        //Arrange
        var response = Responses.ScrumBacklogConfiguration;
        SetResponse(GetUrl(projectName, teamName), response);

        //Act
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        return await srv.GetBacklogConfigAsync(projectName, teamName);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest() => (await GetBacklogConfigurationAsync()).ShouldNotBeNull();

    [TestMethod]
    public async Task TaskBacklog()
    {
        //Act
        var config = await GetBacklogConfigurationAsync();

        //Assert
        config.TaskBacklog.Name.ShouldBe("Tasks");
        config.TaskBacklog.Rank.ShouldBe(1);
        config.TaskBacklog.WorkItemTypes.Length.ShouldBe(1);
        config.TaskBacklog.WorkItemTypes.Single().Name.ShouldBe("Task");
    }
    
    [TestMethod]
    public async Task RequirementsBacklog()
    {
        //Act
        var config = await GetBacklogConfigurationAsync();

        //Assert
        config.RequirementBacklog.Name.ShouldBe("Backlog items");
        config.RequirementBacklog.Rank.ShouldBe(2);
        config.RequirementBacklog.WorkItemTypes.Length.ShouldBe(2);
        config.RequirementBacklog.WorkItemTypes.Select(t => t.Name).ShouldContain("Bug");
        config.RequirementBacklog.WorkItemTypes.Select(t => t.Name).ShouldContain("Product Backlog Item");
    }
    
    [TestMethod]
    public async Task FeaturesBacklog()
    {
        //Act
        var config = await GetBacklogConfigurationAsync();

        //Assert
        var backlog = config.PortfolioBacklogs.Single(backlog => backlog.Rank == config.RequirementBacklog.Rank + 1);
        backlog.Name.ShouldBe("Features");
        backlog.Rank.ShouldBe(3);
        backlog.WorkItemTypes.Length.ShouldBe(1);
        backlog.WorkItemTypes.Single().Name.ShouldBe("Feature");
    }
    
    [TestMethod]
    public async Task EpicsBacklog()
    {
        //Act
        var config = await GetBacklogConfigurationAsync();

        //Assert
        var backlog = config.PortfolioBacklogs.Single(backlog => backlog.Rank == config.RequirementBacklog.Rank + 2);
        backlog.Name.ShouldBe("Epics");
        backlog.Rank.ShouldBe(4);
        backlog.WorkItemTypes.Length.ShouldBe(1);
        backlog.WorkItemTypes.Single().Name.ShouldBe("Epic");
    }
}