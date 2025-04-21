using CodeMonkeyProjectiles.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AzureDevOps.Models;
using Shouldly;
using PullRequest = Satori.AzureDevOps.Models.PullRequest;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class PeopleTests
{
    private readonly TestAzureDevOpsServer _azureDevOpsServer;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private readonly TestTimeServer _timeServer = new();

    public PeopleTests()
    {
        _azureDevOpsServer = new TestAzureDevOpsServer();
        _builder = _azureDevOpsServer.CreateBuilder();
    }

    #region Helpers

    #region Arrange

    private static Sprint BuildSprint()
    {
        return Builder.Builder<Sprint>.New().Build(int.MaxValue);
    }

    private static class People
    {
        public static readonly User Alice = BuildUser(nameof(Alice));
        public static readonly User Bob = BuildUser(nameof(Bob));
        public static readonly User Cathy = BuildUser(nameof(Cathy));
        public static readonly User Dave = BuildUser(nameof(Dave));
        //public static readonly User Emily = BuildUser(nameof(Emily));
        //public static readonly User Fred = BuildUser(nameof(Fred));

        private static User BuildUser(string name)
        {
            var user = Builder.Builder<User>.New().Build(int.MaxValue);

            user.DisplayName = name;
            user.UniqueName = name;
            user.ImageUrl = $"http://devops.test/Org/_api/_common/identityImage?id={user.Id}";

            return user;
        }

    }


    #endregion Arrange

    #region Act

    private async Task<WorkItem[]> GetWorkItemsAsync(params Sprint[] sprints)
    {
        var srv = new SprintBoardService(_azureDevOpsServer.AsInterface(), _timeServer, new AlertService(), new NullLoggerFactory());

        var workItems = (await srv.GetWorkItemsAsync(sprints)).ToArray();
        await srv.GetPullRequestsAsync(workItems);

        return workItems;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.AssignedTo = People.Alice;

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single();
        actual.WithPeople.Count.ShouldBe(1);
        actual.ShouldBeWith(People.Alice);
    }
    
    [TestMethod]
    public async Task ChildTask()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = People.Bob;

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single();
        actual.WithPeople.Count.ShouldBe(2);
        actual.ShouldBeWith(People.Alice);
        actual.ShouldBeWith(People.Bob);
    }
    
    [TestMethod]
    public async Task PullRequest_Author()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
            
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single();
        actual.WithPeople.Count.ShouldBe(2);
        actual.ShouldBeWith(People.Alice);
        actual.ShouldBeWith(People.Bob);
    }
    
    [TestMethod]
    public async Task PullRequest_Reviewer()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
            
        workItem.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy);

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single();
        actual.WithPeople.Count.ShouldBe(3);
        actual.ShouldBeWith(People.Alice);
        actual.ShouldBeWith(People.Bob);
        actual.ShouldBeWith(People.Cathy);
    }
    
    [TestMethod]
    public async Task WithPeopleIsDistinct()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint)
            .AddChild(out var task);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(workItem);
            
        workItem.Fields.AssignedTo = People.Alice;
        task.Fields.AssignedTo = People.Alice;
        pullRequest.CreatedBy = People.Bob;
        pullRequest.AddReviewer(People.Cathy);
        pullRequest.AddReviewer(People.Alice);

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single();
        actual.WithPeople.Count.ShouldBe(3);
        actual.ShouldBeWith(People.Alice);
        actual.ShouldBeWith(People.Bob);
        actual.ShouldBeWith(People.Cathy);
    }

    [TestMethod]
    public async Task ChildTaskPullRequestReviewer()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem().WithSprint(sprint)
            .AddChild(out var task);
        _builder.BuildPullRequest(out var pullRequest).WithWorkItem(task);
            
        pullRequest.AddReviewer(People.Dave);

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single();
        actual.ShouldBeWith(People.Dave);
    }
    
    [TestMethod]
    public async Task Unassigned_IsIncluded()
    {
        //Arrange
        var sprint = BuildSprint();
        _builder.BuildWorkItem(out var workItem).WithSprint(sprint);
        workItem.Fields.AssignedTo = null;

        //Act
        var workItems = await GetWorkItemsAsync(sprint);

        //Assert
        var actual = workItems.Single();
        actual.ShouldBeWith(Person.Empty);
    }
}

internal static class WorkItemPeopleExtensions
{
    public static void ShouldBeWith(this WorkItem workItem, User user)
    {
        workItem.WithPeople.Select(person => person.AzureDevOpsId)
            .ShouldContain(user.Id, $"In Work Item {workItem}, {user.DisplayName} should be a member of {nameof(workItem.WithPeople)}, but the people were {string.Join(", ", workItem.WithPeople.Select(p => p.DisplayName))}");
    }
    
    public static void ShouldBeWith(this WorkItem workItem, Person user)
    {
        workItem.WithPeople.Select(person => person.AzureDevOpsId)
            .ShouldContain(user.AzureDevOpsId, $"In Work Item {workItem}, {user.DisplayName} should be a member of {nameof(workItem.WithPeople)}, but the people were {string.Join(", ", workItem.WithPeople.Select(p => p.DisplayName))}");
    }

    public static Reviewer AddReviewer(this PullRequest pullRequest, User user)
    {
        var reviewer = new Reviewer
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            ImageUrl = user.ImageUrl,
            Url = user.Url,
            UniqueName = user.UniqueName,
            ReviewerUrl = user.ImageUrl,
            Vote = (int) ReviewVote.NoVote,
        };

        pullRequest.Reviewers = pullRequest.Reviewers.Concat(reviewer.Yield()).ToArray();
        return reviewer;
    }
}