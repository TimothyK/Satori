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

    private Url GetUrl(IterationId iteration) =>
        _connectionSettings.Url
            .AppendPathSegments(iteration.ProjectName, iteration.TeamName)
            .AppendPathSegment("_apis/work/iterations")
            .AppendPathSegment(iteration.Id)
            .AppendPathSegment("workItemsOrder")
            .AppendQueryParam("api-version", "6.0-preview.1");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    #endregion Arrange

    #region Act

    private async Task<ReorderResult[]> ReorderAsync(IterationId iteration, ReorderOperation operation, RootObject<ReorderResult> expected)
    {
        //Arrange
        operation.IterationPath = iteration.IterationPath;
        _mockHttp.Expect(HttpMethod.Patch, GetUrl(iteration))
            .WithContent(JsonSerializer.Serialize(operation))
            .Respond("application/json",  JsonSerializer.Serialize(expected));
        
        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();
        //Act
        return await srv.ReorderBacklogWorkItemsAsync(iteration, operation);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var iteration = Builder<IterationId>.New().Build();
        var operation = Builder<ReorderOperation>.New().Build();
        var expected = new RootObject<ReorderResult>
        {
            Count = 1,
            Value = [new ReorderResult {Id = 45, Order = 12345.6}]
        };

        //Act
        var actual = await ReorderAsync(iteration, operation, expected);

        //Assert
        actual.Length.ShouldBe(1);
        actual[0].Id.ShouldBe(expected.Value[0].Id);
        actual[0].Order.ShouldBe(expected.Value[0].Order);
    }

}