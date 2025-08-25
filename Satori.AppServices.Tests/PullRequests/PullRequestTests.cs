using Microsoft.Extensions.DependencyInjection;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Shouldly;
using PullRequest = Satori.AzureDevOps.Models.PullRequest;

namespace Satori.AppServices.Tests.PullRequests;

[TestClass]
public class PullRequestTests
{
    private readonly ServiceProvider _serviceProvider;
    private Uri AzureDevOpsRootUrl => _serviceProvider.GetRequiredService<IAzureDevOpsServer>().ConnectionSettings.Url;

    public PullRequestTests()
    {

        var services = new SatoriServiceCollection();
        services.AddTransient<PullRequestService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    #region Helpers

    #region Arrange

    private PullRequest BuildPullRequest()
    {
        var builder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
        return builder.BuildPullRequest().PullRequest;
    }

    private static Reviewer BuildReviewerWithVote(int vote)
    {
        var reviewer = Builder.Builder<Reviewer>.New().Build(x => x.Vote = vote);
        reviewer.ImageUrl = $"http://devops.test/Org/_api/_common/identityImage?id={reviewer.Id}";
        return reviewer;
    }

    #endregion Arrange

    #region Act

    private async Task<ViewModels.PullRequests.PullRequest[]> GetPullRequestsAsync(WithChildren children = WithChildren.None)
    {
        //Act
        var srv = _serviceProvider.GetRequiredService<PullRequestService>();
        var pullRequests = (await srv.GetPullRequestsAsync()).ToArray();
        if (children.HasFlag(WithChildren.WorkItems))
        {
            await srv.AddWorkItemsToPullRequestsAsync(pullRequests);
        }
        return pullRequests;
    }

    private async Task<ViewModels.PullRequests.PullRequest> GetSinglePullRequestsAsync(WithChildren children = WithChildren.None)
    {
        //Arrange

        //Act
        var pullRequests = await GetPullRequestsAsync(children);

        //Assert
        pullRequests.Length.ShouldBe(1);
        return pullRequests.Single();
    }

    [Flags]
    private enum WithChildren : byte
    {
        None = 0,
        WorkItems = 1,
    }

    #endregion Act

    #region Assert

    [TestCleanup]
    public void TearDown()
    {
        var alertService = _serviceProvider.GetRequiredService<TestAlertService>();
        alertService.VerifyNoMessagesWereBroadcast();
    }

    #endregion Assert

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var pullRequests = await GetPullRequestsAsync();

        //Assert
        pullRequests.Length.ShouldBe(1);
        var actual = pullRequests.Single();
        actual.Id.ShouldBe(pr.PullRequestId);
    }

    #region Properties

