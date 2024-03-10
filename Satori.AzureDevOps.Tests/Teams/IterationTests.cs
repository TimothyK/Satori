using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Teams.SampleFiles;
using Shouldly;
using System.Text;

namespace Satori.AzureDevOps.Tests.Teams;

[TestClass]
public class IterationTests
{

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetIterationUrl(Team team) =>
        _connectionSettings.Url
            .AppendPathSegments(team.ProjectName, team.Name)
            .AppendPathSegment("_apis")
            .AppendPathSegment("work/teamSettings/iterations")
            .AppendQueryParam("$timeframe", "Current")
            .AppendQueryParam("api-version", "6.1-preview");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

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
        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();
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
            .Id.ShouldBe(new Guid("e75e0e39-27e0-4b63-a518-a8040c8fbe12"));

    [TestMethod] 
    public void Name() => 
        GetRequiredIteration(SampleTeams.Active)
            .Name.ShouldBe("Sprint 2024-02");
    
    [TestMethod] 
    public void Path() => 
        GetRequiredIteration(SampleTeams.Active)
            .Path.ShouldBe("MyProject\\MyTeam\\Sprint 2024-02");

    [TestMethod] 
    public void StartDate() => 
        GetRequiredIteration(SampleTeams.Active)
            .Attributes.StartDate.ShouldBe(new DateTimeOffset(2024, 02, 05, 0, 0, 0, TimeSpan.Zero));

    private readonly DateTimeOffset _activeFinishDate = new(2024, 02, 23, 0, 0, 0, TimeSpan.Zero);

    [TestMethod] 
    public void FinishDate() =>
        GetRequiredIteration(SampleTeams.Active)
            .Attributes.FinishDate.ShouldBe(_activeFinishDate);

    [TestMethod]
    public void InactiveTeam_ReturnsNull() => GetIteration(SampleTeams.Inactive).ShouldBeNull();


    [TestMethod]
    public void UndatedTeam_ReturnsNull() => GetIteration(SampleTeams.Undated).ShouldBeNull();
}