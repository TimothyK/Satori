using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Satori.Pages.StandUp.Components;

public partial class ExportButton
{
    [Parameter]
    public required ISummary Summary { get; set; }
    private static bool _isClicking;
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private bool ShowDoneButton { get; set; }

    protected override void OnParametersSet()
    {
        if (Summary.IsRunning)
        {
            var task = GetRunningTimeEntry(Summary).Task;
            ShowDoneButton = 
                task != null 
                && task.Type == WorkItemType.Task 
                && task.State != ScrumState.Done 
                && task.AssignedTo == Person.Me;
        }
        else
        {
            ShowDoneButton = false;
        }

        base.OnParametersSet();
    }

    private async Task ExportAsync(ISummary summary)
    {
        await ClickButtonTemplateAsync(() => StandUpService.ExportAsync(summary.TimeEntries.Where(x => x.CanExport).ToArray()));
    }

    private async Task StopAsync(ISummary summary)
    {
        var runningTimeEntry = GetRunningTimeEntry(summary);
        await ClickButtonTemplateAsync(() => StandUpService.StopTimerAsync(runningTimeEntry));
    }

    private async Task StopAndDoneAsync(ISummary summary)
    {
        var runningTimeEntry = GetRunningTimeEntry(summary);
        await ClickButtonTemplateAsync(async () =>
        {
            await StandUpService.StopTimerAsync(runningTimeEntry);

            var task = runningTimeEntry.Task;
            if (task != null)
            {
                await WorkItemUpdateService.UpdateTaskAsync(task, ScrumState.Done);
            }
        });

    }

    private static TimeEntry GetRunningTimeEntry(ISummary summary)
    {
        var runningTimeEntry = summary.TimeEntries.SingleOrDefault(x => x.IsRunning);
        if (runningTimeEntry == null)
        {
            throw new InvalidOperationException("Stop button should not have been available");
        }

        return runningTimeEntry;
    }

    private async Task ClickButtonTemplateAsync(Func<Task> doWork)
    {
        ClearAlert();
        if (_isClicking)
        {
            return;
        }

        _isClicking = true;
        try
        {
            await doWork.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ShowAlert(ex.Message);
        }
        finally
        {
            _isClicking = false;
        }

        await OnChanged.InvokeAsync();
    }

    private string ShowAlertClassName { get; set; } = "d-none";
    private string AlertContent { get; set; } = string.Empty;
    private Timer? _timer;
    public void ClearAlert()
    {
        ShowAlertClassName = "d-none";
        AlertContent = string.Empty;
    }

    public void ShowAlert(string message)
    {
        AlertContent = message;
        ShowAlertClassName = string.Empty;

        var timer = new Timer(TimeSpan.FromSeconds(30));
        timer.Elapsed += HandleTimer;
        timer.Start();
    }

    private void HandleTimer(object? sender, ElapsedEventArgs e)
    {
        ClearAlert();
        _timer?.Stop();
        _timer = null;
        StateHasChanged();
    }

}