using Autofac;
using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Shouldly;
using System.Text.Json;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class ExportTests
{

    #region Helpers

    #region Arrange
    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private Url GetUrl(int id) =>
        _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id)
            .AppendPathSegment("export");

    #endregion Arrange

    #region Act

    private async Task ExportTimeEntryAsync(int id, Func<HttpRequestMessage, bool> verifyRequest)
    {
        _mockHttp.When(GetUrl(id))
            .With(verifyRequest)
            .Respond("application/json", "{}");

        var srv = Globals.Services.Scope.Resolve<IKimaiServer>();
        await srv.ExportTimeSheetAsync(id);
    }
    
    #endregion Act


    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        await ExportTimeEntryAsync(1, VerifyRequest);
        return;

        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Patch);
            return true;
        }
    }
}