using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.TaskAdjustments;
using Satori.MessageQueues;

namespace Satori.AppServices.Services;

public class TaskAdjuster(UserService userService, ConnectionSettings settings) : Publisher<TaskAdjustment>, ITaskAdjuster
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