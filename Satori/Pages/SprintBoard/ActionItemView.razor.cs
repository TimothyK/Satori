using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels.Abstractions;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.WorkItems;

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
}
