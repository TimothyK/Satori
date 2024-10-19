namespace Satori.AppServices.ViewModels.WorkItems;

/// <summary>
/// State of the work item.
/// </summary>
/// <remarks>
/// <para>
/// Currently only the Scrum states are supported.  It is assumed that Azure DevOps is set up with Scrum.
/// https://learn.microsoft.com/en-us/azure/devops/boards/work-items/workflow-and-state-categories?view=azure-devops&tabs=cmmi-process
/// </para>
/// </remarks>
public class ScrumState : IComparable<ScrumState>
{
    private ScrumState()
    {
        Members.Add(this);
    }

    #region All

    private static readonly List<ScrumState> Members = [];
    public static IEnumerable<ScrumState> All() => Members;

    #endregion

    #region Members

    /// <summary>
    /// New item on the board
    /// </summary>
    /// <remarks>
    /// <para>
    /// Support for all WorkItemTypes except <see cref="WorkItemType.Task"/>.
    /// </para>
    /// </remarks>
    public static readonly ScrumState New = new();

    /// <summary>
    /// Task has not started
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only supported for <see cref="WorkItemType.Task"/>.
    /// </para>
    /// </remarks>
    public static readonly ScrumState ToDo = new();

    /// <summary>
    /// Task has not started
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only supported for <see cref="WorkItemType.Impediment"/>.
    /// </para>
    /// </remarks>
    public static readonly ScrumState Open = new();


    /// <summary>
    /// Approved by the Product Owner
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported for board types: <see cref="WorkItemType.ProductBacklogItem"/> and Büg
    /// </para>
    /// </remarks>
    public static readonly ScrumState Approved = new();
    /// <summary>
    /// Committed to by the scrum team
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported for board types: <see cref="WorkItemType.ProductBacklogItem"/> and Büg
    /// </para>
    /// </remarks>
    public static readonly ScrumState Committed = new();
    /// <summary>
    /// Committed to by the scrum team
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported for non board types: <see cref="WorkItemType.Epic"/>, <see cref="WorkItemType.Feature"/>, <see cref="WorkItemType.Task"/>
    /// </para>
    /// </remarks>
    public static readonly ScrumState InProgress = new();
    /// <summary>
    /// Done-done
    /// </summary>
    public static readonly ScrumState Done = new();

    /// <summary>
    /// Impediment is resolved
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only supported for <see cref="WorkItemType.Impediment"/>.
    /// </para>
    /// </remarks>
    public static readonly ScrumState Closed = new();

    /// <summary>
    /// Cancelled, won't do
    /// </summary>
    public static readonly ScrumState Removed = new();

    /// <summary>
    /// State not yet supported
    /// </summary>
    /// <remarks>
    /// <para>
    /// e.g. Test Case work item type is not currently supported in Satori.  One of its possible states is "Design", which is also not supported.
    /// </para>
    /// </remarks>
    public static readonly ScrumState Unknown = new();

    #endregion

    #region To/From String

    private static readonly Dictionary<ScrumState, string> ToStringMap = new()
    {
        {New, nameof(New)},
        {ToDo, nameof(ToDo)},
        {Open, nameof(Open)},
        {Approved, nameof(Approved)},
        {Committed, nameof(Committed)},
        {InProgress, nameof(InProgress)},
        {Done, nameof(Done)},
        {Closed, nameof(Closed)},
        {Removed, nameof(Removed)},
        {Unknown, nameof(Unknown)},
    };

    public override string ToString() => ToStringMap[this];
    public static ScrumState FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToString() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(ScrumState)}");
    }

    #endregion

    #region ApiValue

    private static readonly Dictionary<ScrumState, string> ApiValueMap = new()
    {
        {New, "New"},
        {ToDo, "To Do"},
        {Open, "Open"},
        {Approved, "Approved"},
        {Committed, "Committed"},
        {InProgress, "In Progress"},
        {Done, "Done"},
        {Closed, "Closed"},
        {Removed, "Removed"},
        {Unknown, "garbage value?!"},
    };

    public string ToApiValue() => ApiValueMap[this];
    public static ScrumState FromApiValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToApiValue() == value);
        return result ?? Unknown;
    }

    #endregion

    #region IComparable

    private static readonly Dictionary<ScrumState, int> OrdinalMap = new()
    {
        {New, 1},
        {ToDo, 2},
        {Open, 3},
        {Approved, 10},
        {Committed, 11},
        {InProgress, 12},
        {Done, 20},
        {Closed, 21},
        {Removed, 30},
        {Unknown, 9999}
    };

    private int OrdinalValue => OrdinalMap[this];

    public int CompareTo(ScrumState? other)
    {
        var results = new[]
        {
            OrdinalValue.CompareTo(other?.OrdinalValue ?? int.MinValue)
        };
        return results
            .SkipWhile(diff => diff == 0)
            .FirstOrDefault();
    }

    public static bool operator <(ScrumState lhs, ScrumState rhs) => lhs.CompareTo(rhs) < 0;
    public static bool operator <=(ScrumState lhs, ScrumState rhs) => lhs.CompareTo(rhs) <= 0;
    public static bool operator >(ScrumState lhs, ScrumState rhs) => lhs.CompareTo(rhs) > 0;
    public static bool operator >=(ScrumState lhs, ScrumState rhs) => lhs.CompareTo(rhs) >= 0;

    #endregion
}