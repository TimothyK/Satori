using Satori.AppServices.Models;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Abstractions;
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
        builder.Services.AddScoped<IConnectionSettingsStore, ConnectionSettingsStore>();

        builder.Services.AddSingleton<HttpClient>();
        
        builder.Services.AddSingleton<SprintBoardService>();
        builder.Services.AddSingleton<PullRequestService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton<StandUpService>();
        
        builder.Services.AddSingleton(settings.AzureDevOps);
        builder.Services.AddSingleton<IAzureDevOpsServer, AzureDevOpsServer>();
        
        builder.Services.AddSingleton(settings.Kimai);
        builder.Services.AddSingleton<IKimaiServer, KimaiServer>();

        builder.Services.AddSingleton(settings.MessageQueue);
        builder.Services.AddSingleton<ITaskAdjustmentExporter, TaskAdjustmentExporter>();
        builder.Services.AddSingleton<IDailyActivityExporter, DailyActivityExporter>();

        builder.Services.AddSingleton<CompletedWorkService>();
        builder.Services.AddSingleton<TaskAdjustmentImporter>();
        
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
            MessageQueue = builder.GetMessageQueueSettings(),
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

    private static MessageQueues.ConnectionSettings GetMessageQueueSettings(this WebApplicationBuilder builder)
    {
        return new MessageQueues.ConnectionSettings()
        {
            HostName = builder.Configuration["MessageQueue:HostName"] ?? throw new InvalidOperationException("Missing MessageQueue:HostName in settings"),
            Port = int.Parse(builder.Configuration["MessageQueue:Port"] ?? "0"),
            Path = builder.Configuration["MessageQueue:Path"] ?? string.Empty,
            UserName = builder.Configuration["MessageQueue:UserName"] ?? string.Empty,
            Password = builder.Configuration["MessageQueue:Password"] ?? string.Empty,
        };
    }

}