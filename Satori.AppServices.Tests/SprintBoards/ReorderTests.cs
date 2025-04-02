using Builder;
using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps.Models;
using Satori.TimeServices;
using Shouldly;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Tests.SprintBoards;

[TestClass]
public class ReorderTests
{
    #region Helpers

    #region Arrange

    private static Sprint BuildSprint()
    {
        return Builder<Sprint>.New().Build(int.MaxValue);
    }

    private static List<WorkItem> BuildWorkItems(int count)
    {
        var sprint = BuildSprint();
        
        var workItems = Builder<WorkItem>.New().BuildMany(count, (wi, _) =>
        {
            wi.Id = Sequence.WorkItemId.Next();
            wi.Sprint = sprint;
        });

        workItems = workItems.OrderBy(wi => wi.AbsolutePriority).ToList();
        for (var i = 0; i < workItems.Count; i++)
        {
            workItems[i].SprintPriority = i + 1;
        }

        return workItems;
    }

    #endregion Arrange

    #region Act

    private static async Task ReorderWorkItemsAsync(ReorderRequest request)
    {
        //Arrange
        var workItems = request.AllWorkItems;
        var azureDevOps = new ReorderAzureDevOpsServer(workItems);
        var srv = new SprintBoardService(azureDevOps.AsInterface(), new TimeServer(), new AlertService());
        LogRequest(request);

        //Act
        await srv.ReorderWorkItemsAsync(request);

        //Assert
        Console.WriteLine("Assert: Work Items (after):");
        LogWorkItems(workItems);

    }

    private static void LogRequest(ReorderRequest request)
    {
        Console.WriteLine($"Arrange: {request}");

        Console.WriteLine("Arrange: Work Items (before):");
        LogWorkItems(request.AllWorkItems);
    }

    private static void LogWorkItems(WorkItem[] workItems)
    {
        foreach (var workItem in workItems.OrderBy(wi => wi.AbsolutePriority))
        {
            Console.WriteLine($"{workItem.Id,10} {workItem.AbsolutePriority,10:N2} {workItem.Sprint!.TeamName}-{workItem.SprintPriority}");
        }
    }

    #endregion Act

    #endregion Helpers

    /// <summary>
    /// Sanity check on BuildWorkItem
    /// </summary>
    [TestMethod]
    public void BuildWorkItems_ReturnsOrderedWorkItems()
    {
        //Act 
        var workItems = BuildWorkItems(3);

        //Assert
        workItems.Count.ShouldBe(3);
        workItems[0].AbsolutePriority.ShouldBeLessThan(workItems[1].AbsolutePriority);
        workItems[1].AbsolutePriority.ShouldBeLessThan(workItems[2].AbsolutePriority);
        workItems[0].SprintPriority.ShouldBe(1);
        workItems[1].SprintPriority.ShouldBe(2);
        workItems[2].SprintPriority.ShouldBe(3);
    }

    [TestMethod]
    public async Task ASmokeTest_ChangesAbsolutePriority()
    {
        //Arrange
        var workItems = BuildWorkItems(3);
        var request = new ReorderRequest(workItems.ToArray(), workItems[0].Id.Yield().ToArray(), RelativePosition.Below, target: workItems[1]);

        //Act
        await ReorderWorkItemsAsync(request);

        //Assert
        workItems[0].AbsolutePriority.ShouldBeGreaterThan(workItems[1].AbsolutePriority);
        workItems[0].AbsolutePriority.ShouldBeLessThan(workItems[2].AbsolutePriority);
    }
    
    [TestMethod]
    public async Task Reorder_ChangesSprintPriority()
    {
        //Arrange
        var workItems = BuildWorkItems(3);
        var request = new ReorderRequest(workItems.ToArray(), workItems[0].Id.Yield().ToArray(), RelativePosition.Below, target: workItems[1]);

        //Act
        await ReorderWorkItemsAsync(request);

        //Assert
        workItems[0].SprintPriority!.Value.ShouldBeGreaterThan(workItems[1].SprintPriority!.Value);
        workItems[0].SprintPriority!.Value.ShouldBeLessThan(workItems[2].SprintPriority!.Value);
    }
    
    [TestMethod]
    public async Task Reorder_MoveToBottomImplicit()
    {
        //Arrange
        var workItems = BuildWorkItems(3);
        var first = workItems.First();
        var last = workItems.Last();
        var request = new ReorderRequest(workItems.ToArray(), first.Id.Yield().ToArray(), RelativePosition.Below, target: last);

        //Act
        await ReorderWorkItemsAsync(request);

        //Assert
        first.AbsolutePriority.ShouldBeGreaterThan(last.AbsolutePriority);
    }
    
    [TestMethod]
    public async Task Reorder_MoveToBottomExplicit()
    {
        //Arrange
        var workItems = BuildWorkItems(3);
        var first = workItems.First();
        var last = workItems.Last();
        var request = new ReorderRequest(workItems.ToArray(), first.Id.Yield().ToArray(), RelativePosition.Below, target: null);

        //Act
        await ReorderWorkItemsAsync(request);

        //Assert
        first.AbsolutePriority.ShouldBeGreaterThan(last.AbsolutePriority);
    }

    [TestMethod]
    public async Task Reorder_MoveAbove()
    {
        //Arrange
        var workItems = BuildWorkItems(3);
        var first = workItems.First();
        var middle = workItems[1];
        var target = workItems.Last();
        var request = new ReorderRequest(workItems.ToArray(), target.Id.Yield().ToArray(), RelativePosition.Above, target: middle);

        //Act
        await ReorderWorkItemsAsync(request);

        //Assert
        target.AbsolutePriority.ShouldBeGreaterThan(first.AbsolutePriority);
        target.AbsolutePriority.ShouldBeLessThan(middle.AbsolutePriority);
    }
    
