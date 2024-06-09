using Satori.AppServices.ViewModels.TaskAdjustments;

namespace Satori.AppServices.Services.Abstractions;

public interface ITaskAdjuster
{
    void Send(TaskAdjustment payload);
}