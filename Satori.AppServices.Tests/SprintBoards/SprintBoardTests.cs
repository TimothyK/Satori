using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles.Builders;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AzureDevOps.Models;
using Shouldly;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class SprintBoardTests
{
    private readonly TestAzureDevOpsServer _azureDevOpsServer;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private Uri AzureDevOpsRootUrl => _azureDevOpsServer.AsInterface().ConnectionSettings.Url;

    public SprintBoardTests()
    {
        _azureDevOpsServer = new TestAzureDevOpsServer();
        _builder = _azureDevOpsServer.CreateBuilder();
    }

    #region Helpers

    #region Arrange

    private Iteration BuildIteration()
    {
        return BuildIteration(out _);
    }

    private Iteration BuildIteration(out Team team)
    {
        _builder.BuildTeam(out team).WithIteration(out var iteration);
        return iteration;
    }

    #endregion Arrange

    #region Act

    private Sprint[] GetSprints()
    {
        //Arrange
        var srv = new SprintBoardService(_azureDevOpsServer.AsInterface());

        //Act
        return srv.GetActiveSprintsAsync().Result.ToArray();
    }
    
    private Sprint GetSingleSprint()
    {
        return GetSprints().Single();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest()
    {
        //Arrange
        BuildIteration();

        //Act
        var sprints = GetSprints();

        //Assert
        sprints.ShouldNotBeEmpty();
    }

    [TestMethod]
    public void NoTeams_NoSprints()
    {
        //Act
        var sprints = GetSprints();

        //Assert
        sprints.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void NoIteration_NoSprints()
    {
        //Arrange
        _builder.BuildTeam();

        //Act
        var sprints = GetSprints();

        //Assert
        sprints.ShouldBeEmpty();
    }

    [TestMethod]
    public void Id()
    {
        //Arrange
        var iteration = BuildIteration();

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.Id.ShouldBe(iteration.Id);
    }
    
    [TestMethod]
    public void Name()
    {
        //Arrange
        var iteration = BuildIteration();

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.Name.ShouldBe(iteration.Name);
    }
    
    [TestMethod]
    public void Path()
    {
        //Arrange
        var iteration = BuildIteration();

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.IterationPath.ShouldBe(iteration.Path);
    }
    
    [TestMethod]
    public void StartDate()
    {
        //Arrange
        var iteration = BuildIteration();
        var startDate = iteration.Attributes.StartDate ?? throw new InvalidOperationException();

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.StartTime.ShouldBe(startDate);
    }
    
    [TestMethod]
    public void FinishDate()
    {
        //Arrange
        var iteration = BuildIteration();
        var finishDate = iteration.Attributes.FinishDate ?? throw new InvalidOperationException();

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.FinishTime.ShouldBe(finishDate);
    }
    
    [TestMethod]
    public void TeamId()
    {
        //Arrange
        BuildIteration(out var team);

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.TeamId.ShouldBe(team.Id);
    }
    
    [TestMethod]
    public void TeamName()
    {
        //Arrange
        BuildIteration(out var team);

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.TeamName.ShouldBe(team.Name);
    }
    
    [TestMethod]
    public void TeamAvatarUrl()
    {
        //Arrange
        BuildIteration(out var team);

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.TeamAvatarUrl.ShouldBe($"{AzureDevOpsRootUrl}/_api/_common/IdentityImage?id={team.Id}");
    }
    
    [TestMethod]
    public void SprintBoardUrl()
    {
        //Arrange
        var iteration = BuildIteration(out var team);

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.SprintBoardUrl.ShouldBe($"{AzureDevOpsRootUrl}/{team.ProjectName}/_sprints/taskBoard/{team.Name}/{iteration.Path}");
    }

}