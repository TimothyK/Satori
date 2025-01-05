using Satori.AppServices.ViewModels.DailyStandUps;

namespace Satori.Pages.StandUp.Components.ViewModels.Models;

public class CommentType : IComparable<CommentType>
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

    public static readonly CommentType WorkItem = new();
    public static readonly CommentType Other = new();
    public static readonly CommentType Accomplishment = new();
    public static readonly CommentType Impediment = new();
    public static readonly CommentType Learning = new();

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
        { Impediment, "Blockers, either needing help or speed bumps already resolved" },
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

    
    #region Cast to/from Underlying Type

    private static readonly Dictionary<CommentType, int> UnderlyingMap = new()
    {
        {WorkItem, 0},
        {Other, 1},
        {Accomplishment, 2},
        {Impediment, 3},
        {Learning, 4}
    };

    public static implicit operator int(CommentType value) => UnderlyingMap[value];
    public static explicit operator CommentType(int value)
    {
        var result = All().FirstOrDefault(x => (int) x == value);
        if (result != null) return result;

        throw new InvalidCastException($"The value {value} is not a valid {nameof(CommentType)}");
    }

    #endregion

    #region IComparable

    public int CompareTo(CommentType? other)
    {
        if (other == null)
        {
            return 1;
        }

        var results = new[]
        {
            ((int) this).CompareTo((int) other)
        };
        return results
            .SkipWhile(diff => diff == 0)
            .FirstOrDefault();
    }

    public static bool operator <(CommentType lhs, CommentType rhs) => lhs.CompareTo(rhs) < 0;
    public static bool operator <=(CommentType lhs, CommentType rhs) => lhs.CompareTo(rhs) <= 0;
    public static bool operator >(CommentType lhs, CommentType rhs) => lhs.CompareTo(rhs) > 0;
    public static bool operator >=(CommentType lhs, CommentType rhs) => lhs.CompareTo(rhs) >= 0;

    #endregion
}