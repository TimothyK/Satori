using Satori.AppServices.Models;

namespace Satori.AppServices.Tests.TimeRangeTests;

internal class TimeRange(DateTimeOffset begin, DateTimeOffset? end) : ITimeRange
{
    public TimeRange(DateTimeOffset begin, TimeSpan duration) : this(begin, begin + duration)
    {
    }

    public DateTimeOffset Begin { get; } = begin;
    public DateTimeOffset? End { get; } = end;

    public override string ToString() => $"[{Begin:O}, {End:O})";

    public TimeSpan? Duration => End - Begin;

    public TimeRange NextBlock()
    {
        var duration = Duration ?? throw new InvalidOperationException($"{nameof(NextBlock)} is only available if {nameof(End)} is known");
        return this + duration;
    }
    public TimeRange PreviousBlock()
    {
        var duration = Duration ?? throw new InvalidOperationException($"{nameof(NextBlock)} is only available if {nameof(End)} is known");
        return this - duration;
    }

    public static TimeRange operator +(TimeRange timeRange, TimeSpan delta) => new(timeRange.Begin + delta, timeRange.End + delta);
    public static TimeRange operator -(TimeRange timeRange, TimeSpan delta) => new(timeRange.Begin - delta, timeRange.End - delta);
}