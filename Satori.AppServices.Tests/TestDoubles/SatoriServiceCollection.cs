using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.TimeServices;

namespace Satori.AppServices.Tests.TestDoubles;

internal class SatoriServiceCollection : ServiceCollection
{
    public SatoriServiceCollection()
    {
        var azureDevOpsServer = new TestAzureDevOpsServer();
        this.AddScoped(_ => azureDevOpsServer);
        this.AddScoped(_ => azureDevOpsServer.AsInterface());

        var builder = azureDevOpsServer.CreateBuilder();
        this.AddScoped(_ => builder);

        var kimai = new TestKimaiServer();
        this.AddScoped(_ => kimai);
        this.AddScoped(_ => kimai.AsInterface());

        this.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);

        var alertService = new TestAlertService();
        this.AddScoped(_ => alertService);
        this.AddScoped<IAlertService>(_ => alertService);

        var timeServer = new TestTimeServer();
        this.AddScoped(_ => timeServer);
        this.AddScoped<ITimeServer>(_ => timeServer);

        this.AddScoped<UserService>();
        this.AddScoped<WorkItemUpdateService>();
    }
}