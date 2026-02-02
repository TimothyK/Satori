using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Satori.AzureDevOps.Tests.Processes.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Processes;

[TestClass]
public class ProjectTests
{
    private readonly ServiceProvider _serviceProvider;

    public ProjectTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;

    private Url GetProjectsUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/projects")
            .AppendQueryParam("api-version", "7.1");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, string response)
    {
        _mockHttp.When(url).Respond("application/json", response);
    }

    #endregion Arrange

    #region Act

    private async Task<Project[]> GetProjectsAsync()
    {
        //Arrange
        SetResponse(GetProjectsUrl(), Responses.AllProjects);

        //Act
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        return await srv.GetProjectsAsync();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest() => (await GetProjectsAsync()).Length.ShouldBe(2);

    [TestMethod]
    public async Task Id() => 
        (await GetProjectsAsync())
        .Single(p => p.Id == new Guid("9a8ce311-a958-47a9-9475-2a30492b8385"))
        .Name.ShouldBe("ZeroBugProject");
}