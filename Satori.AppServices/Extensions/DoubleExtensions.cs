namespace Satori.AppServices.Extensions;

public static class DoubleExtensions
{
    public static double ToNearest(this double value, double interval)
    {
        var factor = 1 / interval;
        return Math.Round(value * factor) / factor;
    }
}