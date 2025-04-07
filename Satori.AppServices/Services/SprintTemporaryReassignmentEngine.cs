using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

/// <summary>
/// Temporarily reassigns work items to a different sprint.
/// <see cref="ReassignAsync"/> returns an <see cref="IAsyncDisposable"/>, which when disposed will restore the work items back to their original sprint.
/// </summary>
/// <param name="azureDevOpsServer"></param>
internal class SprintTemporaryReassignmentEngine(IAzureDevOpsServer azureDevOpsServer)
{
    private readonly List<WorkItem> _workItems = [];

    public void Add(WorkItem[] workItems) => _workItems.AddRange(workItems);

    public void Add(WorkItem? workItem)
    {
        if (workItem != null)
        {
            _workItems.Add(workItem);
        }
    }

    public async Task<IAsyncDisposable> ReassignAsync(WorkItem target)
    {
        _workItems.RemoveAll(wi => IsSameSprint(wi, target));

        foreach (var workItem in _workItems)
        {
            await SprintReassignmentActions.ReassignWorkItemAsync(azureDevOpsServer, workItem, target);
        }

        return new UndoSprintReassignment(azureDevOpsServer, _workItems);
    }

    private static bool IsSameSprint(WorkItem a, WorkItem b)
    {
        return a.ProjectName == b.ProjectName
            && a.AreaPath == b.AreaPath 
            && a.IterationPath == b.IterationPath;
    }
}

internal class UndoSprintReassignment(IAzureDevOpsServer azureDevOpsServer, IEnumerable<WorkItem> workItems) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        foreach (var workItem in workItems)
        {
            await SprintReassignmentActions.ReassignWorkItemAsync(azureDevOpsServer, workItem, workItem);
        }
    }
}

internal static class SprintReassignmentActions
{
    public static async Task ReassignWorkItemAsync(IAzureDevOpsServer azureDevOpsServer, WorkItem workItem, WorkItem target)
    {
        IEnumerable<WorkItemPatchItem> patches = [
            new() {Operation = Operation.Replace, Path = "/fields/System.TeamProject", Value = target.ProjectName },
            new() {Operation = Operation.Replace, Path = "/fields/System.AreaPath", Value = target.AreaPath ?? string.Empty },
            new() {Operation = Operation.Replace, Path = "/fields/System.IterationPath", Value = target.IterationPath ?? string.Empty},
        ];
        await azureDevOpsServer.PatchWorkItemAsync(workItem.Id, patches);
    }
}

