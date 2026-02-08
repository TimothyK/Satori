using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Satori.AzureDevOps.Tests.Processes.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Processes;

[TestClass]
public class ProjectPropertyTests
{
    private readonly ServiceProvider _serviceProvider;

    public ProjectPropertyTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
        _mockHttp.Clear();
    }

    #region Helpers

    #region Arrange

    private readonly Guid _sampleProjectId = new("9a8ce311-a958-47a9-9475-2a30492b8385");
    private readonly ConnectionSettings _connectionSettings;

    private Url GetProjectPropertiesUrl(Guid projectId) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/projects")
            .AppendPathSegment(projectId)
            .AppendPathSegment("properties")
            .AppendQueryParam("api-version", "7.1-preview.1");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, string response)
    {
        _mockHttp.When(url).Respond("application/json", response);
    }

    #endregion Arrange

    #region Act

    private async Task<ProjectProperty[]> GetProjectPropertiesAsync(Guid projectId)
    {
        //Arrange
        SetResponse(GetProjectPropertiesUrl(projectId), Responses.ProjectProperties);

        //Act
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        return await srv.GetProjectPropertiesAsync(projectId);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest() => (await GetProjectPropertiesAsync(_sampleProjectId)).Length.ShouldBe(9);

    [TestMethod]
    public async Task ProcessTemplateType()
    {
        //Act
        var value = (await GetProjectPropertiesAsync(_sampleProjectId))
            .Single(p => p.Name == "System.ProcessTemplateType")
            .Value;

        //Assert
        value.ShouldNotBeNull();
        var actual = new Guid(value.ToString() ?? throw new InvalidOperationException());
        actual.ShouldBe(new Guid("8e2e94ad-d42a-4910-b80a-e7120580a5ed"));
    }
}