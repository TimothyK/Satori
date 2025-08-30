using System.Text.RegularExpressions;

namespace Satori.Kimai.Utilities;

public static partial class CustomerParser
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

    [GeneratedRegex(@"\((?'acronym'.*)\)", RegexOptions.IgnoreCase)]
    private static partial Regex CustomerAcronymRegex();

    public static string? GetAcronym(string customerName)
    {
        var match = CustomerAcronymRegex().Match(customerName);
        return match.Success ? match.Groups["acronym"].Value : null;
    }
}