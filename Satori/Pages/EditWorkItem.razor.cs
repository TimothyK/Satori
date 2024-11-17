using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Pages.ViewModels;

namespace Satori.Pages;

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



    private void OpenWorkItem(WorkItem workItem)
    {
        JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

}

public class WorkItemCommentViewModel(WorkItem? workItem, TimeEntry[] timeEntries)
    : CommentViewModel(
        CommentType.WorkItem
        , CommentType.WorkItem.GetComment(timeEntries.FirstOrDefault(entry => entry.Task == workItem))
        , timeEntries
        , timeEntries.Where(entry => entry.Task == workItem)
    )
{
    public event EventHandler<CancelEventArgs>? WorkItemActivating;
    public event EventHandler? WorkItemActivated;

    protected virtual CancelEventArgs OnWorkItemActivating()
    {
        var e = new CancelEventArgs();
        WorkItemActivating?.Invoke(this, e);
        return e;
    }

    protected virtual void OnWorkItemActivated()
    {
        WorkItemActivated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The ToggleActive button also serves as the Add button to add a new work item.
    /// The Add functionality is handled by <see cref="OnWorkItemActivating"/>.
    /// </summary>
    /// <param name="timeEntry"></param>
    public override void ToggleActive(TimeEntry timeEntry)
    {
        if (!IsActive[timeEntry])
        {
            var e = OnWorkItemActivating();
            if (e.Cancel)
            {
                return;
            }
        }

        base.ToggleActive(timeEntry);
            
        if (IsActive[timeEntry])
        {
            OnWorkItemActivated();
        }
    }

    public WorkItem? WorkItem { get; set; } = workItem;
}

public class CancelEventArgs : EventArgs
{
    public bool Cancel { get; set; }
}