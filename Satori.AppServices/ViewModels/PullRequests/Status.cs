using CodeMonkeyProjectiles.Linq;

namespace Satori.AppServices.ViewModels.PullRequests;

public class Status : IComparable<Status>
{
    private Status()
    {
        Members.Add(this);
    }

    #region All

    private static readonly List<Status> Members = new List<Status>();
    public static IEnumerable<Status> All() => Members;

    #endregion

    #region Members

    public static readonly Status Draft = new();
    public static readonly Status Open = new();
    public static readonly Status Complete = new();
    public static readonly Status Abandoned = new();

    #endregion

    #region To/From String

    private static readonly Dictionary<Status, string> ToStringMap = new Dictionary<Status, string>
    {
        {Draft, nameof(Draft)},
        {Open, nameof(Open)},
        {Complete, nameof(Complete)},
        {Abandoned, nameof(Abandoned)}
    };

    public override string ToString() => ToStringMap[this];
    public static Status FromString(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        var result = All().FirstOrDefault(x => x.ToString() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(Status)}");
    }

    #endregion

    #region ApiValue

    private static readonly Dictionary<Status, string> ApiValueMap = new()
    {
        {Draft, "active"},
        {Open, "active"},
        {Complete, "completed"},
        {Abandoned, "abandoned"},
    };

    public string ToApiValue() => ApiValueMap[this];
    public static Status FromApiValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().Except(Draft.Yield()).FirstOrDefault(x => x.ToApiValue() == value);
        return result ?? Open;
    }

    #endregion

    #region Cast to/from Underlying Type

    private static readonly Dictionary<Status, int> UnderlyingMap = new Dictionary<Status, int>
    {
        {Draft, 0},
        {Open, 1},
        {Complete, 2},
        {Abandoned, 3}
    };

    public static implicit operator int(Status value) => UnderlyingMap[value];
    public static explicit operator Status(int value)
    {
        var result = All().FirstOrDefault(x => (int) x == value);
        if (result != null) return result;

        throw new InvalidCastException($"The value {value} is not a valid {nameof(Status)}");
    }

    #endregion

    #region IComparable

    public int CompareTo(Status other)
    {
        var results = new[]
        {
            ((int) this).CompareTo((int) other)
        };
        return results
            .SkipWhile(diff => diff == 0)
            .FirstOrDefault();
    }

    public static bool operator <(Status lhs, Status rhs) => lhs.CompareTo(rhs) < 0;
    public static bool operator <=(Status lhs, Status rhs) => lhs.CompareTo(rhs) <= 0;
    public static bool operator >(Status lhs, Status rhs) => lhs.CompareTo(rhs) > 0;
    public static bool operator >=(Status lhs, Status rhs) => lhs.CompareTo(rhs) >= 0;

    #endregion

}