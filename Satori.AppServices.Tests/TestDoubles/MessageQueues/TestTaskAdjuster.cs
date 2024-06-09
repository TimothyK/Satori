using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.TaskAdjustments;
using Shouldly;

namespace Satori.AppServices.Tests.TestDoubles.MessageQueues;

internal class TestTaskAdjuster : ITaskAdjuster
{
    public Task SendAsync(TaskAdjustment payload)
    {
        if (ThrowOnSend)
        {
            throw new ApplicationException("Simulating error sending message to message queue");
        }

        Adjustments.Add(payload);

        return Task.CompletedTask;
    }

    public bool ThrowOnSend { get; set; }

    private List<TaskAdjustment> Adjustments { get; } = [];

    public TaskAdjustment Find(int workItemId)
    {
        var adjustment = FindOrDefault(workItemId);
        adjustment.ShouldNotBeNull();

        return adjustment;
    }
    
    public TaskAdjustment? FindOrDefault(int workItemId) => 
        Adjustments.SingleOrDefault(x => x.WorkItemId == workItemId);
}