    [TestMethod]
    public async Task Reorder_MoveToTopImplicit()
    {
        //Arrange
        var workItems = BuildWorkItems(3);
        var first = workItems.First();
        var movingItem = workItems[1];
        var request = new ReorderRequest(workItems.ToArray(), movingItem.Id.Yield().ToArray(), RelativePosition.Above, target: first);

        //Act
        await ReorderWorkItemsAsync(request);

        //Assert
        movingItem.AbsolutePriority.ShouldBeLessThan(first.AbsolutePriority);
    }
    
    [TestMethod]
    public async Task Reorder_MoveToTopExplicit()
    {
        //Arrange
        var workItems = BuildWorkItems(3);
        var first = workItems.First();
        var movingItem = workItems[1];
        var request = new ReorderRequest(workItems.ToArray(), movingItem.Id.Yield().ToArray(), RelativePosition.Above, target: null);

        //Act
        await ReorderWorkItemsAsync(request);

        //Assert
        movingItem.AbsolutePriority.ShouldBeLessThan(first.AbsolutePriority);
    }

    [TestMethod]
    public async Task Reorder_NoneSelected_ThrowsInvalidOp()
    {
        //Arrange
        var workItems = BuildWorkItems(3);
        var request = new ReorderRequest(workItems.ToArray(), [], RelativePosition.Above, target: null);

        //Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => ReorderWorkItemsAsync(request));

        //Assert
        ex.Message.ShouldBe("Work Items must be selected to be moved");
    }

    [TestMethod]
    public void BuildWorkItems_ReturnsNewSprintOnEachCall()
    {
        //Act
        var workItems = BuildWorkItems(3).Concat(BuildWorkItems(3)).ToArray();

        //Assert
        var sprints = workItems.Select(wi => wi.Sprint).Distinct().ToArray();
        sprints.Length.ShouldBe(2);
    }

    [TestMethod]
    public async Task ReorderAzureDevOpsServer_MoveWorkItemsForMultipleTeams_ThrowsInvalidOp()
    {
        //Arrange
        var allWorkItems = BuildWorkItems(3).Concat(BuildWorkItems(3)).OrderBy(wi => wi.AbsolutePriority).ToArray();
        var sprintGroups = allWorkItems.GroupBy(wi => wi.Sprint!).ToArray();
        sprintGroups.Length.ShouldBe(2);

        var workItemsToMove = sprintGroups.Select(g => g.Skip(1).First());
        
        var azureDevOps = new ReorderAzureDevOpsServer(allWorkItems);

        var firstSprint = sprintGroups[0].Key;
        var iteration = (IterationId)firstSprint;

        var operation = new ReorderOperation
        {
            PreviousId = allWorkItems.First().Id,
            NextId = allWorkItems.Skip(1).First().Id,
            Ids = workItemsToMove.Select(wi => wi.Id).ToArray()
        };

        //Act
        await Should.ThrowAsync<ShouldAssertException>(() => azureDevOps.AsInterface().ReorderBacklogWorkItemsAsync(iteration, operation));
    }
    
    [TestMethod]
    public async Task ReorderAzureDevOpsServer_MoveWorkItemsForDifferentTeam_ThrowsInvalidOp()
    {
        //Arrange
        var allWorkItems = BuildWorkItems(3);

        var azureDevOps = new ReorderAzureDevOpsServer(allWorkItems);

        var iteration = Builder<IterationId>.New().Build();

        var operation = new ReorderOperation
        {
            PreviousId = allWorkItems.Skip(1).First().Id,
            NextId = allWorkItems.Last().Id,
            Ids = allWorkItems.First().Yield().Select(wi => wi.Id).ToArray()
        };

        //Act
        await Should.ThrowAsync<ShouldAssertException>(() => azureDevOps.AsInterface().ReorderBacklogWorkItemsAsync(iteration, operation));
    }

    [TestMethod]
    public async Task Reorder_MixedSprints()
    {
        //Arrange
        var allWorkItems = BuildWorkItems(10).Concat(BuildWorkItems(10)).OrderBy(wi => wi.AbsolutePriority).ToArray();
        var sprintGroups = allWorkItems.GroupBy(wi => wi.Sprint!).ToArray();

        var itemIdsToMove = sprintGroups.Select(g => g.Skip(2).Take(3)).SelectMany(x => x).OrderBy(wi => wi.AbsolutePriority).Select(wi => wi.Id).ToArray();
        var last = allWorkItems.Last();
        var request = new ReorderRequest(allWorkItems.ToArray(), itemIdsToMove, RelativePosition.Below, target: last);

        //Act
        await ReorderWorkItemsAsync(request);

        //Assert
        var movedItems = allWorkItems.Where(wi => wi.Id.IsIn(itemIdsToMove)).OrderBy(wi => wi.AbsolutePriority).ToArray();
        movedItems.ShouldAllBe(wi => wi.AbsolutePriority > last.AbsolutePriority);
        movedItems.Select(wi => wi.Id).ShouldBe(itemIdsToMove, ignoreOrder:false);

        movedItems.Last().SprintPriority.ShouldBe(10);
        foreach (var g in sprintGroups)
        {
            var expected = Enumerable.Range(1, 10).ToArray();
            g.Select(wi => wi.SprintPriority!.Value).OrderBy(x => x).ToArray().ShouldBe(expected, ignoreOrder: false);
        }
        
    }
}