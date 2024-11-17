using Satori.AppServices.ViewModels.DailyStandUps;

namespace Satori.Pages.StandUp.Components.ViewModels.Models;

public class CommentType
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
        { Other, nameof(Other) },
        { Accomplishment, nameof(Accomplishment) },
        { Impediment, nameof(Impediment) },
        { Learning, nameof(Learning) },
        { WorkItem, nameof(WorkItem) }
    };

    public override string ToString() => ToStringMap[this];

    #endregion

    #region Icon

    private static readonly Dictionary<CommentType, string> IconMap = new()
    {
        { Other, "📝" },
        { Accomplishment, "🏆" },
        { Impediment, "🧱" },
        { Learning, "🧠" },
        { WorkItem, "#" }
    };

    public string Icon => IconMap[this];

    #endregion Icon

    #region PlaceholderText

    private static readonly Dictionary<CommentType, string> PlaceholderTextMap = new()
    {
        { Other, "Describe the subtask worked on" },
        { Accomplishment, "Achievements, decisions made, documentation added" },
        { Impediment, "Blockers, either needing help or speed bumps" },
        {
            Learning,
            "Today I Learned.  Like accomplishments but you're smarter.  Share with the team to make them smarter too or help identify training gaps"
        },
        { WorkItem, "12345" }
    };

    public string PlaceholderText => PlaceholderTextMap[this];

    #endregion PlaceholderText

    #region Add Button Label

    private static readonly Dictionary<CommentType, string> AddButtonLabelMap = new()
    {
        { Other, "General Comment" },
        { Accomplishment, "Achievement" },
        { Impediment, "Impediment" },
        { Learning, "Today I Learned" },
        { WorkItem, "Azure DevOps Work Item" }
    };

    public string AddButtonLabel => AddButtonLabelMap[this];

    #endregion Add Button Label

    #region GetComment

    private static readonly Dictionary<CommentType, Func<TimeEntry, string?>> GetCommentMap = new()
    {
        { Other, entry => entry.OtherComments },
        { Accomplishment, entry => entry.Accomplishments },
        { Impediment, entry => entry.Impediments },
        { Learning, entry => entry.Learnings },
        {
            WorkItem, entry => entry.Task == null ? null
                : entry.Task.Parent == null ? $"D#{entry.Task.Id} {entry.Task.Title}"
                : $"D#{entry.Task.Parent.Id} {entry.Task.Parent.Title} » D#{entry.Task.Id} {entry.Task.Title}"
        }
    };

    public string? GetComment(TimeEntry? entry) => entry == null ? null : GetCommentMap[this](entry);

    #endregion PlaceholderText
}