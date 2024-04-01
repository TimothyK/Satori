using System.Collections.Immutable;

namespace Satori.AppServices.ViewModels.WorkItems;

public class TriageState
{
    private TriageState()
    {
        Members.Add(this);
    }

    #region All

    private static readonly List<TriageState> Members = [];
    public static IEnumerable<TriageState> All() => Members.ToImmutableArray();

    #endregion

    #region Members

    public static readonly TriageState Pending = new();
    public static readonly TriageState MoreInfo = new();
    public static readonly TriageState InfoReceived = new();
    public static readonly TriageState Triaged = new();

    #endregion

    #region To/From String

    private static readonly Dictionary<TriageState, string> ToStringMap = new()
    {
        {Pending, nameof(Pending)},
        {MoreInfo, nameof(MoreInfo)},
        {InfoReceived, nameof(InfoReceived)},
        {Triaged, nameof(Triaged)}
    };

    public override string ToString() => ToStringMap[this];
    public static TriageState FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToString() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(TriageState)}");
    }

    #endregion

    #region DbValue

    private static readonly Dictionary<TriageState, string> ApiValueMap = new()
    {
        {Pending, "Pending"},
        {MoreInfo, "More Info"},
        {InfoReceived, "Info Received"},
        {Triaged, "Triaged"}
    };

    public string ToApiValue() => ApiValueMap[this];
    public static TriageState? FromApiValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var result = All().FirstOrDefault(x => x.ToApiValue() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(TriageState)}");
    }

    #endregion

}