using Flurl;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Teams.SampleFiles;
using Shouldly;
using System.Net;
using System.Text;

namespace Satori.AzureDevOps.Tests.Teams;

[TestClass]
public class IterationTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = new()
    {
        Url = new Uri("http://devops.test/Org"),
        PersonalAccessToken = "test"
    };

    private Url GetIterationUrl(Team team) =>
        _connectionSettings.Url
            .AppendPathSegments(team.projectName, team.name)
            .AppendPathSegment("_apis")
            .AppendPathSegment("work/teamSettings/iterations")
            .AppendQueryParam("$timeframe", "Current")
            .AppendQueryParam("api-version", "6.1-preview");

    private readonly MockHttpMessageHandler _mockHttp = new();

    private void SetResponse(Team team)
    {
        var url = GetIterationUrl(team);
        var payload = team.GetPayload();
        var statusCode = team.GetHttpStatusCode();

        _mockHttp.When(url).Respond(GetResponse);

        return;
        Task<HttpResponseMessage> GetResponse()
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(Encoding.Default.GetString(payload), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    #endregion Arrange

    #region Act

    private Iteration? GetIteration(Team team)
    {
        //Arrange
        SetResponse(team);

        //Act
        var srv = new AzureDevOpsServer(_connectionSettings, _mockHttp.ToHttpClient(), NullLoggerFactory.Instance);
        return srv.GetCurrentIterationAsync(team).Result;
    }

    private Iteration GetRequiredIteration(Team team)
    {
        var iteration = GetIteration(team);

        iteration.ShouldNotBeNull();
        return iteration;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod] 
    public void ASmokeTest() => 
        GetRequiredIteration(SampleTeams.Active)
            .id.ShouldBe(new Guid("e75e0e39-27e0-4b63-a518-a8040c8fbe12"));

    [TestMethod] 
    public void Name() => 
        GetRequiredIteration(SampleTeams.Active)
            .name.ShouldBe("Sprint 2024-02");
    
    [TestMethod] 
    public void Path() => 
        GetRequiredIteration(SampleTeams.Active)
            .path.ShouldBe("MyProject\\MyTeam\\Sprint 2024-02");

    [TestMethod] 
    public void StartDate() => 
        GetRequiredIteration(SampleTeams.Active)
            .attributes.startDate.ShouldBe(new DateTimeOffset(2024, 02, 05, 0, 0, 0, TimeSpan.Zero));
    
    [TestMethod] 
    public void FinishDate() => 
        GetRequiredIteration(SampleTeams.Active)
            .attributes.finishDate.ShouldBe(new DateTimeOffset(2024, 02, 23, 0, 0, 0, TimeSpan.Zero));

    [TestMethod]
    public void InactiveTeam_ReturnsNull() => GetIteration(SampleTeams.Inactive).ShouldBeNull();
}