using System.Text.RegularExpressions;

namespace Satori.Kimai.Utilities;

internal static partial class ProjectCodeParser
{
    [GeneratedRegex(@"^0*(?'projectCode'\d+)\D.*", RegexOptions.IgnoreCase)]
    private static partial Regex ProjectCodeRegex();

    public static string GetProjectCode(string projectName)
    {
        var match = ProjectCodeRegex().Match(projectName);
        return match.Success ? match.Groups["projectCode"].Value 
            : projectName;
    }
}