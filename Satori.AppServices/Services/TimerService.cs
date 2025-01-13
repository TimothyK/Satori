using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai;

namespace Satori.AppServices.Services;

public class TimerService(IKimaiServer kimaiServer)
{
    public async Task StopTimerAsync(TimeEntry timeEntry)
    {
        await kimaiServer.StopTimerAsync(timeEntry.Id);
    }
}