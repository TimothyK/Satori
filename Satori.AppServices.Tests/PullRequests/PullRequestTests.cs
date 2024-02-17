using Moq;
using Pscl.Linq;
using Satori.AppServices.Services;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Shouldly;
using PullRequest = Satori.AzureDevOps.Models.PullRequest;

namespace Satori.AppServices.Tests.PullRequests;

[TestClass]
public class PullRequestTests
{
    #region Helpers

    #region Arrange

    private readonly List<PullRequest> _pullRequests = new();
    private readonly List<(int PullRequestId, WorkItem WorkItem)> _pullRequestWorkItems = new();

    private static PullRequest BuildPullRequest()
    {
        var pr = Builder.Builder<PullRequest>.New().Build(int.MaxValue);
        pr.Reviewers = Array.Empty<Reviewer>();
        return pr;
    }

    private static WorkItem BuildWorkItem()
    {
        var expected = Builder.Builder<WorkItem>.New().Build(int.MaxValue);
        return expected;
    }

    private void AddPullRequest(PullRequest pullRequest)
    {
        _pullRequests.Add(pullRequest);
    }

    private void LinkWorkItem(PullRequest pullRequest, WorkItem workItem)
    {
        _pullRequestWorkItems.Add((pullRequest.PullRequestId, workItem));
    }

    #endregion Arrange

    #region Act

    private Satori.AppServices.ViewModels.PullRequests.PullRequest[] GetPullRequests()
    {
        //Arrange
        var mock = new Mock<IAzureDevOpsServer>();
        mock.Setup(srv => srv.ConnectionSettings)
            .Returns(new ConnectionSettings() { Url = new Uri("http://azureDevops.test/Team"), PersonalAccessToken = "token" });
        mock.Setup(srv => srv.GetPullRequestsAsync())
            .ReturnsAsync(_pullRequests.ToArray());
        foreach (var pullRequest in _pullRequests)
        {
            var idMaps = _pullRequestWorkItems
                .Where(map => map.PullRequestId == pullRequest.PullRequestId)
                .Select(map => Builder.Builder<IdMap>.New().Build(idMap => idMap.Id = map.WorkItem.Id));
            mock.Setup(srv => srv.GetPullRequestWorkItemIdsAsync(pullRequest))
                .ReturnsAsync(idMaps.ToArray());
        }
        var workItems = _pullRequestWorkItems
            .Select(map => map.WorkItem)
            .Distinct()
            .ToArray();
        mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((IEnumerable<int> workItemIds) => workItems.Where(wi => wi.Id.IsIn(workItemIds)).ToArray());

        //Act
        var srv = new PullRequestService(mock.Object);
        return srv.GetPullRequestsAsync().Result.ToArray();
    }

    private Satori.AppServices.ViewModels.PullRequests.PullRequest GetSinglePullRequests(PullRequest pr)
    {
        //Arrange
        AddPullRequest(pr);

        //Act
        var pullRequests = GetPullRequests();

        //Assert
        pullRequests.Length.ShouldBe(1);
        return pullRequests.Single();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void SmokeTest()
    {
        //Arrange
        var pr = BuildPullRequest();
        AddPullRequest(pr);

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
        var actual = GetSinglePullRequests(pr);

        //Assert
        actual.Title.ShouldBe(pr.Title);
    }
        
    [TestMethod]
    public void RepoName()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests(pr);

        //Assert
        actual.RepositoryName.ShouldBe(pr.Repository.Name);
    }
        
    [TestMethod]
    public void ProjectName()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests(pr);

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
        var actual = GetSinglePullRequests(pr);

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
        var actual = GetSinglePullRequests(pr);

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
        var actual = GetSinglePullRequests(pr);

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
        var actual = GetSinglePullRequests(pr);

        //Assert
        actual.AutoComplete.ShouldBeTrue();
    }
        
    [TestMethod]
    public void CreationDate()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests(pr);

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
        var actual = GetSinglePullRequests(pr)
            .CreatedBy;

        //Assert
        actual.Id.ShouldBe(expected.Id);
        actual.DisplayName.ShouldBe(expected.DisplayName);
        actual.AvatarUrl.ShouldBe(expected.ImageUrl);
    }
        
    [TestMethod]
    public void Reviewer()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = Builder.Builder<Reviewer>.New().Build(x => x.Vote = 0);
        pr.Reviewers = new[] { expected };

        //Act
        var actual = GetSinglePullRequests(pr)
            .Reviews.Single();

        //Assert
        actual.Reviewer.Id.ShouldBe(expected.Id);
        actual.Reviewer.DisplayName.ShouldBe(expected.DisplayName);
        actual.Reviewer.AvatarUrl.ShouldBe(expected.ImageUrl);
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
        var reviewer = Builder.Builder<Reviewer>.New().Build(x => x.Vote = (int)expected);
        pr.Reviewers = new[] { reviewer };

        //Act
        var actual = GetSinglePullRequests(pr)
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
        var actual = GetSinglePullRequests(pr);

        //Assert
        actual.Labels.ShouldBeEmpty();
    }
        
    [TestMethod]
    public void Labels_One()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = Builder.Builder<Label>.New().Build(x => x.Active = true);
        pr.Labels = new[] { expected };

        //Act
        var actual = GetSinglePullRequests(pr);

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
        pr.Labels = new[] { expected };

        //Act
        var actual = GetSinglePullRequests(pr);

        //Assert
        actual.Labels.ShouldBeEmpty();
    }

    #endregion

    #region Work Items

    [TestMethod]
    public void WorkItems_Empty()
    {
        //Arrange
        var pr = BuildPullRequest();

        //Act
        var actual = GetSinglePullRequests(pr);

        //Assert
        actual.WorkItems.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void WorkItems_SmokeTest()
    {
        //Arrange
        var pr = BuildPullRequest();
        var expected = BuildWorkItem();
        LinkWorkItem(pr, expected);

        //Act
        var pullRequest = GetSinglePullRequests(pr);

        //Assert
        pullRequest.WorkItems.Count.ShouldBe(1);
        var actual = pullRequest.WorkItems.Single();
        actual.Id.ShouldBe(expected.Id);
    }

    [TestMethod]
    public void MultiPullRequests_MultiWorkItems()
    {
        //Arrange
        var pr1 = BuildPullRequest();
        var pr2 = BuildPullRequest();
        var pr3 = BuildPullRequest();
        var workItem1 = BuildWorkItem();
        var workItem2 = BuildWorkItem();
        var workItem3 = BuildWorkItem();
        AddPullRequest(pr1);
        AddPullRequest(pr2);
        AddPullRequest(pr3);
        LinkWorkItem(pr1, workItem1);
        LinkWorkItem(pr3, workItem1);
        LinkWorkItem(pr3, workItem2);
        
        //Act
        var prs = GetPullRequests();

        //Assert
        prs.Length.ShouldBe(3);
        //pr1
        prs.Single(pr => pr.Id == pr1.PullRequestId).WorkItems.Count.ShouldBe(1);
        prs.Single(pr => pr.Id == pr1.PullRequestId).WorkItems.Select(wi => wi.Id).ShouldContain(workItem1.Id);
        //pr2
        prs.Single(pr => pr.Id == pr2.PullRequestId).WorkItems.ShouldBeEmpty();
        //pr3
        prs.Single(pr => pr.Id == pr3.PullRequestId).WorkItems.Count.ShouldBe(2);
        prs.Single(pr => pr.Id == pr3.PullRequestId).WorkItems.Select(wi => wi.Id).ShouldBe(new[] { workItem1.Id, workItem2.Id });
    }

    #endregion Work Items
}