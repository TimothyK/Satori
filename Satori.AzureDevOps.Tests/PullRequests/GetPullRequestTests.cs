using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Satori.AzureDevOps.Tests.PullRequests.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.PullRequests;

[TestClass]
public class GetPullRequestTests
{
    private readonly ServiceProvider _serviceProvider;

    public GetPullRequestTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
    }

    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings;


    private Url GetPullRequestUrl(int pullRequestId) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/git/pullRequests")
            .AppendPathSegment(pullRequestId)
            .AppendQueryParam("api-version", "6.0");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private async Task<PullRequest> GetPullRequestAsync(int pullRequestId)
    {
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        return await srv.GetPullRequestAsync(pullRequestId);
    }

    private async Task<PullRequest> GetSinglePullRequestAsync(int pullRequestId)
    {
        //Arrange
        SetResponse(GetPullRequestUrl(pullRequestId), GetPullRequestResponses.PullRequest1);

        //Act
        var pullRequest = await GetPullRequestAsync(pullRequestId);

        //Assert
        return pullRequest;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Act
        var pullRequest = await GetSinglePullRequestAsync(1);

        //Assert
        pullRequest.PullRequestId.ShouldBe(1);
    }



}