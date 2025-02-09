using Autofac;
using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Satori.Kimai.Models;
using Shouldly;
using System.Text.Json;
using Satori.Kimai.Tests.Extensions;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class CreateTimeEntryTests
{
    public CreateTimeEntryTests()
    {
        _mockHttp.Clear();
    }

    #region Helpers

    #region Arrange
    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

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
        var srv = Globals.Services.Scope.Resolve<IKimaiServer>();
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