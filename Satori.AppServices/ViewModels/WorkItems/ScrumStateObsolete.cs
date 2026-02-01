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
public class ScrumStateObsolete : IComparable<ScrumStateObsolete>
{
    private ScrumStateObsolete()
    {
        Members.Add(this);
    }

    #region All

    private static readonly List<ScrumStateObsolete> Members = [];
    public static IEnumerable<ScrumStateObsolete> All() => Members;

    #endregion

    #region Members

    /// <summary>
    /// New item on the board
    /// </summary>
    /// <remarks>
    /// <para>
    /// Support for all WorkItemTypes except <see cref="WorkItemTypeObsolete.Task"/>.
    /// </para>
    /// </remarks>
    public static readonly ScrumStateObsolete New = new();

    /// <summary>
    /// Task has not started
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only supported for <see cref="WorkItemTypeObsolete.Task"/>.
    /// </para>
    /// </remarks>
    public static readonly ScrumStateObsolete ToDo = new();

    /// <summary>
    /// Task has not started
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only supported for <see cref="WorkItemTypeObsolete.Impediment"/>.
    /// </para>
    /// </remarks>
    public static readonly ScrumStateObsolete Open = new();


    /// <summary>
    /// Approved by the Product Owner
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported for board types: <see cref="WorkItemTypeObsolete.ProductBacklogItem"/> and Büg
    /// </para>
    /// </remarks>
    public static readonly ScrumStateObsolete Approved = new();
    /// <summary>
    /// Committed to by the scrum team
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported for board types: <see cref="WorkItemTypeObsolete.ProductBacklogItem"/> and Büg
    /// </para>
    /// </remarks>
    public static readonly ScrumStateObsolete Committed = new();
    /// <summary>
    /// A task that is in progress on a Scrum team board
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported for non board types: <see cref="WorkItemTypeObsolete.Epic"/>, <see cref="WorkItemTypeObsolete.Feature"/>, <see cref="WorkItemTypeObsolete.Task"/>
    /// </para>
    /// </remarks>
    public static readonly ScrumStateObsolete InProgress = new();
    /// <summary>
    /// A task that is actively being worked on by an Agile team
    /// </summary>
    public static readonly ScrumStateObsolete Active = new();

    /// <summary>
    /// Done-done
    /// </summary>
    public static readonly ScrumStateObsolete Done = new();

    /// <summary>
    /// Impediment is resolved
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only supported for <see cref="WorkItemTypeObsolete.Impediment"/>.
    /// </para>
    /// </remarks>
    public static readonly ScrumStateObsolete Closed = new();

    /// <summary>
    /// Cancelled, won't do
    /// </summary>
    public static readonly ScrumStateObsolete Removed = new();

    /// <summary>
    /// State not yet supported
    /// </summary>
    /// <remarks>
    /// <para>
    /// e.g. Test Case work item type is not currently supported in Satori.  One of its possible states is "Design", which is also not supported.
    /// </para>
    /// </remarks>
    public static readonly ScrumStateObsolete Unknown = new();

    #endregion

    #region To/From String

    private static readonly Dictionary<ScrumStateObsolete, string> ToStringMap = new()
    {
        {New, nameof(New)},
        {ToDo, nameof(ToDo)},
        {Open, nameof(Open)},
        {Approved, nameof(Approved)},
        {Committed, nameof(Committed)},
        {InProgress, nameof(InProgress)},
        {Active, nameof(Active)},
        {Done, nameof(Done)},
        {Closed, nameof(Closed)},
        {Removed, nameof(Removed)},
        {Unknown, nameof(Unknown)},
    };

    public override string ToString() => ToStringMap[this];
    public static ScrumStateObsolete FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToString() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(ScrumStateObsolete)}");
    }

    #endregion

    #region ApiValue

    private static readonly Dictionary<ScrumStateObsolete, string> ApiValueMap = new()
    {
        {New, "New"},
        {ToDo, "To Do"},
        {Open, "Open"},
        {Approved, "Approved"},
        {Committed, "Committed"},
        {InProgress, "In Progress"},
        {Active, "Active"},
        {Done, "Done"},
        {Closed, "Closed"},
        {Removed, "Removed"},
        {Unknown, "garbage value?!"},
    };

    public string ToApiValue() => ApiValueMap[this];
    public static ScrumStateObsolete FromApiValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToApiValue() == value);
        return result ?? Unknown;
    }

    #endregion

    #region Category

    private static readonly Dictionary<ScrumStateObsolete, StateCategory> CategoryMap = new()
    {
        {New, StateCategory.Proposed},
        {ToDo, StateCategory.Proposed},
        {Open, StateCategory.Proposed},
        {Approved, StateCategory.Proposed},
        {Committed, StateCategory.InProgress},
        {InProgress, StateCategory.InProgress},
        {Active, StateCategory.InProgress},
        {Done, StateCategory.Completed},
        {Closed, StateCategory.Completed},
        {Removed, StateCategory.Removed},
        {Unknown, StateCategory.Removed},
    };

    public StateCategory Category => CategoryMap[this];

    #endregion

    #region IComparable

    private static readonly Dictionary<ScrumStateObsolete, int> OrdinalMap = new()
    {
        {New, 1},
        {ToDo, 2},
        {Open, 3},
        {Approved, 10},
        {Committed, 11},
        {InProgress, 12},
        {Active, 13},
        {Done, 20},
        {Closed, 21},
        {Removed, 30},
        {Unknown, 9999}
    };

    private int OrdinalValue => OrdinalMap[this];

    public int CompareTo(ScrumStateObsolete? other)
    {
        var results = new[]
        {
            OrdinalValue.CompareTo(other?.OrdinalValue ?? int.MinValue)
        };
        return results
            .SkipWhile(diff => diff == 0)
            .FirstOrDefault();
    }

    public static bool operator <(ScrumStateObsolete lhs, ScrumStateObsolete rhs) => lhs.CompareTo(rhs) < 0;
    public static bool operator <=(ScrumStateObsolete lhs, ScrumStateObsolete rhs) => lhs.CompareTo(rhs) <= 0;
    public static bool operator >(ScrumStateObsolete lhs, ScrumStateObsolete rhs) => lhs.CompareTo(rhs) > 0;
    public static bool operator >=(ScrumStateObsolete lhs, ScrumStateObsolete rhs) => lhs.CompareTo(rhs) >= 0;

    #endregion
}