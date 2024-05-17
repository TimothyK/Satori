using CodeMonkeyProjectiles.Linq;
using Moq;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Shouldly;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Tests.TestDoubles.AzureDevOps;

internal class ReorderAzureDevOpsServer
{
    private readonly WorkItem[] _workItems;
    private readonly Mock<IAzureDevOpsServer> _mock;

    public ReorderAzureDevOpsServer(IEnumerable<WorkItem> workItems)
    {
        _workItems = workItems as WorkItem[] ?? workItems.ToArray();
        _mock = new Mock<IAzureDevOpsServer>(MockBehavior.Strict);

        _mock.Setup(srv => srv.ReorderBacklogWorkItems(It.IsAny<IterationId>(), It.IsAny<ReorderOperation>()))
            .Returns((IterationId iteration, ReorderOperation operation) => ReorderBacklogWorkItems(iteration, operation));
    }

    public IAzureDevOpsServer AsInterface() => _mock.Object;

    private ReorderResult[] ReorderBacklogWorkItems(IterationId iteration, ReorderOperation operation)
    {
        Console.WriteLine($"Act: AzDO.ReorderBacklogWorkItems: For {iteration.TeamName} moving {string.Join(", ", operation.Ids)} between {operation.PreviousId} & {operation.NextId}");

        AssertIteration(iteration, operation);

        var previousPosition = _workItems.SingleOrDefault(wi => wi.Id == operation.PreviousId)?.AbsolutePriority ?? 0.0;
        var nextPosition = _workItems.SingleOrDefault(wi => wi.Id == operation.NextId)?.AbsolutePriority
                           ?? _workItems.Select(wi => wi.AbsolutePriority).Max() + 100.0;

        if (previousPosition >= nextPosition)
        {
            throw new InvalidOperationException("Position of items is invalid");
        }

        var gap = (nextPosition - previousPosition) / (operation.Ids.Length + 1);

        var results = new List<ReorderResult>();
        var position = previousPosition;
        foreach (var workItemId in operation.Ids)
        {
            position += gap;
            results.Add(new ReorderResult() { Id = workItemId, Order = position });
        }

        Console.WriteLine($"Act: AzDO.ReorderBacklogWorkItems: For {iteration.TeamName} moved  {string.Join(", ", results.Select(r => $"{r.Id}({r.Order:N2})"))}");

        return results.ToArray();
    }

    private void AssertIteration(IterationId iteration, ReorderOperation operation)
    {
        var iterations = _workItems
            .Where(wi => wi.Id.IsIn(operation.Ids))
            .SelectWhereHasValue(wi => wi.Sprint?.Id)
            .Distinct()
            .ToArray();

        iterations.Length.ShouldBe(1, "All WorkItems being moved must belong to the same iteration");
        iterations.Single().ShouldBe(iteration.Id, "All WorkItems being moved must belong to the given iteration");
    }
}