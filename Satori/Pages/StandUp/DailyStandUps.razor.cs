using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.Services;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using System.Timers;
using Microsoft.VisualStudio.Threading;
using Timer = System.Timers.Timer;
using Toolbelt.Blazor.HotKeys2;
using Satori.Utilities;

namespace Satori.Pages.StandUp;

public partial class DailyStandUps
{
    private Person CurrentUser { get; set; } = Person.Empty;
    private DateSelectorViewModel DateSelector { get; set; } = new(Person.Empty.FirstDayOfWeek, standUpService: null);
    private PeriodSummary Period { get; set; } = PeriodSummary.CreateEmpty();
    private Timer? RunningTimeEntryTimer { get; set; } 
    private TimeEntry? RunningTimeEntry { get; set; }

    private bool _inLoading = true;

    private bool InLoading
    {
        get => _inLoading;
        set
        {
            _inLoading = value;
            InLoadingLabel = value ? LoadingStatusLabel.InLoading : LoadingStatusLabel.FinishedLoading;
            InLoadingCssClass = value ? new CssClass("in-loading") : CssClass.None;
        }
    }

    private CssClass InLoadingCssClass { get; set; } = new("in-loading");
    private LoadingStatusLabel InLoadingLabel { get; set; } = LoadingStatusLabel.InLoading;

    protected override async Task OnInitializedAsync()
    {
        if (!ConnectionSettingsStore.GetKimaiSettings().Enabled)
        {
            // This page shouldn't be accessible if Kimai is disabled.  Go to Home page where Kimai can be configured/enabled.
            NavigationManager.NavigateTo("/");
        }

        CurrentUser = await UserService.GetCurrentUserAsync();
        var period = GetPeriodFromUrl();
        DateSelector = new DateSelectorViewModel(CurrentUser.FirstDayOfWeek, StandUpService);
        DateSelector.DateChangingAsync += DateChangingAsync;
        DateSelector.DateChanged += DateChanged;
        await DateSelector.ChangePeriodAsync(period);
    }

