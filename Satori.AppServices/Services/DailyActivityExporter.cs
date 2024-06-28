using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.ExportPayloads;
using Satori.MessageQueues;

namespace Satori.AppServices.Services;

public class DailyActivityExporter(UserService userService, ConnectionSettings settings) : Publisher<DailyActivity>, IDailyActivityExporter
{
    private async Task OpenAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        var exchangeName = $"Satori.DailyActivity.{user.DomainLogin}";
        Open(settings, exchangeName);
    }

    public override async Task SendAsync(DailyActivity message)
    {
        if (!IsOpen)
        {
            await OpenAsync();
        }

        await base.SendAsync(message);
    }
}
