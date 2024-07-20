using Microsoft.Extensions.Logging;
using Satori.AppServices.ViewModels.ExportPayloads;
using Satori.MessageQueues;

namespace Satori.AppServices.Services;

/// <summary>
/// Reads task adjustments from the message queue and applies them to the Azure DevOps work items.
/// </summary>
/// <param name="userService"></param>
/// <param name="settings"></param>
/// <param name="completedWorkService"></param>
/// <param name="loggerFactory"></param>
public class TaskAdjustmentImporter(
    UserService userService
    , ConnectionSettings settings
    , CompletedWorkService completedWorkService
    , ILoggerFactory loggerFactory
) : IDisposable
{
    private Subscription<TaskAdjustment>? _subscription;

    private ILogger Logger => loggerFactory.CreateLogger<TaskAdjustmentImporter>();

    public void Start()
    {
        Logger.LogInformation("Starting Task Adjustment Importer...");
        var person = userService.GetCurrentUserAsync().GetAwaiter().GetResult();
        var queueName = $"Satori.TaskAdjustment.{person.DomainLogin}";
        _subscription = new Subscription<TaskAdjustment>(settings, queueName, OnReceive);
        _subscription.Start();
        Logger.LogInformation("Starting Task Adjustment Importer...Done");
    }

    private void OnReceive(TaskAdjustment payload)
    {
        Logger.LogInformation("Adjusting work item {workItemId} by {adjustment}", payload.WorkItemId, payload.Adjustment.TotalHours);
        completedWorkService.AdjustCompletedWorkAsync(payload.WorkItemId, payload.Adjustment.TotalHours).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}