    private Period GetPeriodFromUrl()
    {
        var periodString = new Url(NavigationManager.Uri).QueryParams
            .Where(qp => qp.Name == "DatePeriod")
            .Select(qp => qp.Value.ToString())
            .FirstOrDefault();
        return Enum.TryParse(periodString, out Period period) ? period : DateSelector.Period;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            HotKeys.CreateContext()
                .Add(ModCode.Alt, Code.F5, RefreshAsync, new HotKeyOptions { Description = "Refresh" });

            var period = await LocalStorage.GetItemAsync<Period>("DatePeriod");
            CurrentPeriod = period;
            await DateSelector.ChangePeriodAsync(period);
            StateHasChanged();
        }
    }

    private Period CurrentPeriod { get; set; }
    private async Task DateChangingAsync(object? sender, EventArgs eventArgs)
    {
        InLoading = true;
        Period = PeriodSummary.CreateEmpty();

        if (CurrentPeriod == DateSelector.Period)
        {
            return;
        }

        var url = NavigationManager.Uri
            .AppendQueryParam("DatePeriod", null)
            .AppendQueryParam("DatePeriod", DateSelector.Period);
        NavigationManager.NavigateTo(url, forceLoad: false);

        await LocalStorage.SetItemAsync("DatePeriod", DateSelector.Period);
        CurrentPeriod = DateSelector.Period;
    }

    private async Task RefreshAsync()
    {
        InLoading = true;
        StateHasChanged();
        await DateSelector.RefreshAsync();
    }

    private void DateChanged(object? sender, DateChangedEventArgs eventArgs)
    {
        Period = eventArgs.Period;

        StartRunningTaskTimer();

        InLoading = false;
    }

    private void StartRunningTaskTimer()
    {
        RunningTimeEntry = Period.TimeEntries.FirstOrDefault(e => e.IsRunning);

        RunningTimeEntryTimer ??= new Timer(TimeSpan.FromMinutes(1))
            .With(t => t.Elapsed += RunningTimeEntryTimer_Elapsed);
        RunningTimeEntryTimer.Enabled = RunningTimeEntry != null;
        RefreshTotalTimes();
    }

    private void RunningTimeEntryTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        RefreshTotalTimes();
        StateHasChanged();
    }

    private void RefreshTotalTimes()
    {
        if (RunningTimeEntry == null || !RunningTimeEntry.IsRunning)
        {
            RunningTimeEntryTimer?.Stop();
            return;
        }

        StandUpService.CascadeEndTimeChange(RunningTimeEntry, DateTimeOffset.Now);
    }

    private void OnOpeningDialog()
    {
        RunningTimeEntry = null;
        RunningTimeEntryTimer?.Stop();
    }
    private void OnSavedDialog()
    {
        StateHasChanged();
    }
    private void OnClosedDialog()
    {
        StartRunningTaskTimer();
    }

    #region Collapse

    private static void ToggleCollapsed(DaySummary day)
    {
        day.IsCollapsed = !day.IsCollapsed;
    }
    private static void ToggleCollapsed(ProjectSummary project)
    {
        project.IsCollapsed = !project.IsCollapsed;
    }
    private static void ToggleCollapsed(ActivitySummary activity)
    {
        activity.IsCollapsed = !activity.IsCollapsed;
    }   
    private static void ToggleCollapsed(TaskSummary taskSummary)
    {
        taskSummary.IsCollapsed = !taskSummary.IsCollapsed;
    }  
    
    private void CollapseAll()
    {
        foreach (var node in Period.Days)
        {
            node.IsCollapsed = true;
        }
    }

    private void CollapseProjects()
    {
        foreach (var day in Period.Days)
        {
            day.IsCollapsed = false;
            foreach (var project in day.Projects)
            {
                project.IsCollapsed = true;
            }
        }
    }

    private void CollapseActivities()
    {
        foreach (var day in Period.Days)
        {
            day.IsCollapsed = false;
            foreach (var project in day.Projects)
            {
                project.IsCollapsed = false;
                foreach (var activity in project.Activities)
                {
                    activity.IsCollapsed = true;
                }
            }
        }
    }

    private void CollapseTasks()
    {
        foreach (var day in Period.Days)
        {
            day.IsCollapsed = false;
            foreach (var project in day.Projects)
            {
                project.IsCollapsed = false;
                foreach (var activity in project.Activities)
                {
                    activity.IsCollapsed = false;
                    foreach (var taskSummary in activity.TaskSummaries)
                    {
                        taskSummary.IsCollapsed = true;
                    }
                }
            }
        }
    }

    private void CollapseNone()
    {
        foreach (var day in Period.Days)
        {
            day.IsCollapsed = false;
            foreach(var project in day.Projects)
            {
                project.IsCollapsed = false;
                foreach (var activity in project.Activities)
                {
                    activity.IsCollapsed = false;
                    foreach (var taskSummary in activity.TaskSummaries)
                    {
                        taskSummary.IsCollapsed = false;
                    }
                }
            }
        }
    }

    #endregion Collapse

    private class LoadingStatusLabel
    {
        private readonly string _label;

        private LoadingStatusLabel(string label)
        {
            _label = label;
        }

        public override string ToString() => _label;

        public static readonly LoadingStatusLabel InLoading = new("Loading: ");
        public static readonly LoadingStatusLabel FinishedLoading = new(string.Empty);
    }

    private static bool _isClicking;

    private async Task RestartTimerAsync(params TimeEntry[] timeEntries)
    {
        if (_isClicking)
        {
            return;
        }
        _isClicking = true;

        try
        {
            var ids = timeEntries.Select(te => te.Id).ToArray();
            await TimerService.RestartTimerAsync(ids);

            await RefreshAsync();
        }
        finally
        {
            _isClicking = false;
        }
    }
}

public class DateSelectorViewModel(DayOfWeek firstDayOfWeek, StandUpService? standUpService)
{
    public event AsyncEventHandler<EventArgs>? DateChangingAsync;
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

        await OnDateChangingAsync();
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

    private async Task OnDateChangingAsync()
    {
        if (DateChangingAsync != null)
        {
            await DateChangingAsync.InvokeAsync(this, EventArgs.Empty);
        }
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