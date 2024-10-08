﻿using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Teams.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Teams;

[TestClass]
public class TeamTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetTeamsUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/teams")
            .AppendQueryParam("api-version", "6.0-preview.2");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private static Team[] GetTeams()
    {
        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();
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

    [TestMethod] public void ASmokeTest() => SingleTeam().Id.ShouldBe(new Guid("91d8c103-651c-4c92-8f41-f8c2b67c8b9d"));
    [TestMethod] public void Name() => SingleTeam().Name.ShouldBe("MyTeam");
    [TestMethod] public void ProjectName() => SingleTeam().ProjectName.ShouldBe("MyProject");
    [TestMethod] public void Url() => SingleTeam().Url.ShouldBe("http://devops.test/Org/_apis/projects/673f1d5f-4346-455a-a1b1-9f5128416cb6/teams/91d8c103-651c-4c92-8f41-f8c2b67c8b9d");

}