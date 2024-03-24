namespace Satori.AppServices.ViewModels.WorkItems;

public class LinkType
{
    private LinkType(string name, string apiValue)
    {
        _name = name;
        _apiValue = apiValue;

        _all.Add(this);
    }

    #region All

    private static readonly List<LinkType> _all = [];
    public static IEnumerable<LinkType> All() => _all;

    #endregion

    #region Members

    public static readonly LinkType IsParentOf = new(nameof(IsParentOf), "IsParentOf");
    public static readonly LinkType IsChildOf = new(nameof(IsChildOf), "IsChildOf");
    public static readonly LinkType IsRelatedTo = new(nameof(IsRelatedTo), "IsRelatedTo");
    public static readonly LinkType IsSuccessorOf = new(nameof(IsSuccessorOf), "IsSuccessorOf");
    public static readonly LinkType IsPredecessorOf = new(nameof(IsPredecessorOf), "IsPredecessorOf");
    public static readonly LinkType Other = new(nameof(Other), "O");

    #endregion

    #region To/From String

    private readonly string _name;
    public override string ToString() => _name;
    public static LinkType FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToString() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(LinkType)}");
    }

    #endregion

    public LinkType ReverseLink
    {
        get
        {
            if (this == IsParentOf) return IsChildOf;
            if (this == IsChildOf) return IsParentOf;
            if (this == IsSuccessorOf) return IsPredecessorOf;
            if (this == IsPredecessorOf) return IsSuccessorOf;
            if (this == IsRelatedTo) return IsRelatedTo;
            //if (this == Other) return Other;

            throw new NotSupportedException("Unknown Enum");
        }
    }

    #region DbValue

    private readonly string _apiValue;
    public string ToApiValue() => _apiValue;
    public static LinkType FromApiValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToApiValue() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(LinkType)}");
    }

    #endregion

}