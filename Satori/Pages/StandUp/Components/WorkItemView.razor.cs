using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.Pages.StandUp.Components;

public partial class WorkItemView
{
    [Parameter] 
    public WorkItem? WorkItem { get; set; }
    [Parameter] 
    public TimeSpan? TimeRemaining { get; set; }

    private Person CurrentUser { get; set; } = Person.Empty;

    protected override async Task OnInitializedAsync()
    {
        CurrentUser = await UserService.GetCurrentUserAsync();
    }

    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

    private bool NeedsEstimate =>
        WorkItem != null
        && WorkItem.Type == WorkItemType.Task
        && WorkItem.State.IsIn(ScrumState.ToDo, ScrumState.InProgress)
        && WorkItem.OriginalEstimate == null
        && WorkItem.RemainingWork == null;

}