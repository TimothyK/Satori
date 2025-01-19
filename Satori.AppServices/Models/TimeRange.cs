namespace Satori.AppServices.Models;

public interface ITimeRange
{
    /// <summary>
    /// Start of the time range, inclusive.
    /// </summary>
    public DateTimeOffset Begin { get; }
    /// <summary>
    /// End of the time range, exclusive.
    /// </summary>
    public DateTimeOffset? End { get; }

    public bool IsOverlapping(ITimeRange other)
    {
        if (End == null || other.End == null)
        {
            return false;
        }

        return Begin < other.End && other.Begin < End;
    }
}