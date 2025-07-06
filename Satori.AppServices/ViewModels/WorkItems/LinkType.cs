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

    public static readonly LinkType IsParentOf = new(nameof(IsParentOf), "System.LinkTypes.Hierarchy-Forward");
    public static readonly LinkType IsChildOf = new(nameof(IsChildOf), "System.LinkTypes.Hierarchy-Reverse");

    public static readonly LinkType IsPredecessorOf = new(nameof(IsPredecessorOf), "System.LinkTypes.Dependency-Forward");
    public static readonly LinkType IsSuccessorOf = new(nameof(IsSuccessorOf), "System.LinkTypes.Dependency-Reverse");

    /// <summary>
    /// Newer work item is a duplicate of an older, pre-existing work item.
    /// </summary>
    public static readonly LinkType IsDuplicateOf = new(nameof(IsDuplicateOf), "System.LinkTypes.Duplicate-Forward");

    /// <summary>
    /// Older work item has a duplicate newer work item
    /// </summary>
    public static readonly LinkType HasDuplicate = new(nameof(HasDuplicate), "System.LinkTypes.Duplicate-Reverse");

    public static readonly LinkType IsRelatedTo = new(nameof(IsRelatedTo), "System.LinkTypes.Related");

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
            if (this == IsDuplicateOf) return HasDuplicate;
            if (this == HasDuplicate) return IsDuplicateOf;
            if (this == IsRelatedTo) return IsRelatedTo;

            throw new NotSupportedException("Unknown Enum");
        }
    }

    #region ApiValue

    private readonly string _apiValue;
    public string ToApiValue() => _apiValue;
    public static LinkType FromApiValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = All().FirstOrDefault(x => x.ToApiValue() == value);
        if (result != null) return result;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Invalid {nameof(LinkType)}");
    }

    /// <summary>
    /// Name of the Related work Item.  Used in the "attributes" of the relation json object
    /// </summary>
    public string RelatedWorkItemName
    {
        get
        {
            if (this == IsParentOf) return "Child";
            if (this == IsChildOf) return "Parent";
            if (this == IsPredecessorOf) return "Successor";
            if (this == IsSuccessorOf) return "Predecessor";
            if (this == IsDuplicateOf) return "Duplicate";
            if (this == HasDuplicate) return "Duplicate Of";
            if (this == IsRelatedTo) return "Related";

            throw new NotSupportedException("Unknown Enum");
        }
    }
    #endregion

}