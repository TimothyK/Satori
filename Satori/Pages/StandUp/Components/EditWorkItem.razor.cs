using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Pages.StandUp.Components.ViewModels;
using Satori.Pages.StandUp.Components.ViewModels.Models;

namespace Satori.Pages.StandUp.Components;

public partial class EditWorkItem
{
    [Parameter] public required WorkItemCommentViewModel ViewModel { get; set; }

    private Person CurrentUser { get; set; } = Person.Empty;

    protected override async Task OnInitializedAsync()
    {
        CurrentUser = await UserService.GetCurrentUserAsync();

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        ViewModel.WorkItemActivatingAsync += ActivatingWorkItemAsync;
        base.OnParametersSet();
    }

    #region Add Work Item

    private PositiveIntegerViewModel WorkItemInput { get; set; } = new();

    private async Task ActivatingWorkItemAsync(object? sender, CancelEventArgs e)
    {
        if (sender != ViewModel) throw new InvalidOperationException();

        if (ViewModel.WorkItem != null)
        {
            return;
        }

        e.Cancel = !await SetWorkItemAsync();
    }

    private async Task AddWorkItemKeyUpAsync(KeyboardEventArgs e)
    {
        if (e.Code.IsIn("Enter", "NumpadEnter"))
        {
            await AddWorkItemAsync();
        }
    }

    private async Task AddWorkItemAsync()
    {
        if (!await SetWorkItemAsync())
        {
            return;
        }

        var defaultTimeEntry = ViewModel.IsActive.Keys.Last();
        await ViewModel.ToggleActiveAsync(defaultTimeEntry);
    }

    /// <summary>
    /// Sets the <see cref="ViewModel"/>.<see cref="WorkItemCommentViewModel.WorkItem"/>
    /// to the value input by the user in <see cref="WorkItemInput"/>,
    /// if that work item exists in Azure DevOps.
    /// </summary>
    /// <returns>True if the ViewModel.WorkItem is set successfully</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<bool> SetWorkItemAsync()
    {
        if (ViewModel.WorkItem != null)
        {
            throw new InvalidOperationException("A work item has already been added");
        }

        if (!WorkItemInput.TryParse(out var workItemId))
        {
            return false;
        }
        
        var workItem = await StandUpService.GetWorkItemAsync(workItemId);
        if (workItem == null)
        {
            WorkItemInput.ValidationErrorMessage = "Work item not found";
            return false;
        }

        ViewModel.SetWorkItem(workItem);
        return true;
    }

    #endregion Add Work Item

    public async Task SetWorkItemToParentAsync()
    {
        var parent = ViewModel.WorkItem?.Parent ?? throw new InvalidOperationException();
        parent = await StandUpService.GetWorkItemAsync(parent.Id);
        if (parent == null)
        {
            //This should never happen
            ViewModel.WorkItem.Parent = null;  
        }
        else
        {
            ViewModel.SetWorkItem(parent);
        }
    }
    
    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

}