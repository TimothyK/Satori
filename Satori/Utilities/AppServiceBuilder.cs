using Satori.AppServices.Services;
using Satori.AzureDevOps;
using Serilog;

namespace Satori.Utilities
{
    internal static class AppServiceBuilder
    {
        public static WebApplicationBuilder AddAppServices(this WebApplicationBuilder builder)
        {
            var settings = builder.GetConnectionSettings();
            builder.Services.AddSingleton(settings);
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<PullRequestService>();
            builder.Services.AddSingleton(settings.AzureDevOps);
            builder.Services.AddSingleton<IAzureDevOpsServer, AzureDevOpsServer>();
            var loggerFactory = new LoggerFactory().AddSerilog();
            builder.Services.AddSingleton(loggerFactory);

            return builder;
        }

        private static AppServices.Models.ConnectionSettings GetConnectionSettings(this WebApplicationBuilder builder)
        {
            return new()
            {
                AzureDevOps = builder.GetAzureDevOpsSettings()
            };
        }

        private static ConnectionSettings GetAzureDevOpsSettings(this WebApplicationBuilder builder)
        {
            return new ConnectionSettings()
            {
                Url = new Uri(builder.Configuration["AzureDevOps:Url"] ?? throw new InvalidOperationException("Missing AzureDevOps:Url in settings")),
                PersonalAccessToken = builder.Configuration["AzureDevOps:PersonalAccessToken"] ?? string.Empty
            };
        }

    }
}
