using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Services;

/// <summary>
/// Service to update the completed work on Azure DevOps work items
/// </summary>
/// <param name="azureDevOpsServer"></param>
public class CompletedWorkService(IAzureDevOpsServer azureDevOpsServer)
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
        var workItem = (await azureDevOpsServer.GetWorkItemsAsync(workItemId)).SingleOrDefault() 
                       ?? throw new InvalidOperationException($"Work Item ID {workItemId} was not found");
        if (WorkItemType.FromApiValue(workItem.Fields.WorkItemType) != WorkItemType.Task)
        {
            throw new InvalidOperationException($"Work Item {workItemId} is not a task");
        }

        if (workItem.Fields.OriginalEstimate == null)
        {

        }

        var patchItems = new List<WorkItemPatchItem>
        {
            new()
            {
                Operation = Operation.Add,
                Path = "/fields/Microsoft.VSTS.Scheduling.CompletedWork",
                Value = adjustment
            }
        };

        await azureDevOpsServer.PatchWorkItemAsync(workItemId, patchItems);
    }
}