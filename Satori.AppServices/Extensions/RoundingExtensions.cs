﻿namespace Satori.AppServices.Extensions;

public static class RoundingExtensions
{
    public static double ToNearest(this double value, double interval)
    {
        var factor = 1 / interval;
        return Math.Round(value * factor) / factor;
    }
    public static int ToNearestInt32(this double value) =>
        (int)Math.Round(value);

    public static TimeSpan ToNearest(this TimeSpan value, TimeSpan interval, RoundingDirection direction = RoundingDirection.Nearest)
    {
        var roundedTicks = ToNearestTick(value.Ticks, interval, direction);
        return TimeSpan.FromTicks(roundedTicks);
    }

    public static DateTimeOffset ToNearest(this DateTimeOffset value, TimeSpan interval, RoundingDirection direction = RoundingDirection.Nearest)
    {
        var roundedTicks = ToNearestTick(value.Ticks, interval, direction);
        return new DateTimeOffset(roundedTicks, value.Offset);
    }

    private static long ToNearestTick(long ticks, TimeSpan interval, RoundingDirection direction)
    {
        var roundingTicks = interval.Ticks;

        var remainder = ticks % roundingTicks;

        var roundUp = direction switch
        {
            RoundingDirection.Nearest => remainder > roundingTicks / 2,
            RoundingDirection.Floor => false,
            RoundingDirection.Ceiling => true,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        var roundedTicks = roundUp ? ticks - remainder + roundingTicks : ticks - remainder;

        return roundedTicks;
    }

    /// <summary>
    /// Removes the seconds
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static DateTimeOffset ToNearestMinute(this DateTimeOffset value) => 
        value.ToNearest(TimeSpan.FromMinutes(1));

    public static DateTimeOffset TruncateSeconds(this DateTimeOffset value) =>
        value.ToNearest(TimeSpan.FromMinutes(1), RoundingDirection.Floor);
}

public enum RoundingDirection 
{
    Nearest,
    Floor,
    Ceiling
}