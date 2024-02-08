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

        private PullRequest[] GetPullRequests()
        {
            var srv = new AzureDevOpsServer(_connectionSettings, _mockHttp.ToHttpClient());
            return srv.GetPullRequestsAsync().Result;
        }

        private PullRequest SinglePullRequest()
        {
            //Arrange
            SetResponse(GetPullRequestsUrl, PullRequestResponses.SinglePullRequest);

            //Act
            var pullRequests = GetPullRequests();

            //Assert
            pullRequests.Length.ShouldBe(1);
            var pr = pullRequests.Single();
            return pr;
        }

        #endregion Act

        #endregion Helpers

        [TestMethod]
        public void _SmokeTest() => SinglePullRequest().pullRequestId.ShouldBe(1);

        [TestMethod]
        public void Title() => SinglePullRequest().title.ShouldBe("My PR Title");

        [TestMethod]
        public void RepoName() => SinglePullRequest().repository.name.ShouldBe("MyRepoName");

        [TestMethod]
        public void Project() => SinglePullRequest().repository.project.name.ShouldBe("MyProject");

        [TestMethod]
        public void IsDraft() => SinglePullRequest().isDraft.ShouldBeFalse();

        [TestMethod]
        public void MergeCommitMessage() => SinglePullRequest().completionOptions.mergeCommitMessage.ShouldBe("Merged PR 1: Test PR");

        [TestMethod]
        public void CreationDate() => SinglePullRequest().creationDate
            .ShouldBe(new DateTime(2023, 10, 11, 6, 32, 15, DateTimeKind.Utc).AddTicks(7700876));

        [TestMethod]
        public void CreatedById() => SinglePullRequest().createdBy.id.ShouldBe("c00ef764-dc77-4b32-9a19-590db59f039b");

        [TestMethod]
        public void CreatedByUniqueName() => SinglePullRequest().createdBy.uniqueName.ShouldBe(@"Domain\Timothyk");

        [TestMethod]
        public void CreatedByDisplayName() => SinglePullRequest().createdBy.displayName.ShouldBe("Timothy Klenke");

        [TestMethod]
        public void CreatedByImageUrl() => SinglePullRequest().createdBy.imageUrl
            .ShouldBe("http://devops.test/Team/_api/_common/identityImage?id=c00ef764-dc77-4b32-9a19-590db59f039b");

        [TestMethod]
        public void Reviewers() => SinglePullRequest().reviewers.Length.ShouldBe(1);

        [TestMethod]
        public void ReviewerId() => SinglePullRequest().reviewers.Single().id.ShouldBe("c00ef764-dc77-4b32-9a19-590db59f039b");

        [TestMethod]
        public void ReviewerIsRequired() => SinglePullRequest().reviewers.Single().isRequired.ShouldBeTrue();
        [TestMethod]
        public void ReviewerVote() => SinglePullRequest().reviewers.Single().vote.ShouldBe(10);
        
        [TestMethod]
        public void ReviewerUniqueName() => SinglePullRequest().reviewers.Single().uniqueName.ShouldBe(@"Domain\Timothyk");

        [TestMethod]
        public void ReviewerDisplayName() => SinglePullRequest().reviewers.Single().displayName.ShouldBe("Timothy Klenke");

        [TestMethod]
        public void ReviewerImageUrl() => SinglePullRequest().reviewers.Single().imageUrl
            .ShouldBe("http://devops.test/Team/_api/_common/identityImage?id=c00ef764-dc77-4b32-9a19-590db59f039b");



    }
}