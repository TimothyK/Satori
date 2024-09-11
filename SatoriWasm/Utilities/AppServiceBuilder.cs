using Satori.AppServices.Models;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Abstractions;
using Satori.AzureDevOps;
using Satori.Kimai;
using Satori.TimeServices;

namespace Satori.Utilities;

internal static class AppServiceBuilder
{
    public static IServiceCollection AddSatoriServices(this IServiceCollection services)
    {
        services.AddScoped<IConnectionSettingsStore, ConnectionSettingsStore>();

        services.AddScoped<HttpClient>();
        
        services.AddScoped<SprintBoardService>();
        services.AddScoped<PullRequestService>();
        services.AddScoped<UserService>();
        services.AddScoped<StandUpService>();
        
        services.AddScoped(serviceProvider => serviceProvider.GetRequiredService<IConnectionSettingsStore>().GetAzureDevOpsSettings());
        services.AddScoped<IAzureDevOpsServer, AzureDevOpsServer>();
        
        services.AddScoped(serviceProvider => serviceProvider.GetRequiredService<IConnectionSettingsStore>().GetKimaiSettings());
        services.AddScoped<IKimaiServer, KimaiServer>();

        services.AddScoped(serviceProvider => serviceProvider.GetRequiredService<IConnectionSettingsStore>().GetMessageQueueSettings());
        services.AddScoped<ITaskAdjustmentExporter, TaskAdjustmentExporter>();
        services.AddScoped<IDailyActivityExporter, DailyActivityExporter>();

        services.AddScoped<CompletedWorkService>();
        services.AddScoped<TaskAdjustmentImporter>();
        
        services.AddSingleton<ITimeServer, TimeServer>();

        return services;
    }


}