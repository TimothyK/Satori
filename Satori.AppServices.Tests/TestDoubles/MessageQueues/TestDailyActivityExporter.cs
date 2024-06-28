using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.ExportPayloads;

namespace Satori.AppServices.Tests.TestDoubles.MessageQueues;

internal class TestDailyActivityExporter : IDailyActivityExporter
{
    public Task SendAsync(DailyActivity payload)
    {
        Messages.Add(payload);

        return Task.CompletedTask;
    }

    public List<DailyActivity> Messages { get; } = [];
}