using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Satori.Kimai.Models;
using Shouldly;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Satori.Kimai.Tests.Extensions;
using Satori.Kimai.Tests.Globals;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class CreateTimeEntryTests
{
    private readonly ServiceProvider _serviceProvider;

    public CreateTimeEntryTests()
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

    private Url GetUrl() =>
        _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendQueryParam("full", "true");

    #endregion Arrange

    #region Act

    private async Task<TimeEntry> CreateTimeEntryAsync(TimeEntryForCreate entry, Func<HttpRequestMessage, bool> verifyRequest)
    {
        //Arrange
        var response = Builder.Builder<TimeEntry>.New().Build(int.MaxValue);
        response.Id = 1;
        response.Activity.Id = entry.Activity;
        response.Project.Id = entry.Project;
        response.Begin = entry.Begin;
        response.End = entry.End;
        response.Description = entry.Description;
        response.Exported = entry.Exported;
        response.User.Id = entry.User;

        _mockHttp.When(GetUrl())
            .With(verifyRequest)
            .Respond("application/json", JsonSerializer.Serialize(response));

        //Act
        var srv = _serviceProvider.GetRequiredService<IKimaiServer>();
        return await srv.CreateTimeEntryAsync(entry);
    }
    
    #endregion Act


    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var entry = Builder.Builder<TimeEntryForCreate>.New().Build();

        //Act
        var response = await CreateTimeEntryAsync(entry, VerifyRequest);

        //Assert
        response.ShouldNotBeNull();
        response.Begin.ShouldBe(entry.Begin);
        response.End.ShouldBe(entry.End);
        response.Description.ShouldBe(entry.Description);
        response.Exported.ShouldBe(entry.Exported);
        response.User.Id.ShouldBe(entry.User);
        response.Activity.Id.ShouldBe(entry.Activity);
        response.Project.Id.ShouldBe(entry.Project);
        return;

        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Post);

            var body = request.ReadRequestBody();

            var payload = JsonSerializer.Deserialize<TimeEntryForCreate>(body);

            payload.ShouldNotBeNull();
            payload.End.ShouldBeNull();

            return true;
        }
    }
}