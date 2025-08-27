using Microsoft.Extensions.DependencyInjection;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.TimeServices;
using Shouldly;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class SprintBoardTests
{
    private readonly ServiceProvider _serviceProvider;
    private Uri AzureDevOpsRootUrl => _serviceProvider.GetRequiredService<IAzureDevOpsServer>().ConnectionSettings.Url;
    private readonly TestAlertService _alertService;
    private readonly TestTimeServer _timeServer;

    public SprintBoardTests()
    {
        var services = new SatoriServiceCollection();
        services.AddTransient<SprintBoardService>();
        _serviceProvider = services.BuildServiceProvider();

        _alertService = _serviceProvider.GetRequiredService<TestAlertService>();
        _timeServer = _serviceProvider.GetRequiredService<TestTimeServer>();
    }

    #region Helpers

    #region Arrange

    private Iteration BuildIteration()
    {
        return BuildIteration(out _);
    }

    private Iteration BuildIteration(out Team team)
    {
        var builder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
        builder.BuildTeam(out team).WithIteration(out var iteration);
        return iteration;
    }

    #endregion Arrange

    #region Act

    private Sprint[] GetSprints()
    {
        //Arrange
        var srv = _serviceProvider.GetRequiredService<SprintBoardService>();

        //Act
        return srv.GetActiveSprintsAsync().Result.ToArray();
    }
    
    private Sprint GetSingleSprint()
    {
        return GetSprints().Single();
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
        BuildIteration();

        //Act
        var sprints = GetSprints();

        //Assert
        sprints.ShouldNotBeEmpty();
    }

    #region No Data

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
        var builder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
        builder.BuildTeam();

        //Act
        var sprints = GetSprints();

        //Assert
        sprints.ShouldBeEmpty();
    }

    #endregion

    #region Iteration Properties

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

    #endregion Iteration Properties

    #region Date Bounds
    
    [TestMethod]
    public void LastDay_TreatedAsActive()
    {
        //Arrange
        var iteration = BuildIteration();
        var now = DateTimeOffset.UtcNow;
        _timeServer.SetTime(now);
        iteration.Attributes.FinishDate = now.Date;  //Finished at midnight today.

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.Id.ShouldBe(iteration.Id);
    }

    [TestMethod]
    public void Expired_Yesterday_ReturnsSprint()
    {
        //Arrange
        var iteration = BuildIteration();
        var now = DateTimeOffset.UtcNow;
        _timeServer.SetTime(now);
        iteration.Attributes.FinishDate = now.AddDays(-1);

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.Id.ShouldBe(iteration.Id);
    }

    /// <summary>
    /// The active sprint should have a grace period of 7 days.  This gives the scrum master a chance to set up the next sprint. 
    /// </summary>
    [TestMethod]
    public void Expired_ForSixDays_ReturnsSprint()
    {
        //Arrange
        var iteration = BuildIteration();
        var now = DateTimeOffset.UtcNow;
        _timeServer.SetTime(now);
        iteration.Attributes.FinishDate = now.AddDays(-6);

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.Id.ShouldBe(iteration.Id); 
    }

    [TestMethod]
    public void Expired_ForSevenDays_ReturnsNull()
    {
        //Arrange
        var iteration = BuildIteration();
        var now = DateTimeOffset.UtcNow;
        _timeServer.SetTime(now);
        iteration.Attributes.FinishDate = now.AddDays(-7);

        //Act
        var sprints = GetSprints();

        //Assert
        sprints.ShouldBeEmpty();
    }

    #endregion Date Bounds

    #region Team Properties

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
    public void ProjectName()
    {
        //Arrange
        BuildIteration(out var team);

        //Act
        var sprint = GetSingleSprint();

        //Assert
        sprint.ProjectName.ShouldBe(team.ProjectName);
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

    #endregion

    #region Permissions

    /// <summary>
    /// The Sprint Board should not show teams that the user does not have permissions to write work items to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Write Work Item permission would be required in order to adjust priorities across teams.
    /// Azure DevOps only allows adjusting priorities within a team.
    /// To support adjusting priorities across teams, work items get temporarily moved to one team.
    /// That requires Write permission.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void MissingPermission_NotReturned()
    {
        //Arrange
        BuildIteration(out var team);
        var azureDevOpsServer = _serviceProvider.GetRequiredService<TestAzureDevOpsServer>();
        azureDevOpsServer.RevokeProject(team.ProjectName);

        //Act
        var sprints = GetSprints();

        //Assert
        sprints.ShouldBeEmpty();
    }

    #endregion Permissions

    #region ConnectionErrors

    [TestMethod]
    public void ConnectionError_ReturnEmpty()
    {
        //Arrange
        var azureDevOpsServer = _serviceProvider.GetRequiredService<TestAzureDevOpsServer>();
        azureDevOpsServer.Mock.Setup(srv => srv.GetTeamsAsync()).Throws<ApplicationException>();

        //Act
        var sprints = GetSprints();

        //Assert
        sprints.ShouldBeEmpty();
        _alertService.DisableVerifications();
    }
    
    [TestMethod]
    public void ConnectionError_BroadcastsError()
    {
        //Arrange
        var azureDevOpsServer = _serviceProvider.GetRequiredService<TestAzureDevOpsServer>();
        azureDevOpsServer.Mock.Setup(srv => srv.GetTeamsAsync()).Throws<ApplicationException>();

        //Act
        GetSprints();

        //Assert
        _alertService.LastException.ShouldNotBeNull();
        _alertService.LastException.ShouldBeOfType<ApplicationException>();
        _alertService.DisableVerifications();
    }

    #endregion ConnectionErrors
}