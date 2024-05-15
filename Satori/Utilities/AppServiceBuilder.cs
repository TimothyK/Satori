using Satori.AppServices.Services;
using Satori.AzureDevOps;
using Satori.Kimai;
using Satori.TimeServices;
using Serilog;

namespace Satori.Utilities;

internal static class AppServiceBuilder
{
    public static WebApplicationBuilder AddAppServices(this WebApplicationBuilder builder)
    {
        var settings = builder.GetConnectionSettings();
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<SprintBoardService>();
        builder.Services.AddSingleton<PullRequestService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton(settings.AzureDevOps);
        builder.Services.AddSingleton<IAzureDevOpsServer, AzureDevOpsServer>();
        builder.Services.AddSingleton(settings.Kimai);
        builder.Services.AddSingleton<IKimaiServer, KimaiServer>();
        builder.Services.AddSingleton<ITimeServer, TimeServer>();
        var loggerFactory = new LoggerFactory().AddSerilog();
        builder.Services.AddSingleton(loggerFactory);

        return builder;
    }

    private static AppServices.Models.ConnectionSettings GetConnectionSettings(this WebApplicationBuilder builder)
    {
        return new()
        {
            AzureDevOps = builder.GetAzureDevOpsSettings(),
            Kimai = builder.GetKimaiSettings(),
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
    private static Kimai.ConnectionSettings GetKimaiSettings(this WebApplicationBuilder builder)
    {
        return new Kimai.ConnectionSettings()
        {
            Url = new Uri(builder.Configuration["Kimai:Url"] ?? throw new InvalidOperationException("Missing Kimai:Url in settings")),
            UserName = builder.Configuration["Kimai:User"] ?? string.Empty,
            Token = builder.Configuration["Kimai:Token"] ?? string.Empty,
        };
    }

}