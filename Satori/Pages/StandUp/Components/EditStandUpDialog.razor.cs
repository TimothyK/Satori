using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Pages.StandUp.Components.ViewModels;
using Satori.Pages.StandUp.Components.ViewModels.Models;

namespace Satori.Pages.StandUp.Components;

public partial class EditStandUpDialog
{
    private ActivitySummary Activity { get; set; } = null!;
    private ProjectSummary Project { get; set; } = null!;

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

        base.OnParametersSet();
    }

    private List<CommentViewModel> BuildComments()
    {
        return BuildWorkItemComments()
            .Concat(BuildTextComments())
            .ToList();
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
                ).With(x => x.WorkItemActivated += OnWorkItemActivated))
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

    private void AddComment(CommentType type)
    {
        var comment = type == CommentType.WorkItem
            ? WorkItemCommentViewModel.FromNew(TimeEntries).With(x => x.WorkItemActivated += OnWorkItemActivated)
            : CommentViewModel.FromNew(type, TimeEntries);
        if (comment is WorkItemCommentViewModel)
        {
            OnWorkItemActivated(comment, EventArgs.Empty);
        }

        Comments.Add(comment);
        FocusRequest = comment;
    }

    private void OnWorkItemActivated(object? sender, EventArgs e)
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
                    other.ToggleActive(entry);
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
}