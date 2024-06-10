using Satori.AppServices.ViewModels.ExportPayloads;

namespace Satori.AppServices.Services.Abstractions;

public interface ITaskAdjustmentExporter
{
    Task SendAsync(TaskAdjustment payload);
}