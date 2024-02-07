using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Tests.PullRequests.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.PullRequests
{
    [TestClass]
    public class PullRequestTests
    {
        private readonly ConnectionSettings _connectionSettings = new()
        {
            Url = new Uri( "http://devops.test/Team" ), 
            PersonalAccessToken = "test"
        };

        [TestMethod]
        public void SmokeTest()
        {
            //Arrange
            var url = _connectionSettings.Url
                .AppendPathSegment("_apis/git/pullrequests")
                .AppendQueryParam("api-version", "6.0");

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(url)
                .Respond("application/json", System.Text.Encoding.Default.GetString(PullRequestResponses.SinglePullRequest));

            var srv = new AzureDevOpsServer(_connectionSettings, mockHttp.ToHttpClient());
            
            //Act
            var pullRequests = srv.GetPullRequestsAsync().Result;

            //Assert
            pullRequests.Length.ShouldBe(1);
            var pr = pullRequests.Single();
            pr.pullRequestId.ShouldBe(710);
        }
    }
}