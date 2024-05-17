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
    public int Integer(int lowerBound, int upperBound) => Random.Next(lowerBound, upperBound + 1);

}

public static class RandomSelectionExtensions
{
    private static readonly RandomGenerator _random = new();

    public static T SingleRandom<T>(this IEnumerable<T> values) => values.ToArray().SingleRandom();

    public static T SingleRandom<T>(this T[] values)
    {
        var index = _random.Integer(0, values.Length - 1);
        return values[index];
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