using Autofac;
using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Shouldly;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class TimerTests
{

    #region Helpers

    #region Arrange
    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private Url GetUrl(int id) =>
        _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id)
            .AppendPathSegment("stop");

    #endregion Arrange

    #region Act

    private async Task StopTimerAsync(int id, Func<HttpRequestMessage, bool> verifyRequest)
    {
        _mockHttp.When(GetUrl(id))
            .With(verifyRequest)
            .Respond("application/json", "{}");

        var srv = Globals.Services.Scope.Resolve<IKimaiServer>();
        await srv.StopTimerAsync(id);
    }
    
    #endregion Act


    #endregion Helpers

    [TestMethod]
    public async Task StopTimer()
    {
        await StopTimerAsync(1, VerifyRequest);
        return;

        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Patch);
            return true;
        }
    }
}