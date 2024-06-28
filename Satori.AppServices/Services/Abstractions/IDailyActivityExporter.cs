using Satori.AppServices.ViewModels.ExportPayloads;

namespace Satori.AppServices.Services.Abstractions;

public interface IDailyActivityExporter
{
    Task SendAsync(DailyActivity payload);
}