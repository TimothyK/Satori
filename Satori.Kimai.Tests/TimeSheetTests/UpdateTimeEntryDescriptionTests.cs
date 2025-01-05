using System.Text.Json;
using Autofac;
using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Shouldly;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class UpdateTimeEntryDescriptionTests
{

    #region Helpers

    #region Arrange
    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private Url GetUrl(int id) =>
        _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id);

    #endregion Arrange

    #region Act

    private async Task UpdateTimeEntryDescriptionAsync(int id, string description, Func<HttpRequestMessage, bool> verifyRequest)
    {
        _mockHttp.When(GetUrl(id))
            .With(verifyRequest)
            .Respond("application/json", string.Empty);

        var srv = Globals.Services.Scope.Resolve<IKimaiServer>();
        await srv.UpdateTimeEntryDescriptionAsync(id, description);
    }

    private static string ReadRequestBody(HttpRequestMessage request)
    {
        request.Content.ShouldNotBeNull();
        using var stream = request.Content.ReadAsStream();
        using var reader = new StreamReader(stream);
        var body = reader.ReadToEnd();

        if (request.Content.Headers.ContentType?.MediaType?.StartsWith("application/json") ?? false)
        {
            Console.WriteLine(PrettyJson(body));
        }
        else
        {
            Console.WriteLine(body); 
        }

        return body;
    }

    private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };
    private static string PrettyJson(string body)
    {
        var doc = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(doc, WriteIndented);
    }

    #endregion Act


    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        await UpdateTimeEntryDescriptionAsync(1, "D#12345 Coding", VerifyRequest);
        return;

        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Patch);

            var body = ReadRequestBody(request);
            body.ShouldContain("D#12345 Coding");
            return true;
        }
    }
}