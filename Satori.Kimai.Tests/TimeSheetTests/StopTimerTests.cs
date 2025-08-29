using Flurl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Satori.Kimai.Tests.Globals;
using Satori.Kimai.Tests.TimeSheetTests.SampleFiles;
using Shouldly;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class StopTimerTests
{
    private readonly ServiceProvider _serviceProvider;

    public StopTimerTests()
    {
        var services = new KimaiServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;

    private readonly MockHttpMessageHandler _mockHttp;

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
            .Respond("application/json", System.Text.Encoding.Default.GetString(SampleResponses.TimeEntryCollapsed));

        var srv = _serviceProvider.GetRequiredService<IKimaiServer>();
        var end = await srv.StopTimerAsync(id);

        end.ShouldBe(new DateTimeOffset(2025, 1, 12, 16, 54, 0, TimeSpan.FromHours(-7)));
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