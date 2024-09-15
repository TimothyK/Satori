using Satori.AppServices.Extensions;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.ExportPayloads;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Services;

/// <summary>
/// Service to update the completed work on Azure DevOps work items
/// </summary>
/// <param name="azureDevOpsServer"></param>
public class CompletedWorkService(IAzureDevOpsServer azureDevOpsServer) : ITaskAdjustmentExporter
{
    /// <summary>
    /// Adjusts the completed work field, and remaining work (in the opposite direction) if defined.
    /// </summary>
    /// <param name="workItemId"></param>
    /// <param name="adjustment">Amount to increment the Completed Work by</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task AdjustCompletedWorkAsync(int workItemId, double adjustment)
    {
        var workItem = (await azureDevOpsServer.GetWorkItemsAsync(workItemId)).Single();
        if (WorkItemType.FromApiValue(workItem.Fields.WorkItemType) != WorkItemType.Task)
        {
            throw new InvalidOperationException($"Work Item {workItemId} is not a task");
        }

        var patchItems = new List<WorkItemPatchItem>
        {
            new () { Operation = Operation.Test, Path = "/rev", Value = workItem.Rev },
            new()
            {
                Operation = Operation.Add,
                Path = "/fields/Microsoft.VSTS.Scheduling.CompletedWork",
                Value = ((workItem.Fields.CompletedWork ?? 0.0) + adjustment).ToNearest(0.05)
            }
        };

        if (workItem.Fields.OriginalEstimate == null && workItem.Fields.RemainingWork != null)
        {
            patchItems.Add(new WorkItemPatchItem
            {
                Operation = Operation.Add, 
                Path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate", 
                Value = workItem.Fields.RemainingWork
            });
        }

        var remainingWork = workItem.Fields.RemainingWork ?? workItem.Fields.OriginalEstimate;
        var isDone = ScrumState.FromApiValue(workItem.Fields.State) == ScrumState.Done;
        if (remainingWork != null && !isDone)
        {
            patchItems.Add(new WorkItemPatchItem
            {
                Operation = Operation.Add,
                Path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork",
                Value = (remainingWork.Value - adjustment).ToNearest(0.1)
            });
        }

        await azureDevOpsServer.PatchWorkItemAsync(workItemId, patchItems);
    }

    /// <inheritdoc />
    Task ITaskAdjustmentExporter.SendAsync(TaskAdjustment payload)
    {
        return AdjustCompletedWorkAsync(payload.WorkItemId, payload.Adjustment.TotalHours);
    }
}