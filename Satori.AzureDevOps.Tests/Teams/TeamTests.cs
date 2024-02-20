using Flurl;
using Microsoft.Extensions.Logging.Abstractions;
using Pscl.CommaSeparatedValues;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Teams.SampleFiles;
using Satori.AzureDevOps.Tests.WorkItems.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Teams;

[TestClass]
public class TeamTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = new()
    {
        Url = new Uri("http://devops.test/Org"),
        PersonalAccessToken = "test"
    };

    private Url GetTeamsUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/teams")
            .AppendQueryParam("api-version", "6.0-preview.2");

    private readonly MockHttpMessageHandler _mockHttp = new();

    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private Team[] GetTeams()
    {
        var srv = new AzureDevOpsServer(_connectionSettings, _mockHttp.ToHttpClient(), NullLoggerFactory.Instance);
        return srv.GetTeamsAsync().Result;
    }

    private Team SingleTeam()
    {
        //Arrange
        SetResponse(GetTeamsUrl(), TeamResponses.SingleTeam);

        //Act
        var teams = GetTeams();

        //Assert
        teams.Length.ShouldBe(1);
        return teams.Single();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod] public void ASmokeTest() => SingleTeam().id.ShouldBe(new Guid("91d8c103-651c-4c92-8f41-f8c2b67c8b9d"));
    [TestMethod] public void Name() => SingleTeam().name.ShouldBe("MyTeam");
    [TestMethod] public void ProjectName() => SingleTeam().projectName.ShouldBe("MyProject");

}