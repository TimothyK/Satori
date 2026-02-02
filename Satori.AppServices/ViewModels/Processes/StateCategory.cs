namespace Satori.AppServices.ViewModels.Processes;

public class StateCategory : IComparable<StateCategory>
{
    private StateCategory()
    {
        _all.Add(this);
    }

    #region All

    // ReSharper disable once InconsistentNaming
    private static readonly List<StateCategory> _all = [];
    public static IEnumerable<StateCategory> All() => _all;

    #endregion

    #region Members

    /// <summary>
    /// On the backlog
    /// </summary>
    public static readonly StateCategory Proposed = new();

    /// <summary>
    /// Actively being work on
    /// </summary>
    public static readonly StateCategory InProgress = new();

    /// <summary>
    /// Done, but not done-done
    /// </summary>
    public static readonly StateCategory Resolved = new();

    /// <summary>
    /// Done-done
    /// </summary>
    public static readonly StateCategory Completed = new();

    /// <summary>
    /// Soft deleted
    /// </summary>
    public static readonly StateCategory Removed = new();

    #endregion

    #region To/From String

    private static readonly Dictionary<StateCategory, string> ToStringMap = new()
    {
        {Proposed, nameof(Proposed)},
        {InProgress, nameof(InProgress)},
        {Resolved, nameof(Resolved)},
        {Completed, nameof(Completed)},
        {Removed, nameof(Removed)}
    };

    public override string ToString() => ToStringMap[this];
    public static StateCategory FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return All().FirstOrDefault(x => x.ToString() == value) 
               ?? throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(StateCategory)}");
    }

    #endregion

    #region DbValue

    private static readonly Dictionary<StateCategory, string> ApiValueMap = new()
    {
        {Proposed, "Proposed"},
        {InProgress, "In Progress"},
        {Resolved, "Resolved"},
        {Completed, "Completed"},
        {Removed, "Removed"}
    };

    public string ToDbValue() => ApiValueMap[this];
    public static StateCategory FromApiValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return All().FirstOrDefault(x => x.ToDbValue() == value) 
               ?? throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(StateCategory)}");
    }

    #endregion

    #region IComparable

    public int CompareTo(StateCategory? other)
    {
        var results = new[]
        {
            string.Compare(ToString(), other?.ToString(), StringComparison.Ordinal)
        };
        return results
            .SkipWhile(diff => diff == 0)
            .FirstOrDefault();
    }

    public static bool operator <(StateCategory lhs, StateCategory rhs) => lhs.CompareTo(rhs) < 0;
    public static bool operator <=(StateCategory lhs, StateCategory rhs) => lhs.CompareTo(rhs) <= 0;
    public static bool operator >(StateCategory lhs, StateCategory rhs) => lhs.CompareTo(rhs) > 0;
    public static bool operator >=(StateCategory lhs, StateCategory rhs) => lhs.CompareTo(rhs) >= 0;

    #endregion

}