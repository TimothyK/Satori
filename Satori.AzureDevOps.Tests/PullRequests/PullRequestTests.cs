using Flurl;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.PullRequests.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.PullRequests;

[TestClass]
public class PullRequestTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = new()
    {
        Url = new Uri( "http://devops.test/Org" ), 
        PersonalAccessToken = "test"
    };

    private Url GetPullRequestsUrl =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/git/pullRequests")
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
        var srv = new AzureDevOpsServer(_connectionSettings, _mockHttp.ToHttpClient(), NullLoggerFactory.Instance);
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
    public void ASmokeTest() => SinglePullRequest().PullRequestId.ShouldBe(1);

    [TestMethod]
    public void Title() => SinglePullRequest().Title.ShouldBe("My PR Title");

    [TestMethod]
    public void RepoName() => SinglePullRequest().Repository.Name.ShouldBe("MyRepoName");

    [TestMethod]
    public void Project() => SinglePullRequest().Repository.Project.Name.ShouldBe("MyProject");

    [TestMethod]
    public void IsDraft() => SinglePullRequest().IsDraft.ShouldBeFalse();

    [TestMethod]
    public void MergeCommitMessage()
    {
        var completionOptions = SinglePullRequest().CompletionOptions;
        completionOptions.ShouldNotBeNull();
        completionOptions.MergeCommitMessage.ShouldBe("Merged PR 1: Test PR");
    }

    [TestMethod]
    public void CreationDate() => SinglePullRequest().CreationDate
        .ShouldBe(new DateTimeOffset(2023, 10, 11, 6, 32, 15, TimeSpan.Zero).AddTicks(7700876));

    [TestMethod]
    public void CreatedById() => SinglePullRequest().CreatedBy.Id.ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));

    [TestMethod]
    public void CreatedByUniqueName() => SinglePullRequest().CreatedBy.UniqueName.ShouldBe(@"Domain\Timothyk");

    [TestMethod]
    public void CreatedByDisplayName() => SinglePullRequest().CreatedBy.DisplayName.ShouldBe("Timothy Klenke");

    [TestMethod]
    public void CreatedByImageUrl() => SinglePullRequest().CreatedBy.ImageUrl
        .ShouldBe("http://devops.test/Org/_api/_common/identityImage?id=c00ef764-dc77-4b32-9a19-590db59f039b");

    [TestMethod]
    public void Reviewers() => SinglePullRequest().Reviewers.Length.ShouldBe(1);

    [TestMethod]
    public void ReviewerId() => SinglePullRequest().Reviewers.Single().Id.ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));

    [TestMethod]
    public void ReviewerIsRequired() => SinglePullRequest().Reviewers.Single().IsRequired.ShouldBeTrue();
    [TestMethod]
    public void ReviewerVote() => SinglePullRequest().Reviewers.Single().Vote.ShouldBe(10);
        
    [TestMethod]
    public void ReviewerUniqueName() => SinglePullRequest().Reviewers.Single().UniqueName.ShouldBe(@"Domain\Timothyk");

    [TestMethod]
    public void ReviewerDisplayName() => SinglePullRequest().Reviewers.Single().DisplayName.ShouldBe("Timothy Klenke");

    [TestMethod]
    public void ReviewerImageUrl() => SinglePullRequest().Reviewers.Single().ImageUrl
        .ShouldBe("http://devops.test/Org/_api/_common/identityImage?id=c00ef764-dc77-4b32-9a19-590db59f039b");

    [TestMethod]
    public void Label()
    {
        var labels = SinglePullRequest().Labels;
        labels.ShouldNotBeNull();
        labels.Length.ShouldBe(1);
        labels.Single().Name.ShouldBe("NoBuild");
        labels.Single().Active.ShouldBeTrue();
    }
}