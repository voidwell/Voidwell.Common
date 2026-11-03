using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Hosting;
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
    /// The logger is also assigned to <see cref="Log.Logger"/>.
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
            loggerConfig.WriteTo.Console(new RenderedCompactJsonFormatter());
        }

        var logger = loggerConfig.CreateLogger();

        // Also set the static logger so Serilog middleware such as UseSerilogRequestLogging, which logs through Log.Logger, writes to the same sinks.
        Log.Logger = logger;

        // Required by Serilog request logging (UseSerilogRequestLogging / IDiagnosticContext).
        var diagnosticContext = new DiagnosticContext(logger);
        builder.Services.AddSingleton(diagnosticContext);
        builder.Services.AddSingleton<IDiagnosticContext>(diagnosticContext);

        builder.ClearProviders();
        builder.AddSerilog(logger);

        return builder;
    }
}
