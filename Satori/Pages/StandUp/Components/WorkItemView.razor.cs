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

    private CssClass ProjectCodeCssClass { get; set; } = CssClass.None;

    private string ProjectCode
    {
        get
        {
            var requiredActivity = WorkItem?.KimaiActivity ?? WorkItem?.Parent?.KimaiActivity;

            if (requiredActivity != null)
            {
                return $"{requiredActivity.Project.ProjectCode}.{requiredActivity.ActivityCode}";
            }

            var requiredProject = WorkItem?.KimaiProject ?? WorkItem?.Parent?.KimaiProject;
            return requiredProject != null ? requiredProject.ProjectCode 
                : string.Empty;
        }
    }

    private void VerifyProjectCode()
    {
        var requiredActivity = WorkItem?.KimaiActivity ?? WorkItem?.Parent?.KimaiActivity;
        var requiredProject = requiredActivity?.Project ?? WorkItem?.KimaiProject ?? WorkItem?.Parent?.KimaiProject;

        bool isValid;
        if (requiredActivity != null)
        {
            isValid = requiredActivity.Id == Activity.ActivityId;
        }
        else if (requiredProject != null)
        {
            isValid = requiredProject.Id == Activity.ParentProjectSummary.ProjectId;
        }
        else
        {
            isValid = true;
        }

        ProjectCodeCssClass = isValid ? VisibleCssClass.Hidden
            : new CssClass("project-code-invalid");
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