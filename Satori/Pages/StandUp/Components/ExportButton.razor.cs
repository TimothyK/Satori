using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;

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
            AlertService.BroadcastAlert(ex);
        }
        finally
        {
            _isClicking = false;
        }

        await OnChanged.InvokeAsync();
    }
}