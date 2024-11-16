using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Utilities;

namespace Satori.Pages
{
    public partial class EditStandUpDialog
    {
        private ActivitySummary Activity { get; set; } = null!;
        private ProjectSummary Project { get; set; } = null!;

        private List<CommentViewModel> Comments { get; set; } = null!;

        protected override void OnParametersSet()
        {
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
                    map => new{map.Type, map.Text}
                    , map => map.TimeEntry
                    , (key, g) => new CommentViewModel(key.Type, key.Text, TimeEntries, g))
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
            var comment = new CommentViewModel(type, string.Empty, TimeEntries, TimeEntries.Reverse().Take(1));
            FocusRequest = comment;
            Comments.Add(comment);
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

    internal class CommentViewModel
    {
        public CommentViewModel(CommentType type, string text, IEnumerable<TimeEntry> allTimeEntries, IEnumerable<TimeEntry> activeTimeEntries)
        {
            Type = type;
            Text = text;

            IsActive = allTimeEntries.ToDictionary(t => t, t => (SelectionActiveCssClass) activeTimeEntries.Contains(t));
        }

        public CommentType Type { get; set; }
        public string Text { get; set; }
        
        public Dictionary<TimeEntry, SelectionActiveCssClass> IsActive { get; private set; }

        public void ToggleActive(TimeEntry timeEntry)
        {
            IsActive[timeEntry] = IsActive[timeEntry].Not;

            MarkAsDeleted();
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

    public class SelectionActiveCssClass : CssClass
    {
        private SelectionActiveCssClass(string className) : base(className) { }

        public static SelectionActiveCssClass Activated { get; } = new("selection-activated");
        public static SelectionActiveCssClass Deactivated { get; } = new("selection-deactivated");
        public static SelectionActiveCssClass ToBeDeleted { get; } = new("selection-delete");

        public SelectionActiveCssClass Not => this == Activated ? Deactivated : Activated;

        public static implicit operator bool(SelectionActiveCssClass cssClass) => cssClass == Activated;
        public static implicit operator SelectionActiveCssClass(bool value) => value ? Activated : Deactivated;
    }

    internal class CommentType
    {
        private CommentType()
        {
            _all.Add(this);
        }

        #region All

        private static readonly List<CommentType> _all = [];
        public static IEnumerable<CommentType> All() => _all;

        #endregion

        #region Members

        public static readonly CommentType Other = new();
        public static readonly CommentType Accomplishment = new();
        public static readonly CommentType Impediment = new();
        public static readonly CommentType Learning = new();
        public static readonly CommentType WorkItem = new();

        #endregion

        #region To/From String

        private static readonly Dictionary<CommentType, string> ToStringMap = new()
        {
                {Other, nameof(Other)},
                {Accomplishment, nameof(Accomplishment)},
                {Impediment, nameof(Impediment)},
                {Learning, nameof(Learning)},
                {WorkItem, nameof(WorkItem)}
            };

        public override string ToString() => ToStringMap[this];

        #endregion

        #region Icon

        private static readonly Dictionary<CommentType, string> IconMap = new()
        {
            {Other, "📝"},
            {Accomplishment, "🏆"},
            {Impediment, "🧱"},
            {Learning, "🧠"},
            {WorkItem, "#"}
        };

        public string Icon => IconMap[this];

        #endregion Icon

        #region PlaceholderText

        private static readonly Dictionary<CommentType, string> PlaceholderTextMap = new()
        {
            {Other, "Describe the subtask worked on"},
            {Accomplishment, "Achievements, decisions made, documentation added"},
            {Impediment, "Blockers, either needing help or speed bumps"},
            {Learning, "Today I Learned.  Like accomplishments but you're smarter.  Share with the team to make them smarter too or help identify training gaps"},
            {WorkItem, "12345"}
        };

        public string PlaceholderText => PlaceholderTextMap[this];

        #endregion PlaceholderText

        #region Add Button Label

        private static readonly Dictionary<CommentType, string> AddButtonLabelMap = new()
        {
            {Other, "General Comment"},
            {Accomplishment, "Achievement"},
            {Impediment, "Impediment"},
            {Learning, "Today I Learned"},
            {WorkItem, "Azure DevOps Work Item"}
        };

        public string AddButtonLabel => AddButtonLabelMap[this];

        #endregion Add Button Label

        #region GetComment

        private static readonly Dictionary<CommentType, Func<TimeEntry, string?>> GetCommentMap = new()
        {
            {Other, entry => entry.OtherComments},
            {Accomplishment, entry => entry.Accomplishments},
            {Impediment, entry => entry.Impediments},
            {Learning, entry => entry.Learnings},
            {WorkItem, entry => entry.Task == null ? null 
                : entry.Task.Parent == null ? $"D#{entry.Task.Id} {entry.Task.Title}"
                : $"D#{entry.Task.Parent.Id} {entry.Task.Parent.Title} » D#{entry.Task.Id} {entry.Task.Title}"
            }
        };

        public string? GetComment(TimeEntry entry) => GetCommentMap[this](entry);

        #endregion PlaceholderText
    }



}
