using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Utilities;
using System.Text.RegularExpressions;

namespace Satori.Pages.StandUp.Components;

public partial class WorkItemView
{
    [Parameter]
    public WorkItem? WorkItem { get; set; }

    [Parameter] 
    public TimeSpan? TimeRemaining { get; set; }

    [Parameter]
    public required ActivitySummary Activity { get; set; }


    private Person CurrentUser { get; set; } = Person.Empty;

    protected override async Task OnInitializedAsync()
    {
        CurrentUser = await UserService.GetCurrentUserAsync();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        VerifyProjectCode();
    }

    private CssClass ParentProjectCodeCssClass { get; set; } = CssClass.None;
    private CssClass ProjectCodeCssClass { get; set; } = CssClass.None;

    private void VerifyProjectCode()
    {
        var kimaiProjectNumber = GetFirstInteger(Activity.ParentProjectSummary.ProjectName);

        if (string.IsNullOrEmpty(WorkItem?.Parent?.ProjectCode))
        {
            ParentProjectCodeCssClass = VisibleCssClass.Hidden;
        }
        else if (kimaiProjectNumber != GetFirstInteger(WorkItem?.Parent?.ProjectCode))
        {
            ParentProjectCodeCssClass = new CssClass("project-code-invalid");
        }
        else
        {
            ParentProjectCodeCssClass = VisibleCssClass.Hidden;
        }

        if (string.IsNullOrEmpty(WorkItem?.ProjectCode))
        {
            ProjectCodeCssClass = VisibleCssClass.Hidden;
        }
        else if (kimaiProjectNumber != GetFirstInteger(WorkItem?.ProjectCode))
        {
            ProjectCodeCssClass = new CssClass("project-code-invalid");
        }
        else
        {
            ProjectCodeCssClass = VisibleCssClass.Hidden;
        }

    }

    private static int? GetFirstInteger(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        const string pattern = @"\d+";
        var match = Regex.Match(value, pattern);

        if (match.Success && int.TryParse(match.Value, out var result))
        {
            return result;
        }

        return null;
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