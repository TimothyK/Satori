using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Tests.Globals;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Users;

[TestClass]
public class GetCurrentUserIdTests
{
    private readonly ServiceProvider _serviceProvider;

    public GetCurrentUserIdTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;


    private Url GetUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/ConnectionData")
            .AppendQueryParam("api-version", "6.0-preview.1");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        //Arrange
        SetResponse(GetUrl(), SampleFiles.SampleResponses.ConnectionData);

        //Act
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        var connectionData = await srv.GetCurrentUserAsync();
        return connectionData.AuthenticatedUser.Id;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod] 
    public async Task ASmokeTest() => 
        (await GetCurrentUserIdAsync())
        .ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));
}