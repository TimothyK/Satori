using System.Collections.ObjectModel;
using Satori.Kimai.Utilities;

namespace Satori.Kimai.ViewModels;

public class Project
{
    internal Project()
    {
    }

    public int Id { get; set; }
    public required string Name { get; set; }
    public override string ToString() => Name;


    /// <summary>
    /// How this activity is identified in external systems (e.g. Azure DevOps)
    /// </summary>
    public required string ProjectCode { get; set; }

    public required Customer Customer { get; set; }

    private readonly List<Activity> _activities = [];
    public ReadOnlyCollection<Activity> Activities => _activities.AsReadOnly();
    public bool SupportGlobalActivities { get; set; }

    internal void AddActivity(Activity activity) => _activities.Add(activity);

    public Activity? FindActivity(string? rawProjectCode)
    {
        if (string.IsNullOrWhiteSpace(rawProjectCode))
        {
            return null;
        }

        if (!rawProjectCode.Contains('.'))
        {
            return null;
        }
        var afterFirstPeriod = rawProjectCode[(rawProjectCode.IndexOf('.') + 1)..];
        var activityCode = ProjectCodeParser.GetActivityCode(afterFirstPeriod);

        return Activities.FirstOrDefault(activity =>
            string.Equals(activity.ActivityCode, activityCode, StringComparison.CurrentCultureIgnoreCase));
    }
}