using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Voidwell.Common.Logging;

/// <summary>
/// Extension methods for logging HTTP requests through Serilog.
/// </summary>
public static class RequestLoggingExtensions
{
    /// <summary>
    /// Logs one summary event per HTTP request, enriched with the client IP address and user agent.
    /// Add this after <c>UseForwardedHeaders</c> so the client IP reflects the original requester rather than the proxy,
    /// and set <c>Microsoft.AspNetCore.Hosting.Diagnostics</c> to <c>Warning</c> to drop the default per-request lines.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <paramref name="app"/>.</returns>
    public static IApplicationBuilder UseApplicationRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };
        });
    }
}
