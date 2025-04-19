using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Extensions;
using Satori.AzureDevOps.Tests.PullRequests.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.PullRequests;

[TestClass]
public class GetTagsOfMergeTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetUrl(PullRequest pullRequest) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/Contribution/HierarchyQuery/project")
            .AppendPathSegment(pullRequest.Repository.Project.Name)
            .AppendQueryParam("api-version", "5.0-preview.1");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private static PullRequest BuildPullRequest()
    {
        var pr = Builder.Builder<PullRequest>.New().Build(int.MaxValue);
        pr.Status = "completed";
        pr.LastMergeCommit = new Commit
        {
            CommitId = "1234abcd5678ef9012abcd3456ef7890abcd1234",
            Url = "https://azureDevOps.test/Org/"
        };
        return pr;
    }

    #endregion Arrange

    #region Act

    private async Task<Tag[]> GetTagsOfMergeAsync(PullRequest pullRequest, Func<HttpRequestMessage, bool>? verifyRequest = null)
    {
        //Arrange
        verifyRequest ??= _ => true;
        var url = GetUrl(pullRequest);
        var payload = System.Text.Encoding.Default.GetString(GetTagsOrMergeResponses.GetTagsOfMerge1234);
        _mockHttp.Clear();
        _mockHttp
            .When(url).With(verifyRequest)
            .Respond("application/json", payload);

        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();

        //Act
        return await srv.GetTagsOfMergeAsync(pullRequest);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var tags = await GetTagsOfMergeAsync(pr);

        //Assert
        tags.ShouldNotBeEmpty();
        tags.Length.ShouldBe(1);
        tags.Single().Name.ShouldBe("1.2.3");
    }

    #region Payload tests

    [TestMethod]
    public async Task RequestIsPost()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        await GetTagsOfMergeAsync(pr, VerifyRequest);

        //Assert
        return;
        static bool VerifyRequest(HttpRequestMessage request)
        {
            request.Method.ShouldBe(HttpMethod.Post);
            return true;
        }
    }
    
    [TestMethod]
    public async Task PayloadIsContributionHierarchyQuery()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        await GetTagsOfMergeAsync(pr, VerifyRequest);

        //Assert
        return;
        static bool VerifyRequest(HttpRequestMessage request)
        {
            var payload = request.ReadRequestBody<ContributionHierarchyQuery>();
            payload.ShouldNotBeNull();

            return true;
        }
    }
    
    [TestMethod]
    public async Task Payload_ContribId_CommitsDataProvider()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        await GetTagsOfMergeAsync(pr, VerifyRequest);

        //Assert
        return;
        static bool VerifyRequest(HttpRequestMessage request)
        {
            var payload = request.ReadRequestBody<ContributionHierarchyQuery>();

            payload.ContributionIds.ShouldNotBeEmpty();
            payload.ContributionIds.Length.ShouldBe(1);
            payload.ContributionIds[0].ShouldBe(DataProviderIds.CommitsDataProvider);

            return true;
        }
    }
    
    [TestMethod]
    public async Task Payload_RepoId()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        await GetTagsOfMergeAsync(pr, VerifyRequest);

        //Assert
        return;
        bool VerifyRequest(HttpRequestMessage request)
        {
            var payload = request.ReadRequestBody<ContributionHierarchyQuery>();
            payload.DataProviderContext.Properties.RepositoryId.ShouldBe(pr.Repository.Id);

            return true;
        }
    }
    
    [TestMethod]
    public async Task Payload_LastMergeCommit()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        await GetTagsOfMergeAsync(pr, VerifyRequest);

        //Assert
        return;
        bool VerifyRequest(HttpRequestMessage request)
        {
            pr.LastMergeCommit.ShouldNotBeNull();
            var payload = request.ReadRequestBody<ContributionHierarchyQuery>();
            payload.DataProviderContext.Properties.SearchCriteria.GitArtifactsQueryArguments.CommitIds.Single()
                .ShouldBe(pr.LastMergeCommit.CommitId);

            return true;
        }
    }
    
    [TestMethod]
    public async Task Payload_FetchTags()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        await GetTagsOfMergeAsync(pr, VerifyRequest);

        //Assert
        return;
        bool VerifyRequest(HttpRequestMessage request)
        {
            pr.LastMergeCommit.ShouldNotBeNull();
            var payload = request.ReadRequestBody<ContributionHierarchyQuery>();
            payload.DataProviderContext.Properties.SearchCriteria.GitArtifactsQueryArguments.FetchTags.ShouldBeTrue();

            return true;
        }
    }

    #endregion Payload tests

    #region Error checking
    
    [TestMethod]
    public async Task PullRequest_MustBeCompleted()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.Status = "active";

        //Act
        await Should.ThrowAsync<InvalidOperationException>(() => GetTagsOfMergeAsync(pr));
    }
    
    [TestMethod]
    public async Task PullRequest_MustHaveLastMergeCommit()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.LastMergeCommit = null;

        //Act
        await Should.ThrowAsync<InvalidOperationException>(() => GetTagsOfMergeAsync(pr));
    }


    #endregion Error checking
}