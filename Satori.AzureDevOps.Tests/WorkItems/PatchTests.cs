using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.WorkItems.SampleFiles;
using Shouldly;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Satori.AzureDevOps.Tests.WorkItems;

[TestClass]
public class PatchTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetUrl(int workItemId) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/wit/workItems")
            .AppendPathSegment(workItemId)
            .AppendQueryParam("$expand", "all")
            .AppendQueryParam("api-version", "6.0");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    #endregion Arrange

    #region Act

    private async Task<WorkItem> PatchWorkItemAsync(int id, IEnumerable<WorkItemPatchItem> patches, Func<HttpRequestMessage, bool> verifyRequest)
    {
        //Arrange
        var url = GetUrl(id);
        var response = GetPayload();
        _mockHttp
            .When(url).With(verifyRequest)
            .Respond("application/json", response);

        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();

        //Act
        return await srv.PatchWorkItemAsync(id, patches);
    }

    private const int SingleWorkItemId = 2;

    private static string GetPayload()
    {
        var s = System.Text.Encoding.Default.GetString(WorkItemResponses.SingleWorkItem);
        var json = JsonDocument.Parse(s);

        var workItem = json.RootElement.GetProperty("value")[0];

        return workItem.ToString();
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
        //Arrange
        var patches = new List<WorkItemPatchItem>()
        {
            new() { Operation = Operation.Test, Path = "/rev", Value = 16 } ,
            new() { Operation = Operation.Add, Path = "/fields/Microsoft.VSTS.Scheduling.CompletedWork", Value = 1.1 },
        };

        //Act
        var result = await PatchWorkItemAsync(SingleWorkItemId, patches, VerifyRequest);

        //Assert
        result.ShouldNotBeNull();

        return;
        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Patch);

            var body = ReadRequestBody(request);

            var payload = JsonSerializer.Deserialize<WorkItemPatchItem[]>(body);

            payload.ShouldNotBeNull();
            payload.Length.ShouldBe(2);

            payload.Single(x => x.Operation == Operation.Add).Path.ShouldBe("/fields/Microsoft.VSTS.Scheduling.CompletedWork");
            var value = (JsonElement)payload.Single(x => x.Operation == Operation.Add).Value;
            value.GetDouble().ShouldBe(1.1);

            return true;
        }

    }

}