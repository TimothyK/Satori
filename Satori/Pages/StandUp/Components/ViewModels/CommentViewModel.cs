using Microsoft.AspNetCore.Components;
using Satori.AppServices.Services.CommentParsing;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Pages.StandUp.Components.ViewModels.Models;

namespace Satori.Pages.StandUp.Components.ViewModels;

public class CommentViewModel
{
    protected CommentViewModel(CommentType type, string? text, IEnumerable<TimeEntry> allTimeEntries,
        IEnumerable<TimeEntry> activeTimeEntries)
    {
        Type = type;
        Text = text;

        IsActive = allTimeEntries.ToDictionary(t => t, t => (SelectionActiveCssClass)activeTimeEntries.Contains(t));
    }

    public static CommentViewModel FromNew(CommentType type, TimeEntry[] timeEntries)
    {
        var comment = new CommentViewModel(type, null, timeEntries, timeEntries.Reverse().Take(1));
        return comment;
    }
    public static CommentViewModel FromExisting(CommentType type, string text, TimeEntry[] allTimeEntries, IEnumerable<TimeEntry> activeTimeEntries)
    {
        var comment = new CommentViewModel(type, text, allTimeEntries, activeTimeEntries);
        return comment;
    }

    public CommentType Type { get; set; }
    
    private string? _text;
    public string? Text
    {
        get => _text;
        set
        {
            _text = value;
            OnHasChanged();
        }
    }

    public virtual string? KimaiDescription => 
        Type == CommentType.Other ? Text?.Trim()
            : string.IsNullOrWhiteSpace(Text) ? null
            : $"{Type.Icon}{Text.Trim()}";

    public IEnumerable<TimeEntry> TimeEntries => IsActive.Keys;
    public Dictionary<TimeEntry, SelectionActiveCssClass> IsActive { get; }

    public event EventHandler? HasChanged;

    protected virtual void OnHasChanged()
    {
        HasChanged?.Invoke(this, EventArgs.Empty);
    }

    public virtual Task ToggleActiveAsync(TimeEntry timeEntry)
    {
        IsActive[timeEntry] = IsActive[timeEntry].Not;

        MarkAsDeleted();
        
        OnHasChanged();
        return Task.CompletedTask;
    }

    private void MarkAsDeleted()
    {
        if (IsActive.Values.Any(value => value == SelectionActiveCssClass.Activated))
        {
            foreach (var kvp in IsActive.Where(kvp => kvp.Value == SelectionActiveCssClass.ToBeDeleted))
            {
                IsActive[kvp.Key] = SelectionActiveCssClass.Deactivated;
            }
        }
        else
        {
            foreach (var kvp in IsActive)
            {
                IsActive[kvp.Key] = SelectionActiveCssClass.ToBeDeleted;
            }
        }
    }

    public ElementReference TextBox;
}