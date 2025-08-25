using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.TimeServices;

namespace Satori.AppServices.Tests.TestDoubles;

internal class SatoriServiceCollection : ServiceCollection
{
    public SatoriServiceCollection()
    {
        var azureDevOpsServer = new TestAzureDevOpsServer();
        this.AddSingleton(azureDevOpsServer);
        this.AddSingleton(azureDevOpsServer.AsInterface());
        
        var builder = azureDevOpsServer.CreateBuilder();
        this.AddSingleton(builder);

        var kimai = new TestKimaiServer();
        this.AddSingleton(kimai);
        this.AddSingleton(kimai.AsInterface());

        this.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);

        var alertService = new TestAlertService();
        this.AddSingleton(alertService);
        this.AddSingleton<IAlertService>(alertService);

        var timeServer = new TestTimeServer();
        this.AddSingleton(timeServer);
        this.AddSingleton<ITimeServer>(timeServer);
    }
}