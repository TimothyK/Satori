using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.ExportPayloads;
using Satori.MessageQueues;

namespace Satori.AppServices.Services;

public class TaskAdjustmentExporter(UserService userService, ConnectionSettings settings) : Publisher<TaskAdjustment>, ITaskAdjustmentExporter
{
    private async Task OpenAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        var exchangeName = $"Satori.TaskAdjustment.{user.DomainLogin}";
        Open(settings, exchangeName);
    }

    public override async Task SendAsync(TaskAdjustment message)
    {
        if (!IsOpen)
        {
            await OpenAsync();
        }

        await base.SendAsync(message);
    }
}