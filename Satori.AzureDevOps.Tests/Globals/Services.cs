using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace Satori.AzureDevOps.Tests.Globals;

internal static class Services
{
    static Services()
    {
        var builder = new ContainerBuilder();

        var connectionSettingsFactory = new ConnectionSettingsFactory();
        builder.Register(_ => connectionSettingsFactory).As<ConnectionSettingsFactory>();
        builder.Register(_ => connectionSettingsFactory.GetConnectionSettings()).As<ConnectionSettings>();

        var mockHttp = new MockHttpMessageHandler();
        builder.Register(_ => mockHttp).As<MockHttpMessageHandler>();
        builder.Register(_ => mockHttp.ToHttpClient()).As<HttpClient>();

        builder.Register(_ => NullLoggerFactory.Instance).As<ILoggerFactory>();

        builder.RegisterType<AzureDevOpsServer>().As<IAzureDevOpsServer>();

        var container = builder.Build();
        Scope = container.BeginLifetimeScope();
    }

    public static ILifetimeScope Scope { get; }
}