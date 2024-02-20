﻿using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Services;

namespace Satori.AzureDevOps.Tests.Globals;

internal static class Services
{
    static Services()
    {
        var builder = new ContainerBuilder();

        var connectionSettings =  new ConnectionSettings
        {
            Url = new Uri("http://devops.test/Org"),
            PersonalAccessToken = "test"
        };
        builder.Register(_ => connectionSettings).As<ConnectionSettings>();

        var mockHttp = new MockHttpMessageHandler();
        builder.Register(_ => mockHttp).As<MockHttpMessageHandler>();
        builder.Register(_ => mockHttp.ToHttpClient()).As<HttpClient>();

        builder.Register(_ => NullLoggerFactory.Instance).As<ILoggerFactory>();

        var timeServer = new TestTimeServer();
        builder.Register(_ => timeServer).As<ITimeServer>();
        builder.Register(_ => timeServer).As<TestTimeServer>();

        builder.RegisterType<AzureDevOpsServer>().As<IAzureDevOpsServer>();

        var container = builder.Build();
        Scope = container.BeginLifetimeScope();
    }

    public static ILifetimeScope Scope { get; }
}