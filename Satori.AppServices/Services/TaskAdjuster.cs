using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.TaskAdjustments;
using Satori.MessageQueues;

namespace Satori.AppServices.Services;

public class TaskAdjuster(UserService userService, ConnectionSettings settings) : Publisher<TaskAdjustment>, ITaskAdjuster
{
    private void Open()
    {
        var user = userService.GetCurrentUserAsync().GetAwaiter().GetResult();
        var exchangeName = $"Satori.TaskAdjustment.{user.DomainLogin}";
        Open(settings, exchangeName);
    }

    public override void Send(TaskAdjustment message)
    {
        if (!IsOpen)
        {
            Open();
        }

        base.Send(message);
    }
}