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
public class ScrumState
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
    /// Cancelled, won't do
    /// </summary>
    public static readonly ScrumState Removed = new();

    #endregion

    #region To/From String

    private static readonly Dictionary<ScrumState, string> ToStringMap = new()
    {
        {New, nameof(New)},
        {ToDo, nameof(ToDo)},
        {Approved, nameof(Approved)},
        {Committed, nameof(Committed)},
        {InProgress, nameof(InProgress)},
        {Done, nameof(Done)},
        {Removed, nameof(Removed)}
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

    #region DbValue

    private static readonly Dictionary<ScrumState, string> ApiValueMap = new()
    {
        {New, "New"},
        {ToDo, "To Do"},
        {Approved, "Approved"},
        {Committed, "Committed"},
        {InProgress, "In Progress"},
        {Done, "Done"},
        {Removed, "Removed"}
    };

    public string ToApiValue() => ApiValueMap[this];
    public static ScrumState FromApiValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToApiValue() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(ScrumState)}");
    }

    #endregion



}