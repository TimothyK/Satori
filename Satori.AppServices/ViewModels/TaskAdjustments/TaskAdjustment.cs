namespace Satori.AppServices.ViewModels.TaskAdjustments;

public class TaskAdjustment
{
    /// <summary>
    /// Creates a task adjustment to be sent to Azure DevOps
    /// </summary>
    /// <param name="workItemId">ID of the work item that will be updated.  This should be a task.</param>
    /// <param name="adjustment">Amount to increment the Completed Work by</param>
    public TaskAdjustment(int workItemId, TimeSpan adjustment)
    {
        WorkItemId = workItemId;
        Adjustment = adjustment;
    }

    public int WorkItemId { get; }
    public TimeSpan Adjustment { get; }
}