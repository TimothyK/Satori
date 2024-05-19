﻿using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai;
using Satori.Kimai.Models;
using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.Services.Converters;
using System.Net;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;

namespace Satori.AppServices.Services;

public class StandUpService(IKimaiServer kimai)
{
    public async Task<StandUpDay[]> GetStandUpDaysAsync(DateOnly begin, DateOnly end)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (today < end)
        {
            end = today;
        }
        if (end < begin)
        {
            throw new ArgumentException($"Start date {begin:O} must be before or equal to end date {end:O}");
        }
        var daysInRange = (end.ToDateTime(TimeOnly.MinValue) - begin.ToDateTime(TimeOnly.MinValue)).Days + 1;
        if (daysInRange > 7)
        {
            throw new ArgumentException("There are too many days requested in this report.  Please use a smaller date range");
        }

        var getUserTask = kimai.GetMyUserAsync();
        var getTimeSheetTask = GetTimeSheetAsync(begin, end);

        await Task.WhenAll(getUserTask, getTimeSheetTask);

        var language = getUserTask.Result.Language;
        var url = kimai.BaseUrl.AppendPathSegments(language, "timesheet");

        var timeSheet = getTimeSheetTask.Result;
        var days = timeSheet.GroupBy(GetDateOnly);
        var standUpDays = days.Select(entries => ToDayViewModel(entries, url)).ToList();

        var allDays = Enumerable.Range(0, daysInRange).Select(begin.AddDays);
        AddMissingDays(standUpDays, allDays, url);

        return standUpDays.OrderByDescending(day => day.Date).ToArray();
    }

    private async Task<List<KimaiTimeEntry>> GetTimeSheetAsync(DateOnly begin, DateOnly end)
    {
        var filter = new TimeSheetFilter()
        {
            Begin = begin.ToDateTime(TimeOnly.MinValue),
            End = end.ToDateTime(TimeOnly.MaxValue),
            IsRunning = false,
            Page = 1,
            Size = 250,
        };

        var timeSheet = new List<KimaiTimeEntry>();
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

    private static void AddMissingDays(List<StandUpDay> standUpDays, IEnumerable<DateOnly> allDays, Url url)
    {
        standUpDays.AddRange( 
            allDays
                .Where(d => d.IsNotIn(standUpDays.Select(x => x.Date)))
                .Select(d => new NullGroup<DateOnly, KimaiTimeEntry>(d))
                .Select(g => ToDayViewModel(g, url))
        );
    }

    private static StandUpDay ToDayViewModel(IGrouping<DateOnly, KimaiTimeEntry> day, Url url)
    {
        var uri = url.ToUri()
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
            TotalTime = GetDuration(day),
            AllExported = GetAllExported(day),
            CanExport = GetCanExport(day),
            Url = uri,
            Projects = ToProjectsViewModel(day, uri),
        };
    }

    private static ProjectSummary[] ToProjectsViewModel(IEnumerable<KimaiTimeEntry> entries, Url url)
    {
        var groups = entries.GroupBy(entry => new
        {
            ProjectID = entry.Project.Id,
            ProjectName = entry.Project.Name,
            CustomerID = entry.Project.Customer.Id,
            CustomerName = entry.Project.Customer.Name,
        });

        return groups.Select(g =>
            {
                var uri = url.ToUri().AppendQueryParam("projects[]", g.Key.ProjectID).ToUri();
                return new ProjectSummary()
                {
                    ProjectId = g.Key.ProjectID,
                    ProjectName = g.Key.ProjectName,
                    CustomerId = g.Key.CustomerID,
                    CustomerName = g.Key.CustomerName,
                    TotalTime = GetDuration(g),
                    AllExported = GetAllExported(g),
                    CanExport = GetCanExport(g),
                    Url = uri,
                    Activities = ToActivitiesViewModel(g, uri),
                };
            })
            .OrderByDescending(p => p.TotalTime).ThenBy(p => p.ProjectName)
            .ToArray();
    }

    private static ActivitySummary[] ToActivitiesViewModel(IEnumerable<KimaiTimeEntry> entries, Url url)
    {
        var groups = entries.GroupBy(entry => new
        {
            entry.Activity.Id,
            entry.Activity.Name,
            entry.Activity.Comment,
            ProjectId = entry.Project.Id,
        });

        return groups.Select(g =>
            {
                var uri = url.ToUri().AppendQueryParam("activities[]", g.Key.Id).ToUri();
                return new ActivitySummary()
                {
                    ActivityId = g.Key.Id,
                    ActivityName = g.Key.Name,
                    Comment = g.Key.Comment,
                    TotalTime = GetDuration(g),
                    AllExported = GetAllExported(g),
                    CanExport = GetCanExport(g),
                    Url = uri,
                    TimeEntries = g.Select(ToViewModel).ToArray(),
                };
            })
            .OrderByDescending(a => a.TotalTime).ThenBy(a => a.ActivityName)
            .ToArray();
    }

    private static TimeEntry ToViewModel(KimaiTimeEntry kimaiEntry)
    {
        return new TimeEntry()
        {
            Id = kimaiEntry.Id,
            Begin = kimaiEntry.Begin,
            End = kimaiEntry.End,
            TotalTime = GetDuration(kimaiEntry),
            Exported = kimaiEntry.Exported,
            CanExport = GetCanExport(kimaiEntry),
            OtherComments = kimaiEntry.Description,
        };
    }

    private static bool GetAllExported(IEnumerable<KimaiTimeEntry> entries) => entries.All(entry => entry.Exported);

    private static bool GetCanExport(IEnumerable<KimaiTimeEntry> entries) => entries.Any(GetCanExport);

    private static bool GetCanExport(KimaiTimeEntry entry)
    {
        if (entry.Exported) return false;
        if (!entry.Activity.Visible) return false;
        if (!entry.Project.Visible) return false;
        if (!entry.Project.Customer.Visible) return false;
        if (entry.Activity.Name == "TBD") return false;
        if (entry.Project.Name == "TBD") return false;

        return true;
    }

    private static TimeSpan GetDuration(IEnumerable<KimaiTimeEntry> entries) => entries.Select(GetDuration).Sum();

    private static TimeSpan GetDuration(KimaiTimeEntry entry) => entry.End != null ? (entry.End.Value - entry.Begin) : TimeSpan.Zero;

    private static DateOnly GetDateOnly(KimaiTimeEntry entry) => DateOnly.FromDateTime(entry.Begin.Date);
}