    [TestMethod]
    public async Task Title()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.Title.ShouldBe(pr.Title);
    }

    [TestMethod]
    public async Task RepoName()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.RepositoryName.ShouldBe(pr.Repository.Name);
    }
        
    [TestMethod]
    public async Task ProjectName()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.Project.ShouldBe(pr.Repository.Project.Name);
    }
        
    [TestMethod]
    public async Task Status_Draft()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.IsDraft = true;

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.Status.ShouldBe(Status.Draft);
    }
        
    [TestMethod]
    public async Task Status_Open()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.IsDraft = false;

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.Status.ShouldBe(Status.Open);
    }
        
    [TestMethod]
    public async Task AutoComplete_Off()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.CompletionOptions = null;

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.AutoComplete.ShouldBeFalse();
    }
        
    [TestMethod]
    public async Task AutoComplete_On()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.CompletionOptions.ShouldNotBeNull();
        pr.CompletionOptions.MergeCommitMessage = "Feature X - now with awesomeness";

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.AutoComplete.ShouldBeTrue();
    }
        
    [TestMethod]
    public async Task CreationDate()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.CreationDate.ShouldBe(pr.CreationDate);
    }
        
    [TestMethod]
    public async Task CreatedBy()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = pr.CreatedBy;

        //Act
        var actual = (await GetSinglePullRequestsAsync())
            .CreatedBy;

        //Assert
        actual.AzureDevOpsId.ShouldBe(expected.Id);
        actual.DisplayName.ShouldBe(expected.DisplayName);
        actual.AvatarUrl.ToString().ShouldBe(expected.ImageUrl);
    }
        
    [TestMethod]
    public async Task Reviewer()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = BuildReviewerWithVote(0);
        pr.Reviewers = [expected];

        //Act
        var actual = (await GetSinglePullRequestsAsync())
            .Reviews.Single();

        //Assert
        actual.Reviewer.AzureDevOpsId.ShouldBe(expected.Id);
        actual.Reviewer.DisplayName.ShouldBe(expected.DisplayName);
        actual.Reviewer.AvatarUrl.ToString().ShouldBe(expected.ImageUrl);
        actual.IsRequired.ShouldBe(expected.IsRequired);
        actual.Vote.ShouldBe(ReviewVote.NoVote);
    }

    [TestMethod]
    [DataRow(ReviewVote.Approved)]
    [DataRow(ReviewVote.ApprovedWithSuggestions)]
    [DataRow(ReviewVote.NoVote)]
    [DataRow(ReviewVote.WaitingForAuthor)]
    [DataRow(ReviewVote.Rejected)]
    public async Task ReviewerVote(ReviewVote expected)
    {
        //Arrange
        var pr = BuildPullRequest();
        var reviewer = BuildReviewerWithVote((int)expected);
        pr.Reviewers = [reviewer];

        //Act
        var actual = (await GetSinglePullRequestsAsync())
            .Reviews.Single();

        //Assert
        actual.Vote.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Labels_None()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.Labels = null;

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.Labels.ShouldBeEmpty();
    }
        
    [TestMethod]
    public async Task Labels_One()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = Builder.Builder<Label>.New().Build(x => x.Active = true);
        pr.Labels = [expected];

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.Labels.Count.ShouldBe(1);
        actual.Labels.Single().ShouldBe(expected.Name);
    }
        
    [TestMethod]
    public async Task Labels_Deactivated()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = Builder.Builder<Label>.New().Build(x => x.Active = false);
        pr.Labels = [expected];

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.Labels.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Url()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.Url.ShouldBe($"{AzureDevOpsRootUrl}/{pr.Repository.Project.Name}/_git/{pr.Repository.Name}/pullRequest/{pr.PullRequestId}");
    }
    #endregion

    #region Work Items

    [TestMethod]
    public async Task WorkItems_Empty()
    {
        //Arrange
        BuildPullRequest();

        //Act
        var actual = await GetSinglePullRequestsAsync();

        //Assert
        actual.WorkItems.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task WorkItems_SmokeTest()
    {
        //Arrange
        var builder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
        builder.BuildPullRequest()
            .WithWorkItem(out var expected);

        //Act
        var pullRequest = await GetSinglePullRequestsAsync(WithChildren.WorkItems);

        //Assert
        pullRequest.WorkItems.Count.ShouldBe(1);
        var actual = pullRequest.WorkItems.Single();
        actual.Id.ShouldBe(expected.Id);
    }

    [TestMethod]
    public async Task MultiPullRequests_MultiWorkItems()
    {
        //Arrange
        var builder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
        builder.BuildPullRequest(out var pr1).WithWorkItem(out var workItem1);
        builder.BuildPullRequest(out var pr2);
        builder.BuildPullRequest(out var pr3).WithWorkItem(workItem1).WithWorkItem(out var workItem2);
        
        //Act
        var prs = await GetPullRequestsAsync(WithChildren.WorkItems);

        //Assert
        prs.Length.ShouldBe(3);
        //pr1
        prs.Single(pr => pr.Id == pr1.PullRequestId).WorkItems.Count.ShouldBe(1);
        prs.Single(pr => pr.Id == pr1.PullRequestId).WorkItems.Select(wi => wi.Id).ShouldContain(workItem1.Id);
        //pr2
        prs.Single(pr => pr.Id == pr2.PullRequestId).WorkItems.ShouldBeEmpty();
        //pr3
        prs.Single(pr => pr.Id == pr3.PullRequestId).WorkItems.Count.ShouldBe(2);
        prs.Single(pr => pr.Id == pr3.PullRequestId)
            .WorkItems.Select(wi => wi.Id)
            .ShouldBe([workItem1.Id, workItem2.Id], ignoreOrder: true);
    }

    #endregion Work Items

    #region Error Handling

    [TestMethod]
    public async Task ConnectionError_ReturnsEmpty()
    {
        //Arrange
        var azureDevOpsServer = _serviceProvider.GetRequiredService<TestAzureDevOpsServer>();
        var alertService = _serviceProvider.GetRequiredService<TestAlertService>();
        azureDevOpsServer.Mock
            .Setup(srv => srv.GetPullRequestsAsync())
            .Throws<ApplicationException>();
        alertService.DisableVerifications();

        //Act
        var pullRequests = await GetPullRequestsAsync();

        //Assert
        pullRequests.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task ConnectionError_BroadcastsError()
    {
        //Arrange
        var azureDevOpsServer = _serviceProvider.GetRequiredService<TestAzureDevOpsServer>();
        var alertService = _serviceProvider.GetRequiredService<TestAlertService>();
        azureDevOpsServer.Mock
            .Setup(srv => srv.GetPullRequestsAsync())
            .Throws<ApplicationException>();

        //Act
        await GetPullRequestsAsync();

        //Assert
        alertService.LastException.ShouldNotBeNull();
        alertService.LastException.ShouldBeOfType<ApplicationException>();
        alertService.DisableVerifications();
    }

    #endregion Error Handling
}