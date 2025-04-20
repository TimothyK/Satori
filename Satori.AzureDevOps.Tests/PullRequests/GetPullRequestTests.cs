using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.PullRequests.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.PullRequests;

[TestClass]
public class GetPullRequestTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetPullRequestUrl(int pullRequestId) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/git/pullRequests")
            .AppendPathSegment(pullRequestId)
            .AppendQueryParam("api-version", "6.0");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private static async Task<PullRequest> GetPullRequestAsync(int pullRequestId)
    {
        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();
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