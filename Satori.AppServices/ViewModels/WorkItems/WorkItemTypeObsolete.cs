using CodeMonkeyProjectiles.Linq;
using System.Collections.Immutable;

namespace Satori.AppServices.ViewModels.WorkItems;

public class WorkItemTypeObsolete : IComparable<WorkItemTypeObsolete>
{
    private WorkItemTypeObsolete(string apiValue, string? cssClassSuffix = null)
    {
        _apiValue = apiValue;
        CssClassSuffix = cssClassSuffix ?? _apiValue.ToLower();

        Members.Add(this);
    }

    #region All

    private static readonly List<WorkItemTypeObsolete> Members = [];
    public static IEnumerable<WorkItemTypeObsolete> All() => Members;

    #endregion

    #region Members

    public static readonly WorkItemTypeObsolete ProductBacklogItem = new("Product Backlog Item", "pbi");
    public static readonly WorkItemTypeObsolete Bug = new("Bug");
    public static readonly WorkItemTypeObsolete Task = new("Task");
    public static readonly WorkItemTypeObsolete Feature = new("Feature");
    public static readonly WorkItemTypeObsolete Epic = new("Epic");
    public static readonly WorkItemTypeObsolete Impediment = new("Impediment");
    public static readonly WorkItemTypeObsolete UserStory = new("User Story", "user-story");
    public static readonly WorkItemTypeObsolete Unknown = new("Work Item", "unknown");

    #endregion

    #region API Value

    public override string ToString() => _apiValue;

    private readonly string _apiValue;
    public string ToApiValue() => _apiValue;

    public static WorkItemTypeObsolete FromApiValue(string value)
    {
        return All().FirstOrDefault(x => x.ToApiValue() == value) ?? Unknown;
    }

    #endregion

    private static ImmutableArray<WorkItemTypeObsolete>? _boardTypes;

    /// <summary>
    /// Work item types that can be directly assigned a sprint board.
    /// <see cref="Task"/> can be assigned to a board, but only as a child of one of these work item types.
    /// </summary>
    public static ImmutableArray<WorkItemTypeObsolete> BoardTypes => 
        _boardTypes ??= [..All().Where(t => t.CanAssignToBoard)];

    public bool CanAssignToBoard => this.IsIn(ProductBacklogItem, Bug, Impediment, UserStory);

    private string CssClassSuffix { get; }
    public string CssClass => "work-item-" + CssClassSuffix;

    #region IComparable

    private static readonly Dictionary<WorkItemTypeObsolete, int> OrdinalMap = new()
    {
        { Epic, 10 },
        { Feature, 20 },
        { ProductBacklogItem, 30 },
        { Bug, 31 },
        { Impediment, 32 },
        { UserStory, 33 },
        { Task, 100 },
        { Unknown, int.MaxValue }
    };
    private int OrdinalValue => OrdinalMap[this];

    public int CompareTo(WorkItemTypeObsolete? other)
    {
        var results = new[]
        {
            OrdinalValue.CompareTo(other?.OrdinalValue ?? int.MinValue)
        };
        return results
            .SkipWhile(diff => diff == 0)
            .FirstOrDefault();
    }

    public static bool operator <(WorkItemTypeObsolete lhs, WorkItemTypeObsolete rhs) => lhs.CompareTo(rhs) < 0;
    public static bool operator <=(WorkItemTypeObsolete lhs, WorkItemTypeObsolete rhs) => lhs.CompareTo(rhs) <= 0;
    public static bool operator >(WorkItemTypeObsolete lhs, WorkItemTypeObsolete rhs) => lhs.CompareTo(rhs) > 0;
    public static bool operator >=(WorkItemTypeObsolete lhs, WorkItemTypeObsolete rhs) => lhs.CompareTo(rhs) >= 0;

    #endregion IComparable
}


