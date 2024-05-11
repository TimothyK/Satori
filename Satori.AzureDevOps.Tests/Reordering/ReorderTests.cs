using Autofac;
using Builder;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Shouldly;
using System.Text.Json;

namespace Satori.AzureDevOps.Tests.Reordering;

[TestClass]
public class ReorderTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetUrl(TeamId team) =>
        _connectionSettings.Url
            .AppendPathSegments(team.ProjectName, team.Id)
            .AppendPathSegment("_apis/work/workItemsOrder")
            .AppendQueryParam("api-version", "6.0-preview.1");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    #endregion Arrange

    #region Act

    private ReorderResult[] Reorder(TeamId team, ReorderOperation operation, RootObject<ReorderResult> expected)
    {
        //Arrange
        _mockHttp.Expect(HttpMethod.Patch, GetUrl(team))
            .WithContent(JsonSerializer.Serialize(operation))
            .Respond("application/json",  JsonSerializer.Serialize(expected));
        
        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();
        //Act
        return srv.ReorderBacklogWorkItems(team, operation);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest()
    {
        //Arrange
        var team = Builder<TeamId>.New().Build();
        var operation = Builder<ReorderOperation>.New().Build();
        var expected = new RootObject<ReorderResult>
        {
            Count = 1,
            Value = [new ReorderResult {Id = 45, Order = 12345.6}]
        };

        //Act
        var actual = Reorder(team, operation, expected);

        //Assert
        actual.Length.ShouldBe(1);
        actual[0].Id.ShouldBe(expected.Value[0].Id);
        actual[0].Order.ShouldBe(expected.Value[0].Order);
    }

}