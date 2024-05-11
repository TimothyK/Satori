using Moq;
using Satori.AppServices.Tests.TestDoubles.Builders;
using Satori.AppServices.Tests.TestDoubles.Database;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles;

/// <summary>
/// Mock implementation of <see cref="IAzureDevOpsServer"/>
/// </summary>
/// <remarks>
/// <para>
/// This class doesn't implement <see cref="IAzureDevOpsServer"/>, but it can be used as that via <see cref="AsInterface"/>.
/// </para>
/// <para>
/// This class does hold a database of objects that will be returned from <see cref="IAzureDevOpsServer"/>.
/// This database is internal, but can be written to via <see cref="CreateBuilder"/>.
/// </para>
/// </remarks>
internal class TestAzureDevOpsServer
{
    private const string AzureDevOpsRootUrl = "http://devops.test/Org";

    #region Database

    private readonly AzureDevOpsDatabase _database = new();
    public AzureDevOpsDatabaseBuilder CreateBuilder() => new(_database);

    #endregion Database

    private readonly Mock<IAzureDevOpsServer> _mock;

    public TestAzureDevOpsServer()
    {
        _mock = new Mock<IAzureDevOpsServer>(MockBehavior.Strict);
        _mock.Setup(srv => srv.ConnectionSettings)
            .Returns(new ConnectionSettings { Url = new Uri(AzureDevOpsRootUrl), PersonalAccessToken = "token" });

        _mock.Setup(srv => srv.GetPullRequestsAsync())
            .ReturnsAsync(() => _database.GetPullRequests());

        _mock.Setup(srv => srv.GetPullRequestWorkItemIdsAsync(It.IsAny<PullRequest>()))
            .ReturnsAsync((PullRequest pr) => GetWorkItemMap(pr));

        _mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((IEnumerable<int> workItemIds) => GetWorkItems(workItemIds));

        _mock.Setup(srv => srv.GetTeamsAsync())
            .ReturnsAsync(() => _database.GetTeams());

        _mock.Setup(srv => srv.GetCurrentIterationAsync(It.IsAny<Team>()))
            .ReturnsAsync((Team team) => _database.GetIterationForTeam(team));

        _mock.Setup(srv => srv.GetIterationWorkItemsAsync(It.IsAny<IterationId>()))
            .ReturnsAsync((IterationId iteration) => _database.GetWorkItemsForIteration(iteration));

        return;
        IdMap[] GetWorkItemMap(PullRequest pullRequest)
        {
            return _database.GetWorkItemIdsForPullRequestId(pullRequest.PullRequestId)
                .Select(workItemId => Builder.Builder<IdMap>.New().Build(idMap => idMap.Id = workItemId))
                .ToArray();
        }

        WorkItem[] GetWorkItems(IEnumerable<int> workItemIds)
        {
            return _database.GetWorkItemsById(workItemIds).ToArray();
        }
    }

    public IAzureDevOpsServer AsInterface() => _mock.Object;

 
}