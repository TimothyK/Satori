using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai;
using Satori.Kimai.Models;
using CodeMonkeyProjectiles.Linq;
using Flurl;
using System.Net;

namespace Satori.AppServices.Services;

public class StandUpService(IKimaiServer kimai)
{
    public async Task<StandUpDay[]> GetStandUpDaysAsync(DateOnly begin, DateOnly end)
    {
        if (end < begin)
        {
            throw new ArgumentException("start date must be before or equal to end date");
        }

        //return (await BuildDaysAsync(begin, end)).Where(day => day.Date.ToDateTime(TimeOnly.MinValue) <= DateTime.Today).ToArray();
        //return [new StandUpDay() { Date = end, AllExported = true}];

        var getUserTask = kimai.GetMyUserAsync();
        var getTimeSheetTask = GetTimeSheetAsync(begin, end);

        await Task.WhenAll(getUserTask, getTimeSheetTask);

        var timeSheet = getTimeSheetTask.Result;
        var url = kimai.BaseUrl.AppendPathSegments(getUserTask.Result.Language, "timesheet");

        var days = timeSheet.GroupBy(GetDateOnly);
        return days.Select(entries => ToViewModel(entries, url)).ToArray();
    }

    private async Task<List<TimeEntry>> GetTimeSheetAsync(DateOnly begin, DateOnly end)
    {
        var filter = new TimeSheetFilter()
        {
            Begin = begin.ToDateTime(TimeOnly.MinValue),
            End = end.ToDateTime(TimeOnly.MaxValue),
            IsRunning = false,
            Page = 1,
            Size = 250,
        };

        var timeSheet = new List<TimeEntry>();
        bool done;
        do
        {
            try
            {
                var page = await kimai.GetTimeSheetAsync(filter);
                timeSheet.AddRange(page);
                done = page.Length < filter.Size;
                filter.Page++;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                done = true;
            }
        } while (!done);

        return timeSheet;
    }

    private static StandUpDay ToViewModel(IGrouping<DateOnly, TimeEntry> day, Url url)
    {
        var uri = url
            .AppendQueryParam("daterange", $"{day.Key:O} - {day.Key:O}")
            .AppendQueryParam("state", 3)  // stopped
            .AppendQueryParam("billable", 0)
            .AppendQueryParam("exported", 1)
            .AppendQueryParam("orderBy", "begin")
            .AppendQueryParam("order", "DESC")
            .AppendQueryParam("searchTerm", string.Empty)
            .AppendQueryParam("performSearch", "performSearch")
            .ToUri();

        return new StandUpDay()
        {
            Date = day.Key,
            TotalTime = day.Select(GetDuration).Sum(),
            AllExported = day.All(entry => entry.Exported),
            CanExport = day.Any(GetCanExport),
            Url = uri
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