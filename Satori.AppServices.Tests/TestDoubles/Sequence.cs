namespace Satori.AppServices.Tests.TestDoubles;

public class Sequence
{
    private int _value;

    public int Next() => Interlocked.Increment(ref _value);

    public static readonly Sequence ActivityId = new();
    public static readonly Sequence ProjectId = new();
    public static readonly Sequence CustomerId = new();
    public static readonly Sequence TimeEntryId = new();

    public static readonly Sequence WorkItemId = new();
}