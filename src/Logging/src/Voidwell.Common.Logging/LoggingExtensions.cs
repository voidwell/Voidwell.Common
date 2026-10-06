using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;
using Voidwell.Common.Configuration;

namespace Voidwell.Common.Logging;

/// <summary>
/// Extension methods for configuring Serilog-based logging on a host builder.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Replaces the default logging providers with a Serilog logger configured from the builder's configuration.
    /// Development uses a readable console format; other environments use compact JSON.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <paramref name="builder"/>.</returns>
    public static IHostApplicationBuilder AddApplicationLogging(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddApplicationLogging(builder.Environment, builder.Configuration);

        return builder;
    }

    public static ILoggingBuilder AddApplicationLogging(this ILoggingBuilder builder, IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", configuration.GetApplicationName());

        if (hostEnvironment.IsDevelopment())
        {
            loggerConfig.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}]{NewLine}{Message:lj}{NewLine}{Exception}");
        }
        else
        {
            loggerConfig.WriteTo.Console(new CompactJsonFormatter());
        }

        builder.ClearProviders();
        builder.AddSerilog(loggerConfig.CreateLogger());

        return builder;
    }
}
