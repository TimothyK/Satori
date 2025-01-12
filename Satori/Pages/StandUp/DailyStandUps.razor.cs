using Satori.AppServices.Services;
using Satori.AppServices.ViewModels.DailyStandUps;

namespace Satori.Pages.StandUp;

public class DateSelectorViewModel(DayOfWeek firstDayOfWeek, StandUpService? standUpService)
{
    public event EventHandler<EventArgs>? DateChanging;
    public event EventHandler<DateChangedEventArgs>? DateChanged;

    private DayOfWeek FirstDayOfWeek { get; } = firstDayOfWeek;

    public Period Period { get; set; } = Period.Today;

    public string PeriodText { get; private set; } = "Today";
    public DateOnly BeginDate { get; private set; } = Today;
    public DateOnly EndDate { get; private set; } = Today;
    public string DateRangeText { get; private set; } = DateTime.Today.ToString("D");

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public async Task ChangePeriodAsync(Period period)
    {
        SetPeriod(period);

        var today = Today;
        var beginDate = period switch
        {
            Period.Today => today,
            Period.LastTwoDays => today.AddDays(-1),
            Period.WorkWeek => GetStartOfWeek(today),
            Period.LastSevenDays => today.AddDays(-6),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown enum value")
        };
        await SetBeginDateAsync(beginDate);
    }

    private void SetPeriod(Period period)
    {
        Period = period;
        PeriodText = period switch
        {
            Period.Today => "Today",
            Period.LastTwoDays => "Last 2 Days",
            Period.WorkWeek => "Work Week",
            Period.LastSevenDays => "Last 7 Days",
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown enum value")
        };
    }

    private async Task SetBeginDateAsync(DateOnly beginDate)
    {
        BeginDate = beginDate;

        EndDate = Period == Period.WorkWeek ? BeginDate.AddDays(6)
            : Today;

        DateRangeText = beginDate == EndDate ? BeginDate.ToString("D")
            : $"{BeginDate:D} - {EndDate:D}";

        OnDateChanging();
        await Task.Yield();

        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (standUpService == null)
        {
            OnDateChanged(PeriodSummary.CreateEmpty());
            return;
        }

        var period = await standUpService.GetStandUpPeriodAsync(BeginDate, EndDate);
        OnDateChanged(period);

        await Task.Yield();
        await standUpService.GetWorkItemsAsync(period);
    }

    private void OnDateChanging()
    {
        DateChanging?.Invoke(this, EventArgs.Empty);
    }

    private void OnDateChanged(PeriodSummary days)
    {
        DateChanged?.Invoke(this, new DateChangedEventArgs(days));
    }


    private DateOnly GetStartOfWeek(DateOnly date)
    {
        while (date.DayOfWeek != FirstDayOfWeek)
        {
            date = date.AddDays(-1);
        }
        return date;
    }

    public async Task DecrementPeriodAsync()
    {
        switch (Period)
        {
            case Period.Today:
                await ChangePeriodAsync(Period.LastTwoDays);
                break;
            case Period.LastTwoDays:
                if (BeginDate < GetStartOfWeek(Today))
                {
                    SetPeriod(Period.WorkWeek);
                    await SetBeginDateAsync(GetStartOfWeek(BeginDate));
                }
                else
                {
                    await ChangePeriodAsync(Period.WorkWeek);
                }
                break;
            case Period.WorkWeek:
                if (Today.AddDays(-6) < BeginDate)
                {
                    await ChangePeriodAsync(Period.LastSevenDays);
                }
                else
                {
                    await SetBeginDateAsync(BeginDate.AddDays(-7));
                }
                break;
            case Period.LastSevenDays:
                SetPeriod(Period.WorkWeek);
                await SetBeginDateAsync(GetStartOfWeek(BeginDate));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Period), Period, "Unknown enum value");
        }
    }

    public async Task IncrementPeriodAsync()
    {
        switch (Period)
        {
            case Period.Today:
                // Do nothing
                break;
            case Period.LastTwoDays:
                await ChangePeriodAsync(Period.Today);
                break;
            case Period.WorkWeek:
                if (Today < BeginDate.AddDays(7))
                {
                    await ChangePeriodAsync(Period.LastTwoDays);
                }
                else if (GetStartOfWeek(Today) == BeginDate.AddDays(7))
                {
                    await ChangePeriodAsync(Period.LastSevenDays);
                }
                else
                {
                    await SetBeginDateAsync(BeginDate.AddDays(7));
                }
                break;
            case Period.LastSevenDays:
                if (GetStartOfWeek(Today) == Today)
                {
                    await ChangePeriodAsync(Period.LastTwoDays);
                }
                else
                {
                    await ChangePeriodAsync(Period.WorkWeek);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Period), Period, "Unknown enum value");
        }

    }

}

public class DateChangedEventArgs(PeriodSummary period) : EventArgs
{
    public PeriodSummary Period { get; } = period;
}