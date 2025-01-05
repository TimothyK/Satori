using CodeMonkeyProjectiles.Linq;
using System.Collections.Immutable;

namespace Satori.AppServices.ViewModels.WorkItems;

public class WorkItemType
{
    private WorkItemType(string apiValue, string? cssClassSuffix = null)
    {
        _apiValue = apiValue;
        CssClassSuffix = cssClassSuffix ?? _apiValue.ToLower();

        Members.Add(this);
    }

    #region All

    private static readonly List<WorkItemType> Members = [];
    public static IEnumerable<WorkItemType> All() => Members;

    #endregion

    #region Members

    public static readonly WorkItemType ProductBacklogItem = new("Product Backlog Item", "pbi");
    public static readonly WorkItemType Bug = new("Bug");
    public static readonly WorkItemType Task = new("Task");
    public static readonly WorkItemType Feature = new("Feature");
    public static readonly WorkItemType Epic = new("Epic");
    public static readonly WorkItemType Impediment = new("Impediment");
    public static readonly WorkItemType Unknown = new("Work Item", "unknown");

    #endregion

    #region API Value

    public override string ToString() => _apiValue;

    private readonly string _apiValue;
    public string ToApiValue() => _apiValue;

    public static WorkItemType FromApiValue(string value)
    {
        return All().FirstOrDefault(x => x.ToApiValue() == value) ?? Unknown;
    }

    #endregion

    private static ImmutableArray<WorkItemType>? _boardTypes;

    /// <summary>
    /// Work item types that can be directly assigned a sprint board.
    /// <see cref="Task"/> can be assigned to a board, but only as a child of one of these work item types.
    /// </summary>
    public static ImmutableArray<WorkItemType> BoardTypes => 
        _boardTypes ??= [..All().Where(t => t.CanAssignToBoard)];

    public bool CanAssignToBoard => this.IsIn(ProductBacklogItem, Bug, Impediment);

    private string CssClassSuffix { get; }
    public string CssClass => "work-item-" + CssClassSuffix;
}


