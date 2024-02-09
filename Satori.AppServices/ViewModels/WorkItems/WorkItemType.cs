namespace Satori.AppServices.ViewModels.WorkItems;

public class WorkItemType
{
    private WorkItemType(string apiValue, string? cssClassSuffix = null)
    {
        _apiValue = apiValue;
        CssClassSuffix = cssClassSuffix ?? _apiValue.ToLower();

        _all.Add(this);
    }

    #region All

    private static readonly List<WorkItemType> _all = new();
    public static IEnumerable<WorkItemType> All() => _all;

    #endregion

    #region Members

    public static readonly WorkItemType ProductBacklogItem = new("Product Backlog Item", "pbi");
    public static readonly WorkItemType Bug = new("Bug");
    public static readonly WorkItemType Task = new("Task");
    public static readonly WorkItemType Feature = new("Feature");
    public static readonly WorkItemType Epic = new("Epic");
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

    private string CssClassSuffix { get; }
    public string CssClass => "work-item-" + CssClassSuffix;
}


