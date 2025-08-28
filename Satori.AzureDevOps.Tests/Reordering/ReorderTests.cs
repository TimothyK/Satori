using Builder;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Shouldly;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Satori.AzureDevOps.Tests.Globals;

namespace Satori.AzureDevOps.Tests.Reordering;

[TestClass]
public class ReorderTests
{
    private readonly ServiceProvider _serviceProvider;

    public ReorderTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();

    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;


    private Url GetUrl(IterationId iteration) =>
        _connectionSettings.Url
            .AppendPathSegments(iteration.ProjectName, iteration.TeamName)
            .AppendPathSegment("_apis/work/iterations")
            .AppendPathSegment(iteration.Id)
            .AppendPathSegment("workItemsOrder")
            .AppendQueryParam("api-version", "6.0-preview.1");

    private readonly MockHttpMessageHandler _mockHttp;

    #endregion Arrange

    #region Act

    private async Task<ReorderResult[]> ReorderAsync(IterationId iteration, ReorderOperation operation, RootObject<ReorderResult> expected)
    {
        //Arrange
        operation.IterationPath = iteration.IterationPath;
        _mockHttp.Expect(HttpMethod.Patch, GetUrl(iteration))
            .WithContent(JsonSerializer.Serialize(operation))
            .Respond("application/json",  JsonSerializer.Serialize(expected));
        
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
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