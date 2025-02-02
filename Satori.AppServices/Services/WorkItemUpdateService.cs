using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

/// <summary>
/// Service to create Azure DevOps tasks and update them with a new state and remaining work
/// </summary>
/// <param name="azureDevOps"></param>
/// <param name="userService"></param>
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


    #region Update Task

    public async Task UpdateTaskAsync(WorkItem task, ScrumState state, TimeSpan? remaining = null)
    {
        if (task.Type != WorkItemType.Task)
        {
            return;
        }
        var me = await userService.GetCurrentUserAsync();
        if (task.AssignedTo != me)
        {
            throw new InvalidOperationException("Only the assigned user can update a task");
        }
        var fields = BuildPatchItems(task, state, remaining);
        if (fields.None())
        {
            //Nothing to update
            return;
        }

        var patchResult = await azureDevOps.PatchWorkItemAsync(task.Id, fields);

        UpdateViewModel(task, patchResult);
    }

    private static List<WorkItemPatchItem> BuildPatchItems(WorkItem task, ScrumState state, TimeSpan? remaining)
    {
        var fields = new List<WorkItemPatchItem>();
        if (state != task.State)
        {
            fields.Add(new WorkItemPatchItem
            {
                Operation = Operation.Add, 
                Path = "/fields/System.State", 
                Value = state.ToApiValue() 
            });
        }

        if (state.IsIn(ScrumState.New, ScrumState.InProgress) && remaining != null)
        {
            remaining = remaining.Value.ToNearest(TimeSpan.FromMinutes(6));
            if (remaining != task.RemainingWork)
            {
                fields.Add(new WorkItemPatchItem
                {
                    Operation = Operation.Add, 
                    Path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork", 
                    Value = remaining .Value.TotalHours
                });
            }
        }

        if (task.OriginalEstimate == null && remaining != null)
        {
            fields.Add(new WorkItemPatchItem
            {
                Operation = Operation.Add, 
                Path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate", 
                Value = remaining.Value.TotalHours
            });
        }

        if (fields.Any())
        {
            fields.Add(new WorkItemPatchItem() { Operation = Operation.Test, Path = "/rev", Value = task.Rev });
        }

        return fields;
    }

    private static void UpdateViewModel(WorkItem task, AzureDevOps.Models.WorkItem patchResult)
    {
        var vm = patchResult.ToViewModel();

        task.State = vm.State;
        task.OriginalEstimate = vm.OriginalEstimate;
        task.RemainingWork = vm.RemainingWork;
        task.Rev = vm.Rev;
    }

    #endregion Update Task
}