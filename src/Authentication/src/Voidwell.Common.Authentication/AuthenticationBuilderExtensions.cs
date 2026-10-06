using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Voidwell.Common.Authentication.ServiceAuthentication;

namespace Voidwell.Common.Authentication;

public enum SupportedTokens
{
    Both,
    Jwt,
    Reference
}

public static class ServiceAuthenticationExtensions
{
    private const string _introspectionScheme = "introspection";

    public static AuthenticationBuilder AddServiceAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<ServiceAuthenticationOptions> optionsAction)
    {
        var options = new ServiceAuthenticationOptions();
        optionsAction.Invoke(options);

        if (options.SupportedTokens != SupportedTokens.Reference)
        {
            builder.AddJwtBearer(authenticationScheme, o =>
            {
                o.Authority = options.Authority;
                o.Audience = options.Audience;
                o.SaveToken = options.SaveToken;
                o.RequireHttpsMetadata = options.RequireHttpsMetadata;
                o.BackchannelHttpHandler = options.BackchannelHttpHandler;
                o.MapInboundClaims = false;

                if (string.IsNullOrEmpty(options.Audience))
                {
                    o.TokenValidationParameters.ValidateAudience = false;
                }

                if (!string.IsNullOrEmpty(options.RoleClaimType))
                {
                    o.TokenValidationParameters.RoleClaimType = options.RoleClaimType;
                }

                if (options.SupportedTokens == SupportedTokens.Both)
                {
                    o.ForwardDefaultSelector = ForwardReferenceToken(_introspectionScheme);
                }
            });
        }

        if (options.SupportedTokens != SupportedTokens.Jwt)
        {
            builder.AddOAuth2Introspection(options.SupportedTokens == SupportedTokens.Reference ? authenticationScheme : _introspectionScheme, o =>
            {
                o.Authority = options.Authority;
                o.ClientId = options.ClientId;
                o.ClientSecret = options.ClientSecret;
                o.SaveToken = options.SaveToken;
                o.CacheDuration = options.CacheDuration;
                if (!options.EnableCaching)
                {
                    o.SetCacheEntryFlags = HybridCacheEntryFlags.DisableLocalCache | HybridCacheEntryFlags.DisableDistributedCache;
                }

                if (!string.IsNullOrEmpty(options.NameClaimType))
                {
                    o.NameClaimType = options.NameClaimType;
                }

                if (!string.IsNullOrEmpty(options.RoleClaimType))
                {
                    o.RoleClaimType = options.RoleClaimType;
                }
            });
        }

        return builder;
    }

    private static Func<HttpContext, string?> ForwardReferenceToken(string introspectionScheme)
    {
        return context =>
        {
            var (scheme, credential) = GetSchemeAndCredential(context);

            if (scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) && !credential.Contains('.'))
            {
                return introspectionScheme;
            }

            return null;
        };
    }

    private static (string, string) GetSchemeAndCredential(HttpContext context)
    {
        var header = context.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(header))
        {
            return ("", "");
        }

        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return ("", "");
        }

        return (parts[0], parts[1]);
    }
}
