using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.ExportPayloads;
using Satori.MessageQueues;

namespace Satori.AppServices.Services;

public class DailyActivityExporter(
    ConnectionSettings settings
    , HttpClient httpClient
) : Publisher<DailyActivity>(settings, httpClient), IDailyActivityExporter
{
    public override async Task SendAsync(DailyActivity message)
    {
        await base.SendAsync(message);
    }
}
