using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai;
using Satori.Kimai.Models;
using CodeMonkeyProjectiles.Linq;

namespace Satori.AppServices.Services;

public class StandUpService(IKimaiServer kimai)
{
    public async Task<StandUpDay[]> GetStandUpDaysAsync(DateOnly begin, DateOnly end)
    {
        return (await BuildDaysAsync(begin, end)).Where(day => day.Date <= DateTime.Today).ToArray();
        //return [new StandUpDay() { Date = end, AllExported = true}];

        var filter = new TimeSheetFilter()
        {
            Begin = begin.ToDateTime(TimeOnly.MinValue),
            End = end.ToDateTime(TimeOnly.MaxValue),
            Active = true,
            Page = 1,
            Size = 250,
        };

        var timeSheet = await kimai.GetTimeSheetAsync(filter);

        var days = timeSheet.GroupBy(GetDateOnly);
        return days.Select(ToViewModel).ToArray();
    }

    private static async Task<StandUpDay[]> BuildDaysAsync(DateOnly begin, DateOnly end)
    {
        StandUpDay[] result = [];
        await Task.Run(() => result = BuildDays(begin, end));
        return result;
    }

    private static StandUpDay[] BuildDays(DateOnly begin, DateOnly end)
    {
        var result = new List<StandUpDay>();
        while (begin <= end)
        {
            var day = end.ToDateTime(TimeOnly.MinValue);
            result.Add(new StandUpDay()
            {
                Date = end,
                AllExported = day <= DateTime.Today.AddDays(-2) || DateTime.Today < day, CanExport = day == DateTime.Today
            });

            end = end.AddDays(-1);
        };
        return result.ToArray();
    }


    private static StandUpDay ToViewModel(IGrouping<DateOnly, TimeEntry> day)
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

    private static DateOnly GetDateOnly(TimeEntry entry) => DateOnly.FromDateTime(entry.Begin.Date);
}