using Satori.AppServices.Services;

namespace Satori.Utilities
{
    internal static class AppServiceBuilder
    {
        public static WebApplicationBuilder AddAppServices(this WebApplicationBuilder builder)
        {
            var settings = builder.GetConnectionSettings();
            builder.Services.AddSingleton(new PullRequestService(settings));

            return builder;
        }

        private static AppServices.Models.ConnectionSettings GetConnectionSettings(this WebApplicationBuilder builder)
        {
            return new()
            {
                AzureDevOps = builder.GetAzureDevOpsSettings()
            };
        }

        private static AzureDevOps.ConnectionSettings GetAzureDevOpsSettings(this WebApplicationBuilder builder)
        {
            return new AzureDevOps.ConnectionSettings()
            {
                Url = new Uri(builder.Configuration["AzureDevOps:Url"] ?? throw new InvalidOperationException("Missing AzureDevOps:Url in settings")),
                PersonalAccessToken = builder.Configuration["AzureDevOps:PersonalAccessToken"] ?? string.Empty
            };
        }

    }
}
