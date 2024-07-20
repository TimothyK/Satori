namespace Satori.Utilities;

internal static class ConnectionSettingsFromConfigExtensions
{
    public static IServiceCollection AddConnectionSettingsFromConfig(this IServiceCollection services, WebApplicationBuilder builder)
    {
        var settings = builder.GetConnectionSettings();
        services.AddSingleton(settings);

        return services;
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