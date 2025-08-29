using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.Kimai;
using Satori.Kimai.ViewModels;
using Project = Satori.Kimai.ViewModels.Project;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

/// <summary>
/// Service to create Azure DevOps tasks and update them with a new state and remaining work
/// </summary>
public class WorkItemUpdateService
{
    private readonly IAzureDevOpsServer _azureDevOps;
    private readonly UserService _userService;
    private readonly IKimaiServer _kimai;

    /// <summary>
    /// Service to create Azure DevOps tasks and update them with a new state and remaining work
    /// </summary>
    /// <param name="azureDevOps"></param>
    /// <param name="userService"></param>
    /// <param name="kimai"></param>
    public WorkItemUpdateService(IAzureDevOpsServer azureDevOps
        , UserService userService
        ,IKimaiServer kimai
        )
    {
        _azureDevOps = azureDevOps;
        _userService = userService;
        _kimai = kimai;
    }

    #region Create Task

    public async Task<WorkItem> CreateTaskAsync(WorkItem parent, string title, double estimate)
    {
        var fields = await BuildFieldsAsync(parent, title, estimate);

        var task = await (await _azureDevOps.PostWorkItemAsync(parent.ProjectName, fields)).ToViewModelAsync(_kimai);
        task = await SetInProgressAsync(task);
        
        task.Parent = parent;
        parent.Children.Add(task);

        return task;
    }

    private async Task<List<WorkItemPatchItem>> BuildFieldsAsync(WorkItem parent, string title, double estimate)
    {
        var me = await _userService.GetCurrentUserAsync();

        var relation = new Dictionary<string, object>()
        {
            { "rel", LinkType.IsChildOf.ToApiValue() },
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

        return await (await _azureDevOps.PatchWorkItemAsync(task.Id, fields)).ToViewModelAsync(_kimai);
    }

    #endregion Create Task

    #region UpdateProjectCode

    public async Task UpdateProjectCodeAsync(WorkItem workItem, Project? project, Activity? activity)
    {
        project = activity?.Project ?? project;
        var projectCode = project?.ProjectCode;
        if (activity != null)
        {
            projectCode += "." + activity.ActivityCode;
        }
        projectCode ??= string.Empty;

        await UpdateProjectCodeAsync(workItem, projectCode);

        workItem.KimaiProject = project;
        workItem.KimaiActivity = activity;
        workItem.Rev++;
    }

    private async Task UpdateProjectCodeAsync(WorkItem workItem, string projectCode)
    {
        var fields = new List<WorkItemPatchItem>
        {
            new()
            {
                Operation = Operation.Add, 
                Path = "/fields/Custom.ProjectCode", 
                Value = projectCode
            }
        };

        await _azureDevOps.PatchWorkItemAsync(workItem.Id, fields);
    }

    #endregion UpdateProjectCode

    #region UpdateProjectCodeAndStatus

    public async Task UpdateProjectCodeAndStatus(WorkItem workItem, Activity activity)
    {
        var projectCode = activity.Project.ProjectCode + "." + activity.ActivityCode;

        var fields = new List<WorkItemPatchItem>
        {
            new()
            {
                Operation = Operation.Add, 
                Path = "/fields/Custom.ProjectCode", 
                Value = projectCode,
            },
            new()
            {
                Operation = Operation.Add, 
                Path = "/fields/System.State", 
                Value = ScrumState.InProgress.ToApiValue() 
            }
        };

        await _azureDevOps.PatchWorkItemAsync(workItem.Id, fields);
    }

    #endregion UpdateProjectCodeAndState

    #region CreateDependencyLink

    public async Task CreateDependencyLinkAsync(WorkItem predecessor, WorkItem successor)
    {
        var relation = new Dictionary<string, object>()
        {
            { "rel", LinkType.IsSuccessorOf.ToApiValue() },
            { "url", predecessor.ApiUrl },
        };
        var fields = new List<WorkItemPatchItem>
        {
            new()
            {
                Operation = Operation.Add,
                Path = "/relations/-",
                Value = relation
            }
        };
        await _azureDevOps.PatchWorkItemAsync(successor.Id, fields);
    }

    #endregion CreateDependencyLink

    #region Update Task

    public async Task UpdateTaskAsync(WorkItem task, ScrumState state, TimeSpan? remaining = null)
    {
        if (task.Type != WorkItemType.Task)
        {
            return;
        }
        var me = await _userService.GetCurrentUserAsync();
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

        var patchResult = await _azureDevOps.PatchWorkItemAsync(task.Id, fields);

        await UpdateViewModelAsync(task, patchResult, _kimai);
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

    private static async Task UpdateViewModelAsync(WorkItem task, AzureDevOps.Models.WorkItem patchResult, IKimaiServer kimai)
    {
        var vm = await patchResult.ToViewModelAsync(kimai);

        task.State = vm.State;
        task.OriginalEstimate = vm.OriginalEstimate;
        task.RemainingWork = vm.RemainingWork;
        task.Rev = vm.Rev;
    }

    #endregion Update Task
}