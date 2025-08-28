using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace Satori.AzureDevOps.Tests.Globals;

internal class AzureDevOpsServiceCollection : ServiceCollection
{
    public AzureDevOpsServiceCollection()
    {
        var connectionSettingsFactory = new ConnectionSettingsFactory();
        this.AddScoped(_ => connectionSettingsFactory);
        this.AddScoped(provider => provider.GetRequiredService<ConnectionSettingsFactory>().GetConnectionSettings());

        var mockHttp = new MockHttpMessageHandler();
        this.AddScoped(_ => mockHttp);
        this.AddScoped(_ => mockHttp.ToHttpClient());

        this.AddScoped<Microsoft.Extensions.Logging.ILoggerFactory>(_ => NullLoggerFactory.Instance);

        this.AddScoped<IAzureDevOpsServer, AzureDevOpsServer>();
    }
}