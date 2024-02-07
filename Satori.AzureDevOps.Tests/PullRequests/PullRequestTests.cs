using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.PullRequests.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.PullRequests
{
    [TestClass]
    public class PullRequestTests
    {
        #region Helpers

        #region Arrange

        private readonly ConnectionSettings _connectionSettings = new()
        {
            Url = new Uri( "http://devops.test/Team" ), 
            PersonalAccessToken = "test"
        };

        private Url GetPullRequestsUrl =>
            _connectionSettings.Url
                .AppendPathSegment("_apis/git/pullrequests")
                .AppendQueryParam("api-version", "6.0");

        private readonly MockHttpMessageHandler _mockHttp = new();

        private void SetResponse(Url url, byte[] response)
        {
            _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
        }

        #endregion Arrange

        #region Act

        private Value[] GetPullRequests()
        {
            var srv = new AzureDevOpsServer(_connectionSettings, _mockHttp.ToHttpClient());
            return srv.GetPullRequestsAsync().Result;
        }

        #endregion Act

        #endregion Helpers

        [TestMethod]
        public void SmokeTest()
        {
            //Arrange
            SetResponse(GetPullRequestsUrl, PullRequestResponses.SinglePullRequest);

            //Act
            var pullRequests = GetPullRequests();

            //Assert
            pullRequests.Length.ShouldBe(1);
            var pr = pullRequests.Single();
            pr.pullRequestId.ShouldBe(710);
        }
    }
}