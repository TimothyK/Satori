using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Shouldly;

namespace Satori.AzureDevOps.Tests.WorkItems;

[TestClass]
public class WorkItemRelationTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetIterationWorkItemsUrl(IterationId iteration) =>
        _connectionSettings.Url
            .AppendPathSegments(iteration.ProjectName, iteration.TeamName)
            .AppendPathSegment("_apis")
            .AppendPathSegment("work/teamSettings/iterations")
            .AppendPathSegment(iteration.Id)
            .AppendPathSegment("workItems")
            .AppendQueryParam("api-version", "6.1-preview");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private void SetResponse(Url url, string response)
    {
        _mockHttp.When(url).Respond("application/json", response);
    }

    private static class Iterations
    {
        public static readonly IterationId Simple = Builder.Builder<IterationId>.New().Build(int.MaxValue);
    }

    private static string GetPayload(IterationId iteration)
    {
        if (iteration.Id == Iterations.Simple.Id)
        {
            return SampleFiles.WorkItemResponses.WorkItemRelations;
        }

        throw new InvalidOperationException("Unknown Iteration");
    }

    #endregion Arrange

    #region Act

    private WorkItemRelation[] GetIterationWorkItems() => GetIterationWorkItems(Iterations.Simple);

    private WorkItemRelation[] GetIterationWorkItems(IterationId iteration)
    {
        //Arrange
        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();
        SetResponse(GetIterationWorkItemsUrl(iteration), GetPayload(iteration));

        //Act
        return srv.GetIterationWorkItemsAsync(iteration).Result;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest()
    {
        var workItems = GetIterationWorkItems();

        workItems.Length.ShouldBe(2);
        var workItem1 = workItems.Single(wi => wi.Target.Id == 1);
        workItem1.Source.ShouldBeNull();

        var workItem2 = workItems.Single(wi => wi.Target.Id == 2);
        var source2 = workItem2.Source;
        source2.ShouldNotBeNull();
        source2.Id.ShouldBe(1);
    }

}