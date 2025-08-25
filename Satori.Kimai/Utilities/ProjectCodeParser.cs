using System.Text.RegularExpressions;

namespace Satori.Kimai.Utilities;

public static partial class ProjectCodeParser
{
    [GeneratedRegex(@"^0*(?'projectCode'\d+)\D?.*$", RegexOptions.IgnoreCase)]
    private static partial Regex ProjectCodeRegex();

    public static string GetProjectCode(string projectName)
    {
        var match = ProjectCodeRegex().Match(projectName);
        return match.Success ? match.Groups["projectCode"].Value 
            : projectName;
    }

    [GeneratedRegex(@"^(?<activityCode>(?:0*\d+(?:\.0*\d+)*))\D?.*$", RegexOptions.IgnoreCase)]
    private static partial Regex ActivityCodeRegex();

    public static string GetActivityCode(string activityName)
    {
        var match = ActivityCodeRegex().Match(activityName);
        if (!match.Success)
        {
            return activityName;
        }

        // Strip leading zeros from each segment
        var code = match.Groups["activityCode"].Value;
        var segments = code.Split('.');
        return string.Join('.', segments.Select(s => s.TrimStart('0').Length == 0 ? "0" : s.TrimStart('0')));
    }

}