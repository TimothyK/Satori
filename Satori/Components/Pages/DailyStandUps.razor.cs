using Satori.AppServices.ViewModels.DailyStandUps;

namespace Satori.Components.Pages
{
    public class DateSelectorViewModel(DayOfWeek firstDayOfWeek)
    {
        private DayOfWeek FirstDayOfWeek { get; } = firstDayOfWeek;

        private Period Period { get; set; } = Period.Today;

        public string PeriodText { get; private set; } = "Today";
        private DateTime BeginDate { get; set; } = DateTime.Today;
        public string DateRangeText { get; private set; } = DateTime.Today.ToString("D");

        public void ChangePeriod(Period period)
        {
            SetPeriod(period);

            var beginDate = period switch
            {
                Period.Today => DateTime.Today,
                Period.LastTwoDays => DateTime.Today.AddDays(-1),
                Period.WorkWeek => GetStartOfWeek(DateTime.Today),
                Period.LastSevenDays => DateTime.Today.AddDays(-6),
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

        private void SetBeginDate(DateTime beginDate)
        {
            BeginDate = beginDate;

            var endDate = Period == Period.WorkWeek ? BeginDate.AddDays(6)
                : DateTime.Today;

            DateRangeText = beginDate == endDate ? BeginDate.ToString("D")
                : $"{BeginDate:D} - {endDate:D}";
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            date = date.Date;
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
                    ChangePeriod(Period.WorkWeek);
                    break;
                case Period.WorkWeek:
                    if (DateTime.Today.AddDays(-6) < BeginDate)
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
                    if (DateTime.Today < BeginDate.AddDays(7))
                    {
                        ChangePeriod(Period.LastTwoDays);
                    }
                    else if (GetStartOfWeek(DateTime.Today) == BeginDate.AddDays(7))
                    {
                        ChangePeriod(Period.LastSevenDays);
                    }
                    else
                    {
                        SetBeginDate(BeginDate.AddDays(7));
                    }
                    break;
                case Period.LastSevenDays:
                    ChangePeriod(Period.WorkWeek);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Period), Period, "Unknown enum value");
            }

        }

    }
}
