using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.Kimai;
using Shouldly;
using PullRequest = Satori.AzureDevOps.Models.PullRequest;

namespace Satori.AppServices.Tests.PullRequests;

[TestClass]
public class PullRequestTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TestAzureDevOpsServer _azureDevOpsServer;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private readonly TestAlertService _alertService = new();
    private Uri AzureDevOpsRootUrl => _serviceProvider.GetRequiredService<IAzureDevOpsServer>().ConnectionSettings.Url;

    public PullRequestTests()
    {
        _azureDevOpsServer = new TestAzureDevOpsServer();
        _builder = _azureDevOpsServer.CreateBuilder();
        var kimai = new TestKimaiServer();

        var services = new ServiceCollection();
        services.AddSingleton(_azureDevOpsServer.AsInterface());
        services.AddSingleton(kimai.AsInterface());
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IAlertService>(_alertService);
        services.AddTransient<PullRequestService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    #region Helpers

    #region Arrange

    private PullRequest BuildPullRequest()
    {
        return _builder.BuildPullRequest().PullRequest;
    }

    private static Reviewer BuildReviewerWithVote(int vote)
    {
        var reviewer = Builder.Builder<Reviewer>.New().Build(x => x.Vote = vote);
        reviewer.ImageUrl = $"http://devops.test/Org/_api/_common/identityImage?id={reviewer.Id}";
        return reviewer;
    }

    #endregion Arrange

    #region Act

    private ViewModels.PullRequests.PullRequest[] GetPullRequests(WithChildren children = WithChildren.None)
    {
        //Act
        var srv = _serviceProvider.GetRequiredService<PullRequestService>();
        var pullRequests = srv.GetPullRequestsAsync().Result.ToArray();
        if (children.HasFlag(WithChildren.WorkItems))
        {
            srv.AddWorkItemsToPullRequestsAsync(pullRequests).GetAwaiter().GetResult();
        }
        return pullRequests;
    }

    private ViewModels.PullRequests.PullRequest GetSinglePullRequests(WithChildren children = WithChildren.None)
    {
        //Arrange

        //Act
        var pullRequests = GetPullRequests(children);

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
        _alertService.VerifyNoMessagesWereBroadcast();
    }

    #endregion Assert

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest()
    {
        //Arrange
        var pr = _builder.BuildPullRequest().PullRequest;

        //Act
        var pullRequests = GetPullRequests();

        //Assert
        pullRequests.Length.ShouldBe(1);
        var actual = pullRequests.Single();
        actual.Id.ShouldBe(pr.PullRequestId);
    }

    #region Properties

    [TestMethod]
    public void Title()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.Title.ShouldBe(pr.Title);
    }

    [TestMethod]
    public void RepoName()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.RepositoryName.ShouldBe(pr.Repository.Name);
    }
        
    [TestMethod]
    public void ProjectName()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.Project.ShouldBe(pr.Repository.Project.Name);
    }
        
    [TestMethod]
    public void Status_Draft()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.IsDraft = true;

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.Status.ShouldBe(Status.Draft);
    }
        
    [TestMethod]
    public void Status_Open()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.IsDraft = false;

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.Status.ShouldBe(Status.Open);
    }
        
    [TestMethod]
    public void AutoComplete_Off()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.CompletionOptions = null;

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.AutoComplete.ShouldBeFalse();
    }
        
    [TestMethod]
    public void AutoComplete_On()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.CompletionOptions.ShouldNotBeNull();
        pr.CompletionOptions.MergeCommitMessage = "Feature X - now with awesomeness";

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.AutoComplete.ShouldBeTrue();
    }
        
    [TestMethod]
    public void CreationDate()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.CreationDate.ShouldBe(pr.CreationDate);
    }
        
    [TestMethod]
    public void CreatedBy()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = pr.CreatedBy;

        //Act
        var actual = GetSinglePullRequests()
            .CreatedBy;

        //Assert
        actual.AzureDevOpsId.ShouldBe(expected.Id);
        actual.DisplayName.ShouldBe(expected.DisplayName);
        actual.AvatarUrl.ToString().ShouldBe(expected.ImageUrl);
    }
        
    [TestMethod]
    public void Reviewer()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = BuildReviewerWithVote(0);
        pr.Reviewers = [expected];

        //Act
        var actual = GetSinglePullRequests()
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
    public void ReviewerVote(ReviewVote expected)
    {
        //Arrange
        var pr = BuildPullRequest();
        var reviewer = BuildReviewerWithVote((int)expected);
        pr.Reviewers = [reviewer];

        //Act
        var actual = GetSinglePullRequests()
            .Reviews.Single();

        //Assert
        actual.Vote.ShouldBe(expected);
    }

    [TestMethod]
    public void Labels_None()
    {
        //Arrange
        var pr = BuildPullRequest();
        pr.Labels = null;

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.Labels.ShouldBeEmpty();
    }
        
    [TestMethod]
    public void Labels_One()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = Builder.Builder<Label>.New().Build(x => x.Active = true);
        pr.Labels = [expected];

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.Labels.Count.ShouldBe(1);
        actual.Labels.Single().ShouldBe(expected.Name);
    }
        
    [TestMethod]
    public void Labels_Deactivated()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = Builder.Builder<Label>.New().Build(x => x.Active = false);
        pr.Labels = [expected];

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.Labels.ShouldBeEmpty();
    }

    [TestMethod]
    public void Url()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.Url.ShouldBe($"{AzureDevOpsRootUrl}/{pr.Repository.Project.Name}/_git/{pr.Repository.Name}/pullRequest/{pr.PullRequestId}");
    }
    #endregion

    #region Work Items

    [TestMethod]
    public void WorkItems_Empty()
    {
        //Arrange
        BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests();

        //Assert
        actual.WorkItems.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void WorkItems_SmokeTest()
    {
        //Arrange
        _builder.BuildPullRequest()
            .WithWorkItem(out var expected);

        //Act
        var pullRequest = GetSinglePullRequests(WithChildren.WorkItems);

        //Assert
        pullRequest.WorkItems.Count.ShouldBe(1);
        var actual = pullRequest.WorkItems.Single();
        actual.Id.ShouldBe(expected.Id);
    }

    [TestMethod]
    public void MultiPullRequests_MultiWorkItems()
    {
        //Arrange
        _builder.BuildPullRequest(out var pr1).WithWorkItem(out var workItem1);
        _builder.BuildPullRequest(out var pr2);
        _builder.BuildPullRequest(out var pr3).WithWorkItem(workItem1).WithWorkItem(out var workItem2);
        
        //Act
        var prs = GetPullRequests(WithChildren.WorkItems);

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
    public void ConnectionError_ReturnsEmpty()
    {
        //Arrange
        _azureDevOpsServer.Mock
            .Setup(srv => srv.GetPullRequestsAsync())
            .Throws<ApplicationException>();
        _alertService.DisableVerifications();

        //Act
        var pullRequests = GetPullRequests();

        //Assert
        pullRequests.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void ConnectionError_BroadcastsError()
    {
        //Arrange
        _azureDevOpsServer.Mock
            .Setup(srv => srv.GetPullRequestsAsync())
            .Throws<ApplicationException>();

        //Act
        GetPullRequests();

        //Assert
        _alertService.LastException.ShouldNotBeNull();
        _alertService.LastException.ShouldBeOfType<ApplicationException>();
        _alertService.DisableVerifications();
    }

    #endregion Error Handling
}