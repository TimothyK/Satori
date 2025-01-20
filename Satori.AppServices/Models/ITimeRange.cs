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
}

public static class TimeRangeExtensions
{
    public static bool IsOverlapping(this ITimeRange range1, ITimeRange range2)
    {
        if (range1.End == null || range2.End == null)
        {
            return false;
        }
        if (range1.End < range1.Begin || range2.End < range2.Begin)
        {
            throw new InvalidOperationException($"{nameof(ITimeRange.End)} must be after {nameof(ITimeRange.Begin)}");
        }

        return range1.Begin < range2.End && range2.Begin < range1.End;
    }
}
