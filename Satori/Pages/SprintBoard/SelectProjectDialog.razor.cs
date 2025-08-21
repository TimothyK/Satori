using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Kimai.ViewModels;

namespace Satori.Pages.SprintBoard;

public partial class SelectProjectDialog : ComponentBase
{
    [Parameter] public WorkItem? WorkItem { get; set; }
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    public Customers? Customers { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (KimaiServer.Enabled)
        {
            Customers = await KimaiServer.GetCustomersAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnIsOpenChangedAsync(bool value)
    {
        IsOpen = value;
        await IsOpenChanged.InvokeAsync(value);
    }

    private async Task CloseAsync()
    {
        await OnIsOpenChangedAsync(false);
    }

    private async Task OkAsync()
    {
        await CloseAsync();
    }

    private async Task CancelAsync()
    {
        await CloseAsync();
    }

    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

}