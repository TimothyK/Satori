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
        ViewModel.WorkItemActivating += ActivatingWorkItem;
        base.OnParametersSet();
    }

    #region Add Work Item

    private PositiveIntegerViewModel WorkItemInput { get; set; } = new();

    private void ActivatingWorkItem(object? sender, CancelEventArgs e)
    {
        if (sender != ViewModel) throw new InvalidOperationException();

        if (ViewModel.WorkItem != null)
        {
            return;
        }

        e.Cancel = !SetWorkItem();
    }

    private void AddWorkItemKeyUp(KeyboardEventArgs e)
    {
        if (e.Code.IsIn("Enter", "NumpadEnter"))
        {
            AddWorkItem();
        }
    }

    private void AddWorkItem()
    {
        if (!SetWorkItem())
        {
            return;
        }

        var defaultTimeEntry = ViewModel.IsActive.Keys.Last();
        ViewModel.ToggleActive(defaultTimeEntry);
    }

    /// <summary>
    /// Sets the <see cref="ViewModel"/>.<see cref="WorkItemCommentViewModel.WorkItem"/>
    /// to the value input by the user in <see cref="WorkItemInput"/>,
    /// if that work item exists in Azure DevOps.
    /// </summary>
    /// <returns>True if the ViewModel.WorkItem is set successfully</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private bool SetWorkItem()
    {
        if (ViewModel.WorkItem != null)
        {
            throw new InvalidOperationException("A work item has already been added");
        }

        if (!WorkItemInput.TryParse(out var workItemId))
        {
            return false;
        }
        
        //TODO: Lookup work item in AzDO

        return true;
    }

    #endregion Add Work Item
    
    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

}