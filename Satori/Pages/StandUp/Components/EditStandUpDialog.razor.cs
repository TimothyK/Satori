using System.Text;
using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Satori.AppServices.Services.CommentParsing;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Pages.StandUp.Components.ViewModels;
using Satori.Utilities;

namespace Satori.Pages.StandUp.Components;

public partial class EditStandUpDialog
{
    [Parameter]
    public required TimeEntry[] TimeEntries { get; set; }

    private ActivitySummary Activity { get; set; } = null!;
    private ProjectSummary Project { get; set; } = null!;
    private PeriodSummary Period => Project.ParentDay.ParentPeriod;

    private List<CommentViewModel> Comments { get; set; } = null!;

    protected override void OnParametersSet()
    {
        TimeEntries = TimeEntries
            .Where(t => !t.Exported)
            .OrderBy(t => t.Begin)
            .ToArray();

        if (TimeEntries.Length == 0)
        {
            return;
        }

        Activity = TimeEntries.Select(t => t.ParentActivitySummary).Distinct().Single();
        Project = Activity.ParentProjectSummary;

        Comments = BuildComments();
        Validate();

        base.OnParametersSet();
    }

    private void Validate()
    {
        TimeOverlappingValidationMessage = TimeEntries.Any(t => t.IsOverlapping) ? "Time entries are overlapping" : string.Empty;
        ActivityValidationMessage = GetActivityValidationMessage();

        foreach (var entry in TimeEntries)
        {
            var activeComments = Comments
                .Where(c => c.IsActive[entry])
                .Where(c => !string.IsNullOrEmpty(c.KimaiDescription))
                .ToArray();
            if (activeComments.None())
            {
                AttentionRequired = AttentionRequiredCssClass.Yes;
                CommentRequiredValidationMessage = "Enter at least one comment per time entry";
                return;
            }
            if (activeComments.OfType<WorkItemCommentViewModel>().Any(c => c.AttentionRequired))
            {
                AttentionRequired = AttentionRequiredCssClass.Yes;
                return;
            }
            CommentRequiredValidationMessage = string.Empty;
        }
        
        AttentionRequired = string.IsNullOrEmpty(TimeOverlappingValidationMessage) 
                            && string.IsNullOrEmpty(ActivityValidationMessage)
            ? AttentionRequiredCssClass.No 
            : AttentionRequiredCssClass.Yes;
    }

    private string GetActivityValidationMessage()
    {
        var activityMsg = new StringBuilder();

        if (!Activity.IsActive) activityMsg.AppendLine("Activity is not active.");
        if (!Activity.ParentProjectSummary.IsActive) activityMsg.AppendLine("Project is not active.");
        if (!Activity.ParentProjectSummary.CustomerIsActive) activityMsg.AppendLine("Customer is not active.");
        if (Activity.ActivityName == "TBD") activityMsg.AppendLine("Activity is To Be Determined.");
        if (Activity.ParentProjectSummary.ProjectName == "TBD") activityMsg.AppendLine("Project is To Be Determined.");

        return activityMsg.ToString();
    }

    private AttentionRequiredCssClass AttentionRequired { get; set; } = AttentionRequiredCssClass.No;
    private string CommentRequiredValidationMessage { get; set; } = string.Empty;
    private string TimeOverlappingValidationMessage { get; set; } = string.Empty;
    private string ActivityValidationMessage { get; set; } = string.Empty;

    private List<CommentViewModel> BuildComments()
    {
        var comments = BuildWorkItemComments()
            .Concat(BuildTextComments())
            .ToList();

        foreach (var comment in comments)
        {
            comment.HasChanged += OnCommentHasChanged;
        }

        return comments;
    }

    private List<WorkItemCommentViewModel> BuildWorkItemComments()
    {
        return TimeEntries
            .SelectWhereHasValue(entry => entry.Task)
            .Distinct()
            .Select(workItem => WorkItemCommentViewModel
                .FromExisting(
                    workItem ?? throw new InvalidOperationException()
                    , TimeEntries
                    , Period
                ).With(x => x.WorkItemActivatedAsync += OnWorkItemActivatedAsync))
            .ToList();
    }

    private List<CommentViewModel> BuildTextComments()
    {
        var timeEntryComments = TimeEntries.ToDictionary(t => t, _ => new List<(CommentType, string)>());

        foreach (var entry in TimeEntries)
        {
            timeEntryComments[entry].AddRange(SplitComment(CommentType.Other, entry));
            timeEntryComments[entry].AddRange(SplitComment(CommentType.Accomplishment, entry));
            timeEntryComments[entry].AddRange(SplitComment(CommentType.Impediment, entry));
            timeEntryComments[entry].AddRange(SplitComment(CommentType.Learning, entry));
        }

        return timeEntryComments
            .SelectMany(kvp => kvp.Value.Select(x =>
            {
                var (type, comment) = x;
                return new { TimeEntry = kvp.Key, Type = type, Text = comment };
            }))
            .GroupBy(
                map => new { map.Type, map.Text }
                , map => map.TimeEntry
                , (key, g) => CommentViewModel.FromExisting(key.Type, key.Text, TimeEntries, g))
            .ToList();
    }

