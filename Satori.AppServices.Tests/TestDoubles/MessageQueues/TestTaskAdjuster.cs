using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.TaskAdjustments;
using Shouldly;

namespace Satori.AppServices.Tests.TestDoubles.MessageQueues;

internal class TestTaskAdjuster : ITaskAdjuster
{
    public void Send(TaskAdjustment payload)
    {
        Adjustments.Add(payload);
    }

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