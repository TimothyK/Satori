using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.WorkItems.SampleFiles;
using Shouldly;
using System.Text.Json;
using Satori.AzureDevOps.Tests.Extensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Satori.AzureDevOps.Tests.WorkItems;

[TestClass]
public class PostTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetUrl(string projectName) =>
        _connectionSettings.Url
            .AppendPathSegment(projectName)
            .AppendPathSegment("_apis/wit/workItems")
            .AppendPathSegment("$Task")
            .AppendQueryParam("$expand", "all")
            .AppendQueryParam("api-version", "6.0");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private const string ProjectName = "Skunk Works";

    #endregion Arrange

    #region Act

    private async Task<WorkItem> PostWorkItemAsync(string projectName, IEnumerable<WorkItemPatchItem> fields, Func<HttpRequestMessage, bool> verifyRequest)
    {
        //Arrange
        var url = GetUrl(projectName);
        var response = GetPayload();
        _mockHttp.Clear();
        _mockHttp
            .When(url).With(verifyRequest)
            .Respond("application/json", response);

        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();

        //Act
        return await srv.PostWorkItemAsync(projectName, fields);
    }

    private static string GetPayload()
    {
        var s = WorkItemResponses.SingleWorkItem;
        var json = JsonDocument.Parse(s);

        var workItem = json.RootElement.GetProperty("value")[0];

        return workItem.ToString();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var fields = new List<WorkItemPatchItem>()
        {
            new() { Operation = Operation.Add, Path = "/fields/System.Title", Value = "New Task Title" },
            new() { Operation = Operation.Add, Path = "/fields/System.AreaPath", Value = @"Product\AppArea" },
            new() { Operation = Operation.Add, Path = "/fields/System.IterationPath", Value = @"CD\Skunk\Sprint 2024-02" },
            new() { Operation = Operation.Add, Path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate", Value = 4.0 },
        };

        //Act
        var result = await PostWorkItemAsync(ProjectName, fields, VerifyRequest);

        //Assert
        result.ShouldNotBeNull();

        return;
        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Post);

            var payload = request.ReadRequestBody<WorkItemPatchItem[]>();

            var title = (JsonElement) payload.Single(x => x is { Operation: Operation.Add, Path: "/fields/System.Title" }).Value;
            title.GetString().ShouldBe("New Task Title");
            var estimate = (JsonElement) payload.Single(x => x is { Operation: Operation.Add, Path: "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate" }).Value;
            estimate.GetDouble().ShouldBe(4.0);

            return true;
        }

    }
    
    [TestMethod]
    public async Task AddParentLink()
    {
        //Arrange
        var relation = new Dictionary<string, object>()
        {
            { "rel", "System.LinkTypes.Hierarchy-Reverse" },
            { "url", "https://devops.test/Org/_apis/wit/workItems/1"},
        };
        var fields = new List<WorkItemPatchItem>()
        {
            new() { Operation = Operation.Add, Path = "/fields/System.Title", Value = "New Task Title" },
            new() { Operation = Operation.Add, Path = "/relations/-", Value = relation },
        };

        //Act
        var result = await PostWorkItemAsync(ProjectName, fields, VerifyRequest);

        //Assert
        result.ShouldNotBeNull();

        return;
        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Post);

            var payload = request.ReadRequestBody<WorkItemPatchItem[]>();

            var relations = (JsonElement) payload.Single(x => x is { Operation: Operation.Add, Path: "/relations/-" }).Value;
            var rel = relations.EnumerateObject().SingleOrDefault(kvp => kvp.Name == "rel");
            rel.Value.GetString().ShouldBe("System.LinkTypes.Hierarchy-Reverse");
            var url = relations.EnumerateObject().SingleOrDefault(kvp => kvp.Name == "url");
            url.Value.GetString().ShouldBe("https://devops.test/Org/_apis/wit/workItems/1");

            return true;
        }

    }

}