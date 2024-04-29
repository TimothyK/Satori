using Serilog;

namespace Satori.Utilities;

internal static class LogBuilderExtensions
{
    public static LoggerConfiguration WriteToSatoriSinks(this LoggerConfiguration configuration, WebApplicationBuilder builder)
    {
        configuration
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "Satori")
            .Enrich.WithProcessId()
            .Enrich.WithThreadId();
            
        var seqUrl = builder.Configuration["Logging:Seq:Url"];
        if (!string.IsNullOrEmpty(seqUrl))
        {
            configuration.WriteTo.Seq(seqUrl);
        }

        var filePath = builder.Configuration["Logging:File:Path"];
        if (!string.IsNullOrEmpty(filePath))
        {
            configuration.WriteTo.File(Path.Combine(filePath, "Satori-.log")
                , outputTemplate: "{Level:u3} {Timestamp:HH:mm:ss.fff} {ProcessId}:{ThreadId} {Message:l}{NewLine}{SqlStatement}{Exception}"
                , fileSizeLimitBytes: 500 * 1024 * 1024 //500 MB
                , rollingInterval: RollingInterval.Day
                , rollOnFileSizeLimit: false
                , buffered: false
            );
        }
            
        return configuration;
    }
}