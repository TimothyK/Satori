namespace Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;

public class RandomGenerator
{
    private static readonly Random Random = new();

    public string String(int length = 10, CharType type = CharType.AlphaNumeric)
    {
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string digitChars = "0123456789";

        var chars = "";
        if (type.HasFlag(CharType.UpperCase))
        {
            chars += upperChars;
        }
        if (type.HasFlag(CharType.LowerCase))
        {
            chars += lowerChars;
        }
        if (type.HasFlag(CharType.Digit))
        {
            chars += digitChars;
        }

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)])
            .ToArray());
    }

    /// <summary>
    /// Random integer in the range [1, upperBound], inclusive
    /// </summary>
    /// <param name="upperBound">Inclusive upper bound.  The return value may be the same as this.</param>
    /// <returns></returns>
    public int Integer(int upperBound) => Integer(1, upperBound);

    /// <summary>
    /// Random integer in the range [lowerBound, upperBound], inclusive
    /// </summary>
    /// <param name="lowerBound">Inclusive lower bound</param>
    /// <param name="upperBound">Inclusive upper bound</param>
    /// <returns></returns>
    public static int Integer(int lowerBound, int upperBound) => Random.Next(lowerBound, upperBound + 1);

    /// <summary>
    /// Generates a random number with a normal distribution
    /// </summary>
    /// <remarks>
    /// <para>
    /// Taken from https://stackoverflow.com/a/218600/902742
    /// </para>
    /// </remarks>
    /// <param name="target"></param>
    /// <param name="standardDeviation">distribution of the returned value</param>
    /// <returns></returns>
    private static double Number(double target, double? standardDeviation = null)
    {
        var u1 = 1.0 - Random.NextDouble(); //uniform(0,1] random doubles
        var u2 = 1.0 - Random.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                            Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)

        return target + (standardDeviation ?? target * 0.1) * randStdNormal; //random normal(mean,stdDev^2)
    }

    public static TimeSpan TimeSpan(TimeSpan target, TimeSpan? standardDeviation = null)
    {
        return System.TimeSpan.FromSeconds(Number(target.TotalSeconds, standardDeviation?.TotalSeconds));
    }
}

public static class RandomSelectionExtensions
{
    public static T SingleRandom<T>(this IEnumerable<T> values) => values.ToArray().SingleRandom();

    public static T SingleRandom<T>(this T[] values)
    {
        var index = RandomGenerator.Integer(0, values.Length - 1);
        return values[index];
    }

    public static TimeSpan Randomize(this TimeSpan originalValue, TimeSpan? standardDeviation = null)
    {
        return RandomGenerator.TimeSpan(originalValue, standardDeviation);
    }
}

[Flags]
public enum CharType
{
    UpperCase = 1,
    LowerCase = 2,
    Digit = 4,
    AlphaNumeric = UpperCase | LowerCase | Digit,
}