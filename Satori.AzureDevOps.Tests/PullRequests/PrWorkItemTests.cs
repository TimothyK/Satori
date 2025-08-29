using Builder;
using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Satori.AzureDevOps.Tests.PullRequests.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.PullRequests;

[TestClass]
public class PrWorkItemTests
{
    private readonly ServiceProvider _serviceProvider;

    public PrWorkItemTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();

    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;

    private readonly PullRequest _samplePullRequest = Builder<PullRequest>.New().Build(int.MaxValue);


    private Url GetPullRequestWorkItemsUrl(PullRequest pr) =>
        _connectionSettings.Url
            .AppendPathSegment(pr.Repository.Project.Name)
            .AppendPathSegment("_apis/git/repositories")
            .AppendPathSegment(pr.Repository.Name)
            .AppendPathSegment("pullRequests")
            .AppendPathSegment(pr.PullRequestId)
            .AppendPathSegment("workItems")
            .AppendQueryParam("api-version", "6.0");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, byte[] response)
    {
        Console.WriteLine("Mocking " + url);
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private IdMap[] GetPrWorkItems()
    {
        //Arrange
        var pr = _samplePullRequest;
        var url = GetPullRequestWorkItemsUrl(pr);
        SetResponse(url, PrWorkItemResponses.PrWorkItem);

        //Act
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        return srv.GetPullRequestWorkItemIdsAsync(_samplePullRequest).Result;
    }

    #endregion Act

    #endregion Helpers


    [TestMethod]
    public void ASmokeTest()
    {
        //Act
        var workItemMap = GetPrWorkItems();

        //Assert
        workItemMap.Length.ShouldBe(1);
        workItemMap.Single().Id.ShouldBe(123);
    }
}