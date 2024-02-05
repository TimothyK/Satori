using Satori.AzureDevOps;
using Satori.Components;

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
        AzureDevOpsConnectionSettings = GetConnectionSettings(builder);
    }

    private static ConnectionSettings GetConnectionSettings(WebApplicationBuilder builder)
    {
        return new ConnectionSettings()
        {
            Url = new Uri(builder.Configuration["AzureDevOps:Url"] ?? throw new InvalidOperationException("Missing AzureDevOps:Url in settings")),
            PersonalAccessToken = builder.Configuration["AzureDevOps:PersonalAccessToken"] ?? string.Empty
        };
    }

    internal static ConnectionSettings AzureDevOpsConnectionSettings { get; private set; }
}