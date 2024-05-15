using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Users;

[TestClass]
public class GetCurrentUserIdTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/ConnectionData")
            .AppendQueryParam("api-version", "6.0-preview.1");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private Guid GetCurrentUserId()
    {
        //Arrange
        SetResponse(GetUrl(), SampleFiles.SampleResponses.ConnectionData);

        //Act
        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();
        return srv.GetCurrentUserIdAsync().Result;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod] public void ASmokeTest() => GetCurrentUserId().ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));
}