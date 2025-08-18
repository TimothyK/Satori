using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels.Abstractions;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.PullRequests.ActionItems;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AppServices.ViewModels.WorkItems.ActionItems;

namespace Satori.Pages.SprintBoard;

public partial class ActionItemView
{
    [Parameter]
    public required ActionItem ActionItem { get; set; }

    [Parameter]
    public EventCallback HasChanged { get; set; }

    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

    private async Task OpenPullRequestAsync(PullRequest pullRequest)
    {
        await JsRuntime.InvokeVoidAsync("open", pullRequest.Url, "_blank");
    }

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

    private async Task CreateWaitsForLinkAsync(WorkItem predecessor)
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

    private bool _showSelectProjectDialog;

    private void OpenFundDialog()
    {
        _showSelectProjectDialog = true;
    }

    private Task OnIsOpenChangedAsync(bool arg)
    {
        _showSelectProjectDialog = false;

        return Task.CompletedTask;
    }
}