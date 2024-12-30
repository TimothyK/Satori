using Satori.AppServices.Extensions;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

public class WorkItemUpdateService(
    IAzureDevOpsServer azureDevOps
    , UserService userService
)
{
    #region Create Task

    public async Task<WorkItem> CreateTaskAsync(WorkItem parent, string title, double estimate)
    {
        var fields = await BuildFieldsAsync(parent, title, estimate);

        var task = (await azureDevOps.PostWorkItemAsync(parent.ProjectName, fields)).ToViewModel();
        task = await SetInProgressAsync(task);
        
        task.Parent = parent;
        parent.Children.Add(task);

        return task;
    }

    private async Task<List<WorkItemPatchItem>> BuildFieldsAsync(WorkItem parent, string title, double estimate)
    {
        var me = await userService.GetCurrentUserAsync();

        var relation = new Dictionary<string, object>()
        {
            { "rel", "System.LinkTypes.Hierarchy-Reverse" },
            { "url", parent.ApiUrl},
        };

        estimate = estimate.ToNearest(0.1);
        var fields = new List<WorkItemPatchItem>()
        {
            new() { Operation = Operation.Add, Path = "/fields/System.Title", Value = title },
            new() { Operation = Operation.Add, Path = "/fields/System.AssignedTo", Value = me.DisplayName },
            new() { Operation = Operation.Add, Path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate", Value = estimate },
            new() { Operation = Operation.Add, Path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork", Value = estimate },
            new() { Operation = Operation.Add, Path = "/relations/-", Value = relation },
        };
        if (!string.IsNullOrEmpty(parent.AreaPath))
        {
            fields.Add(new WorkItemPatchItem { Operation = Operation.Add, Path = "/fields/System.AreaPath", Value = parent.AreaPath });
        }
        if (!string.IsNullOrEmpty(parent.IterationPath))
        {
            fields.Add(new WorkItemPatchItem { Operation = Operation.Add, Path = "/fields/System.IterationPath", Value = parent.IterationPath });
        }

        return fields;
    }

    private async Task<WorkItem> SetInProgressAsync(WorkItem task)
    {
        var fields = new List<WorkItemPatchItem>()
        {
            new() { Operation = Operation.Add, Path = "/fields/System.State", Value = ScrumState.InProgress.ToApiValue() },
            new() { Operation = Operation.Test, Path = "/rev", Value = task.Rev },
        };

        return (await azureDevOps.PatchWorkItemAsync(task.Id, fields)).ToViewModel();
    }

    #endregion Create Task


}