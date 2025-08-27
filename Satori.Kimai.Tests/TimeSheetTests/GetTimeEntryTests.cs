using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Satori.Kimai.Models;
using Satori.Kimai.Tests.Globals;
using Shouldly;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class GetTimeEntryTests
{
    private readonly ServiceProvider _serviceProvider;

    public GetTimeEntryTests()
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
            .AppendPathSegment(id);

    #endregion Arrange

    #region Act

    private async Task<TimeEntryCollapsed> GetTimeEntryAsync(int id, Func<HttpRequestMessage, bool> verifyRequest)
    {
        //Arrange
        var response = Builder.Builder<TimeEntryCollapsed>.New().Build(int.MaxValue);
        response.Id = id;

        _mockHttp.When(GetUrl(id))
            .With(verifyRequest)
            .Respond("application/json", JsonSerializer.Serialize(response));

        //Act
        var srv = _serviceProvider.GetRequiredService<IKimaiServer>();
        return await srv.GetTimeEntryAsync(id);
    }
    
    #endregion Act


    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        const int id = 1;

        //Act
        var response = await GetTimeEntryAsync(id, VerifyRequest);

        //Assert
        response.ShouldNotBeNull();
        response.Id.ShouldBe(id);
        return;

        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Get);
            return true;
        }
    }
}