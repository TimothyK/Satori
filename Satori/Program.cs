using Blazored.LocalStorage;
using Satori.AppServices.Services;
using Satori.Components;
using Satori.Utilities;
using Serilog;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace Satori;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        Log.Logger = new LoggerConfiguration()
            .WriteToSatoriSinks(builder)
            .CreateLogger();
        Log.Logger.Information("Starting Satori");

        builder.Logging.AddSerilog();

        var loggerFactory = new LoggerFactory().AddSerilog();
        builder.Services.AddSingleton(loggerFactory);

        builder.Services.AddConnectionSettingsFromConfig(builder);
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddHotKeys2();
        
        builder.Services.AddSatoriServices();
        
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

        App = app;

        app.Run();
    }

    public static IServiceProvider Services => 
        App?.Services 
        ?? throw new InvalidOperationException("Services are not available until web application is initialized.");

    private static WebApplication? App { get; set; }
}