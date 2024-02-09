using Satori.Components;
using ConnectionSettings = Satori.AppServices.Models.ConnectionSettings;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        SetAzureDevOpsConnectionSettings(builder);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }

    private static void SetAzureDevOpsConnectionSettings(WebApplicationBuilder builder)
    {
        ConnectionSettings = new()
        {
            AzureDevOps = GetAzureDevOpsSettings(builder)
        };
    }

    private static Satori.AzureDevOps.ConnectionSettings GetAzureDevOpsSettings(WebApplicationBuilder builder)
    {
        return new Satori.AzureDevOps.ConnectionSettings()
        {
            Url = new Uri(builder.Configuration["AzureDevOps:Url"] ?? throw new InvalidOperationException("Missing AzureDevOps:Url in settings")),
            PersonalAccessToken = builder.Configuration["AzureDevOps:PersonalAccessToken"] ?? string.Empty
        };
    }

    internal static ConnectionSettings ConnectionSettings { get; private set; }
}