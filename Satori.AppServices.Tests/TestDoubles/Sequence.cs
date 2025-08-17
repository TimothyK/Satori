namespace Satori.AppServices.Tests.TestDoubles;

public class Sequence
{
    private Sequence()
    {
        _value = RandomGenerator.Integer(0, 99);
    }

    #region Next

    private int _value;

    public int Next() => Interlocked.Increment(ref _value);

    #endregion Next

    #region Instances

    public static readonly Sequence ActivityId = new();
    public static readonly Sequence ProjectId = new();
    public static readonly Sequence CustomerId = new();
    public static readonly Sequence TimeEntryId = new();

    public static readonly Sequence WorkItemId = new();
    
    public static readonly Sequence KimaiUserId = new();

    #endregion Instances
}