using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai;
using Satori.Kimai.Models;
using CodeMonkeyProjectiles.Linq;

namespace Satori.AppServices.Services;

public class StandUpService(IKimaiServer kimai)
{
    public async Task<StandUpDay[]> GetStandUpDaysAsync(DateTime begin, DateTime end)
    {
        return (await BuildDaysAsync(begin, end)).Where(day => day.Date <= DateTime.Today).ToArray();
        //return [new StandUpDay() { Date = end, AllExported = true}];

        var filter = new TimeSheetFilter()
        {
            Begin = begin,
            End = end,
            Active = true,
            Page = 1,
            Size = 250,
        };

        var timeSheet = await kimai.GetTimeSheetAsync(filter);

        var days = timeSheet.GroupBy(entry => entry.Begin.Date);
        return days.Select(ToViewModel).ToArray();
    }

    private static async Task<StandUpDay[]> BuildDaysAsync(DateTime begin, DateTime end)
    {
        StandUpDay[] result = [];
        await Task.Run(() => result = BuildDays(begin, end));
        return result;
    }

    private static StandUpDay[] BuildDays(DateTime begin, DateTime end)
    {
        var result = new List<StandUpDay>();
        while (begin <= end)
        {
            result.Add(new StandUpDay(){Date = end, AllExported = end <= DateTime.Today.AddDays(-2) || DateTime.Today < end, CanExport = end == DateTime.Today});

            end = end.AddDays(-1);
        };
        return result.ToArray();
    }


    private static StandUpDay ToViewModel(IGrouping<DateTime, TimeEntry> day)
    {
        return new StandUpDay()
        {
            Date = day.Key,
            TotalTime = day.Select(GetDuration).Sum(),
            AllExported = day.All(entry => entry.Exported),
            CanExport = day.Any(GetCanExport),
        };
    }

    private static bool GetCanExport(TimeEntry entry)
    {
        if (entry.Exported) return false;
        if (!entry.Activity.Visible) return false;
        if (!entry.Project.Visible) return false;
        if (!entry.Project.Customer.Visible) return false;
        if (entry.Activity.Name == "TBD") return false;
        if (entry.Project.Name == "TBD") return false;

        return true;
    }

    private static TimeSpan GetDuration(TimeEntry entry)
    {
        return entry.End != null ? (entry.End.Value - entry.Begin) : TimeSpan.Zero;
    }
}