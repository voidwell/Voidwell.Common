using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Voidwell.Common.Authentication.AuthenticatedHttpClient;

namespace Voidwell.Common.Authentication;

/// <summary>
/// Extension methods for authenticating an <see cref="HttpClient"/> with bearer tokens.
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Authenticates the client with client-credentials bearer tokens. A <c>401 Unauthorized</c> response
    /// invalidates the rejected token and the request is resent once with a fresh one.
    /// </summary>
    /// <param name="builder">The HTTP client builder. Its name identifies the client's options and token cache, so it must be unique.</param>
    /// <param name="configure">Configures the token options. Runs when the options are first resolved.</param>
    /// <returns>The same <paramref name="builder"/>.</returns>
    public static IHttpClientBuilder AddTokenHandler(this IHttpClientBuilder builder, Action<IServiceProvider, AuthenticatedHttpClientOptions> configure)
    {
        var name = builder.Name;

        builder.Services.AddTokenServices();
        builder.Services.TryAddKeyedSingleton<ITokenManager, TokenManager>(name);
        builder.Services.AddOptions<AuthenticatedHttpClientOptions>(name)
            .Configure<IServiceProvider>((options, serviceProvider) => configure(serviceProvider, options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The retry must sit outside the auth handler so the resent request gets a new token.
        builder.AddResilienceHandler($"{name}.UnauthorizedRetry", (pipeline, context) =>
        {
            var tokenManager = context.ServiceProvider.GetRequiredKeyedService<ITokenManager>(name);
            pipeline.AddRetry(UnauthorizedRetry.CreateOptions(tokenManager));
        });

        return builder.AddHttpMessageHandler(sp =>
        {
            var tokenManager = sp.GetRequiredKeyedService<ITokenManager>(name);
            return new AuthenticatedHttpMessageHandler(tokenManager);
        });
    }

    private static void AddTokenServices(this IServiceCollection services)
    {
        services.AddHttpClient(ClientTokenService.HttpClientName);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClientTokenService, ClientTokenService>();
    }
}
