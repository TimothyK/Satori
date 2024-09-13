using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Satori.AppServices.Services;
using Satori.Utilities;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace Satori;

public class Program
{
    public static readonly object TaskImporterLock = new();
    public static TaskAdjustmentImporter? TaskImporter { get; set; }

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddHotKeys2();
        
        builder.Services.AddSatoriServices();

        await builder.Build().RunAsync();
    }
}