    private static IEnumerable<(CommentType, string)> SplitComment(CommentType type, TimeEntry timeEntry)
    {
        var value = type.GetComment(timeEntry);

        if (value == null)
        {
            return [];
        }

        return value.Split('\n')
            .SelectWhereHasValue(x => string.IsNullOrWhiteSpace(x) ? null : x.Trim())
            .Select(x => (type, x));
    }

    #region Save/Close Dialog

    [Parameter] public EventCallback OnOpening { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }

    public VisibleCssClass DialogVisible { get; set; } = VisibleCssClass.Hidden;

    private async Task ShowDialogAsync()
    {
        await OnOpening.InvokeAsync();
        DialogVisible = VisibleCssClass.Visible;
    }

    private async Task CloseClickAsync()
    {
        //On cancel - reset the ViewModel
        Comments = BuildComments();
        Validate();

        //Close the dialog
        DialogVisible = VisibleCssClass.Hidden;
        await OnClosed.InvokeAsync();
    }

    private async Task SaveClickAsync()
    {
        try
        {
            await SaveAzureDevOpsTaskAsync();
            await SaveKimaiTimeEntriesAsync();
        }
        catch (Exception ex)
        {
            AlertService.BroadcastAlert(ex);
            return;
        }

        DialogVisible = VisibleCssClass.Hidden;
        await OnSaved.InvokeAsync();
        await OnClosed.InvokeAsync();
    }

    private async Task SaveAzureDevOpsTaskAsync()
    {
        foreach (var comment in Comments.OfType<WorkItemCommentViewModel>().Where(x => x.WorkItem != null))
        {
            var workItem = comment.WorkItem ?? throw new InvalidOperationException();
            if (workItem.AssignedTo != Person.Me)
            {
                return;  //Nothing to save.  Can't save other person's work item.
            }

            var remainingTime = comment.UnexportedTime + comment.SelectedTime + TimeSpan.FromHours(comment.TimeRemainingInput);

            await WorkItemUpdateService.UpdateTaskAsync(workItem, comment.State, remainingTime);
        }
    }

    private async Task SaveKimaiTimeEntriesAsync()
    {
        var newDescriptionMap = TimeEntries.ToDictionary(entry => entry.Id, GetTimeEntryDescription);

        await StandUpService.UpdateTimeEntryDescriptionAsync(Project.ParentDay, newDescriptionMap);
    }

    private string GetTimeEntryDescription(TimeEntry entry)
    {
        var comments = Comments
            .Where(comment => comment.IsActive[entry])
            .OrderBy(comment => comment.Type)
            .SelectWhereHasValue(comment => comment.KimaiDescription?.Trim())
            .Distinct();
        return string.Join("\r\n", comments);
    }

    #endregion Save/Close Dialog

    #region Add Comment

    private void AddComment(CommentType type)
    {
        var comment = type == CommentType.WorkItem
            ? WorkItemCommentViewModel.FromNew(TimeEntries, Period).With(x => x.WorkItemActivatedAsync += OnWorkItemActivatedAsync)
            : CommentViewModel.FromNew(type, TimeEntries);

        comment.HasChanged += OnCommentHasChanged;

        Comments.Add(comment);
        FocusRequest = comment;
    }

    private void OnCommentHasChanged(object? sender, EventArgs e)
    {
        Validate();
    }


    private async Task OnWorkItemActivatedAsync(object? sender, EventArgs e)
    {
        var workItem = sender as WorkItemCommentViewModel 
                       ?? throw new ArgumentException($"value should be a {nameof(WorkItemCommentViewModel)}", nameof(sender));
     
        var otherWorkItems = Comments
            .OfType<WorkItemCommentViewModel>()
            .Where(vm => vm != workItem);

        foreach (var other in otherWorkItems)
        {
            foreach (var entry in TimeEntries)
            {
                if (workItem.IsActive[entry] && other.IsActive[entry])
                {
                    await other.ToggleActiveAsync(entry);
                }
            }
        }
        StateHasChanged();
    }

    private CommentViewModel? FocusRequest { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (FocusRequest != null)
        {
            await FocusRequest.TextBox.FocusAsync();
            FocusRequest = null;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    #endregion

    private IEnumerable<CommentType> AllCommentTypes()
    {
        var disabledTypes = new List<CommentType>();

        var azureDevOpsEnabled = ConnectionSettingsStore.GetAzureDevOpsSettings().Enabled;
        if (!azureDevOpsEnabled)
        {
            disabledTypes.Add(CommentType.WorkItem);
        }

        return CommentType.All().Except(disabledTypes);
    }
}

internal class AttentionRequiredCssClass : CssClass
{
    private AttentionRequiredCssClass(string className) : base(className)
    {
    }

    public static readonly AttentionRequiredCssClass Yes = new("attention-required");
    public static readonly AttentionRequiredCssClass No = new(string.Empty);
}