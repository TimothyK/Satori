using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.Abstractions;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.PullRequests.ActionItems;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AppServices.ViewModels.WorkItems.ActionItems;
using Satori.Kimai.ViewModels;
using Satori.Pages.Components;

namespace Satori.Pages.SprintBoard;

public partial class ActionItemView
{
    [Parameter]
    public required ActionItem ActionItem { get; set; }

    /// <summary>
    /// Work Item related to the Action Item, if any.
    /// </summary>
    private WorkItem? WorkItem => (ActionItem as WorkItemActionItem)?.WorkItem;

    /// <summary>
    /// Work Item related to the board work item (PBI or Bug) on the Sprint Board row.
    /// </summary>
    [Parameter]
    public required WorkItem ParentWorkItem { get; set; }

    [Parameter]
    public EventCallback HasChanged { get; set; }

    [Parameter] 
    public required IReadOnlyCollection<int> RunningWorkItemIds { get; set; }

    private bool IsRunning => (ActionItem as TaskActionItem)?.WorkItem.Id.IsIn(RunningWorkItemIds) ?? false;

    #region Menu

    private bool _isMenuOpen;
    private bool _isMenuHovered;
    private bool _isWaitsForSubMenuOpen;
    private bool _isWaitsForSubMenuHovered;

    private void ShowMenu()
    {
        _isMenuOpen = true;
        StateHasChanged();
    }

    private async Task OnButtonMouseLeaveAsync()
    {
        await Task.Delay(100);
        if (!_isMenuHovered)
        {
            _isMenuOpen = false;
            StateHasChanged();
        }
    }

    private void OnMenuMouseEnter()
    {
        _isMenuHovered = true;
    }

    private async Task OnMenuMouseLeaveAsync()
    {
        _isMenuHovered = false;
        // Small delay to allow for accidental mouseout
        await Task.Delay(100);
        if (!_isMenuHovered)
        {
            _isMenuOpen = false;
            StateHasChanged();
        }
    }

    private void OnWaitsForWorkItemMouseEnter()
    {
        _isWaitsForSubMenuOpen = true;
        _isWaitsForSubMenuHovered = true;
        StateHasChanged();
    }

    private async Task OnWaitsForWorkItemMouseLeaveAsync()
    {
        _isWaitsForSubMenuHovered = false;
        await Task.Delay(100);
        if (!_isWaitsForSubMenuHovered)
        {
            _isWaitsForSubMenuOpen = false;
            StateHasChanged();
        }
    }

    private void OnWaitsForMouseEnter()
    {
        _isWaitsForSubMenuOpen = true;
        StateHasChanged();
    }

    private async Task OnWaitsForMouseLeaveAsync()
    {
        await Task.Delay(100);
        if (!_isWaitsForSubMenuHovered)
        {
            _isWaitsForSubMenuOpen = false;
            StateHasChanged();
        }
    }

    #endregion Menu

    #region Open

    private async Task OnOpenClickAsync()
    {
        switch (ActionItem)
        {
            case WorkItemActionItem workItemAction:
                await OpenWorkItemAsync(workItemAction.WorkItem);
                break;
            case PullRequestActionItem prAction:
                await OpenPullRequestAsync(prAction.PullRequest);
                break;
        }
    }

    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

    private async Task OpenPullRequestAsync(PullRequest pullRequest)
    {
        await JsRuntime.InvokeVoidAsync("open", pullRequest.Url, "_blank");
    }

    #endregion Open

    #region Start Timer

    private bool ShouldShowStartTimer => 
        KimaiServer.Enabled 
        && WorkItem != null 
        && WorkItem.AssignedTo == Person.Me
        && WorkItem.Type == WorkItemType.Task;
    private bool WillShowStartTimerDialog => WorkItem?.KimaiActivity == null;

    private SelectProjectDialog? _startTimerDialog;

    private async Task OnStartTimerClickAsync()
    {
        var workItem = WorkItem ?? throw new InvalidOperationException();
        _isMenuOpen = false;
        _isWaitsForSubMenuOpen = false;

        if (workItem.KimaiActivity == null)
        {
            _startTimerDialog?.ShowDialog(workItem);
        }
        else
        {
            await StartTimerAsync(workItem, workItem.KimaiActivity);
        }
    }

    private async Task OnStartTimerDialogSaveAsync((Project?, Activity?) value)
    {
        var workItem = _startTimerDialog?.WorkItem ?? throw new InvalidOperationException();

        var (_, activity) = value;
        if (activity == null)
        {
            return;
        }

        await StartTimerAsync(workItem, activity);
    }

    private async Task StartTimerAsync(WorkItem workItem, Activity activity)
    {
        await TimerService.StartTimerAsync(workItem, activity);
        await HasChanged.InvokeAsync();
    }

    #endregion Start Timer

    #region Start Timer from Pull Request

    private IEnumerable<WorkItem> PullRequestTimerTasks
    {
        get
        {
            if (!(KimaiServer.Enabled
                  && ActionItem is PullRequestActionItem pullRequestActionItem
                  && pullRequestActionItem.On.Select(review => review.Person).Contains(Person.Me)))
            {
                return [];
            }

            return ParentWorkItem
                .Children
                .Where(task => task.Type == WorkItemType.Task) // sanity check
                .Where(task => task.AssignedTo == Person.Me);
        }
    }

    private async Task OnPullRequestStartTimerClickAsync(WorkItem task)
    {
        _isMenuOpen = false;
        _isWaitsForSubMenuOpen = false;

        if (task.KimaiActivity == null)
        {
            _startTimerDialog?.ShowDialog(task);
        }
        else
        {
            await StartTimerAsync(task, task.KimaiActivity);
        }
    }

    #endregion Start Timer from Pull Request

    #region Fund Dialog

    private SelectProjectDialog? _fundDialog;

    private void OpenFundDialog()
    {
        var workItem = WorkItem ?? throw new InvalidOperationException();
        _isMenuOpen = false;
        _isWaitsForSubMenuOpen = false;

        _fundDialog?.ShowDialog(workItem);
    }

    private async Task OnFundDialogSaveAsync((Project?, Activity?) value)
    {
        var (project, activity) = value;
        var workItem = WorkItem ?? throw new InvalidOperationException();

        await WorkItemUpdateService.UpdateProjectCodeAsync(workItem, project, activity);

        await HasChanged.InvokeAsync();
    }

    #endregion Fund Dialog

    #region Create Predecessor Link

    private async Task OnCreatePredecessorLinkClickAsync(WorkItem predecessor)
    {
        var successor = (ActionItem as TaskActionItem)?.WorkItem ?? throw new InvalidOperationException("Action Item should be a Task");
        await WorkItemUpdateService.CreateDependencyLinkAsync(predecessor, successor);

        _isMenuOpen = false;
        _isWaitsForSubMenuOpen = false;

        await HasChanged.InvokeAsync();
    }

    private bool HasWaitsForMenu => WaitsForSiblings().Any();

    private IEnumerable<WorkItem> WaitsForSiblings()
    {
        if (ActionItem is not TaskActionItem actionItem 
            || actionItem.WorkItem.State != ScrumState.ToDo
           )
        {
            return [];
        }

        return actionItem.WorkItem
                   .Parent
                   ?.Children
                   .Except(actionItem.WorkItem.Yield())
                   .Where(task => task.State < ScrumState.Done) 
               ?? [];
    }

    #endregion Create Predecessor Link

}