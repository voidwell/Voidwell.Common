using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Voidwell.Common.Configuration;

namespace Voidwell.Common.Swagger;

/// <summary>
/// Extension methods for adding Swagger to an API.
/// </summary>
public static class SwaggerExtensions
{
    private const string _bearerScheme = "Bearer";

    /// <summary>
    /// Adds the Swagger generator with a <c>v1</c> document titled after the application and an optional bearer scheme.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration, used to read <c>ApplicationName</c>.</param>
    /// <returns>The same <paramref name="services"/>.</returns>
    public static IServiceCollection AddApiSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationName = configuration.GetApplicationName();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = applicationName,
                Version = "v1"
            });

            // Authentication is optional: the scheme is only defined here and is applied per operation
            // by AuthorizeOperationFilter, so anonymous endpoints can be called without a token.
            options.AddSecurityDefinition(_bearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token for endpoints that require authorization. Not needed for public endpoints."
            });

            options.OperationFilter<AuthorizeOperationFilter>(_bearerScheme);
        });

        return services;
    }

    /// <summary>
    /// Serves the Swagger document and UI for the <c>v1</c> document added by <see cref="AddApiSwagger"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <paramref name="app"/>.</returns>
    public static IApplicationBuilder UseApiSwagger(this IApplicationBuilder app)
    {
        var applicationName = app.ApplicationServices.GetRequiredService<IConfiguration>().GetApplicationName();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{applicationName} v1");
        });

        return app;
    }
}
