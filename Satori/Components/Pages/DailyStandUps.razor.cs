using Satori.AppServices.ViewModels.DailyStandUps;

namespace Satori.Components.Pages
{
    public class DateSelectorViewModel(DayOfWeek firstDayOfWeek)
    {
        public event EventHandler<EventArgs>? DateChanged;

        private DayOfWeek FirstDayOfWeek { get; } = firstDayOfWeek;

        public Period Period { get; set; } = Period.Today;

        public string PeriodText { get; private set; } = "Today";
        public DateOnly BeginDate { get; set; } = Today;
        public DateOnly EndDate { get; set; } = Today;
        public string DateRangeText { get; private set; } = DateTime.Today.ToString("D");

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        public void ChangePeriod(Period period)
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
            SetBeginDate(beginDate);
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

        private void SetBeginDate(DateOnly beginDate)
        {
            BeginDate = beginDate;

            EndDate = Period == Period.WorkWeek ? BeginDate.AddDays(6)
                : Today;

            DateRangeText = beginDate == EndDate ? BeginDate.ToString("D")
                : $"{BeginDate:D} - {EndDate:D}";

            OnDateChanged();
        }

        private void OnDateChanged()
        {
            DateChanged?.Invoke(this, EventArgs.Empty);
        }


        private DateOnly GetStartOfWeek(DateOnly date)
        {
            while (date.DayOfWeek != FirstDayOfWeek)
            {
                date = date.AddDays(-1);
            }
            return date;
        }

        public void DecrementPeriod()
        {
            switch (Period)
            {
                case Period.Today:
                    ChangePeriod(Period.LastTwoDays);
                    break;
                case Period.LastTwoDays:
                    if (BeginDate < GetStartOfWeek(Today))
                    {
                        SetPeriod(Period.WorkWeek);
                        SetBeginDate(GetStartOfWeek(BeginDate));
                    }
                    else
                    {
                        ChangePeriod(Period.WorkWeek);
                    }
                    break;
                case Period.WorkWeek:
                    if (Today.AddDays(-6) < BeginDate)
                    {
                        ChangePeriod(Period.LastSevenDays);
                    }
                    else
                    {
                        SetBeginDate(BeginDate.AddDays(-7));
                    }
                    break;
                case Period.LastSevenDays:
                    SetPeriod(Period.WorkWeek);
                    SetBeginDate(GetStartOfWeek(BeginDate));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Period), Period, "Unknown enum value");
            }
        }

        public void IncrementPeriod()
        {
            switch (Period)
            {
                case Period.Today:
                    // Do nothing
                    break;
                case Period.LastTwoDays:
                    ChangePeriod(Period.Today);
                    break;
                case Period.WorkWeek:
                    if (Today < BeginDate.AddDays(7))
                    {
                        ChangePeriod(Period.LastTwoDays);
                    }
                    else if (GetStartOfWeek(Today) == BeginDate.AddDays(7))
                    {
                        ChangePeriod(Period.LastSevenDays);
                    }
                    else
                    {
                        SetBeginDate(BeginDate.AddDays(7));
                    }
                    break;
                case Period.LastSevenDays:
                    if (GetStartOfWeek(Today) == Today)
                    {
                        ChangePeriod(Period.LastTwoDays);
                    }
                    else
                    {
                        ChangePeriod(Period.WorkWeek);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Period), Period, "Unknown enum value");
            }

        }

    }
}
