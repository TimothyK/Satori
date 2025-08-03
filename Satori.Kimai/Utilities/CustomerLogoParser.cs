using System.Text.RegularExpressions;

namespace Satori.Kimai.Utilities;

public static partial class CustomerLogoParser
{
    [GeneratedRegex(@"\[Logo\]\((?'url'.*)\)", RegexOptions.IgnoreCase)]
    private static partial Regex CustomerLogoRegex();

    public static Uri? GetCustomerLogo(string? comment)
    {
        if (comment == null)
        {
            return null;
        }

        var match = CustomerLogoRegex().Match(comment);
        if (!match.Success)
        {
            return null;
        }

        try
        {
            return new Uri(match.Groups["url"].Value);
        }
        catch (UriFormatException)
        {
            return null;

        }
    }
}