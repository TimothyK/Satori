using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels.Abstractions;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AppServices.ViewModels.WorkItems.ActionItems;

namespace Satori.Pages.SprintBoard;

public partial class ActionItemView
{
    [Parameter]
    public required ActionItem ActionItem { get; set; }

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


    private void CreateWaitsForLink()
    {
        // TODO: Create Link


        _isMenuOpen = false;
        _isWaitsForSubMenuOpen = false;
    }

    private bool HasMenu => HasWaitsForMenu;

    private bool HasWaitsForMenu => TaskSiblings().Any();

    private IEnumerable<WorkItem> TaskSiblings()
    {
        if (ActionItem is not TaskActionItem actionItem 
            || actionItem.Task.State != ScrumState.ToDo
        )
        {
            return [];
        }

        return actionItem.Task
                   .Parent
                   ?.Children
                   .Except(actionItem.Task.Yield())
                   .Where(task => task.State < ScrumState.Done) 
               ?? [];
    }
}
