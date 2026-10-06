using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Voidwell.Common.Authentication.ServiceAuthentication;

namespace Voidwell.Common.Authentication.Tests;

public class ServiceAuthenticationExtensionsTests
{
    private const string _scheme = "service";
    private const string _introspectionScheme = "introspection";

    [Fact]
    public async Task Jwt_RegistersOnlyJwtBearer()
    {
        await using var provider = BuildProvider(SupportedTokens.Jwt);
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.Equal(typeof(JwtBearerHandler), (await schemes.GetSchemeAsync(_scheme))!.HandlerType);
        Assert.Null(await schemes.GetSchemeAsync(_introspectionScheme));
    }

    [Fact]
    public async Task Reference_RegistersOnlyIntrospection_UnderTheGivenScheme()
    {
        await using var provider = BuildProvider(SupportedTokens.Reference);
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.Equal(typeof(OAuth2IntrospectionHandler), (await schemes.GetSchemeAsync(_scheme))!.HandlerType);
        Assert.Null(await schemes.GetSchemeAsync(_introspectionScheme));
    }

    [Fact]
    public async Task Both_RegistersJwtBearerAndIntrospection()
    {
        await using var provider = BuildProvider(SupportedTokens.Both);
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.Equal(typeof(JwtBearerHandler), (await schemes.GetSchemeAsync(_scheme))!.HandlerType);
        Assert.Equal(typeof(OAuth2IntrospectionHandler), (await schemes.GetSchemeAsync(_introspectionScheme))!.HandlerType);
    }

    [Fact]
    public async Task Jwt_MapsOptions()
    {
        await using var provider = BuildProvider(SupportedTokens.Jwt, options =>
        {
            options.Audience = "my-api";
            options.RoleClaimType = "role";
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
        });

        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(_scheme);

        Assert.Equal("https://auth.test", jwt.Authority);
        Assert.Equal("my-api", jwt.Audience);
        Assert.Equal("role", jwt.TokenValidationParameters.RoleClaimType);
        Assert.True(jwt.TokenValidationParameters.ValidateAudience);
        Assert.True(jwt.SaveToken);
        Assert.False(jwt.RequireHttpsMetadata);
        Assert.False(jwt.MapInboundClaims);
        Assert.Null(jwt.ForwardDefaultSelector);
    }

    [Fact]
    public async Task Jwt_WithoutAudience_DoesNotValidateAudience()
    {
        await using var provider = BuildProvider(SupportedTokens.Jwt);

        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(_scheme);

        Assert.False(jwt.TokenValidationParameters.ValidateAudience);
    }

    [Fact]
    public async Task Reference_MapsOptions()
    {
        await using var provider = BuildProvider(SupportedTokens.Reference, options =>
        {
            options.ClientId = "api";
            options.ClientSecret = "secret";
            options.SaveToken = true;
            options.EnableCaching = true;
            options.CacheDuration = TimeSpan.FromMinutes(2);
            options.NameClaimType = "name";
            options.RoleClaimType = "role";
        });

        var introspection = provider.GetRequiredService<IOptionsMonitor<OAuth2IntrospectionOptions>>().Get(_scheme);

        Assert.Equal("https://auth.test", introspection.Authority);
        Assert.Equal("api", introspection.ClientId);
        Assert.Equal("secret", introspection.ClientSecret);
        Assert.True(introspection.SaveToken);
        Assert.Equal(HybridCacheEntryFlags.None, introspection.SetCacheEntryFlags);
        Assert.Equal(TimeSpan.FromMinutes(2), introspection.CacheDuration);
        Assert.Equal("name", introspection.NameClaimType);
        Assert.Equal("role", introspection.RoleClaimType);
    }

    [Fact]
    public async Task Reference_RegistersTheHybridCacheItNeeds()
    {
        await using var provider = BuildProvider(SupportedTokens.Reference);

        Assert.NotNull(provider.GetService<HybridCache>());
    }

    [Fact]
    public async Task Reference_WithCachingDisabled_DisablesBothCacheLayers()
    {
        await using var provider = BuildProvider(SupportedTokens.Reference, options => options.EnableCaching = false);

        var introspection = provider.GetRequiredService<IOptionsMonitor<OAuth2IntrospectionOptions>>().Get(_scheme);

        Assert.Equal(HybridCacheEntryFlags.DisableLocalCache | HybridCacheEntryFlags.DisableDistributedCache, introspection.SetCacheEntryFlags);
    }

    [Fact]
    public async Task Reference_WithoutClaimTypes_KeepsTheLibraryDefaults()
    {
        await using var provider = BuildProvider(SupportedTokens.Reference);

        var introspection = provider.GetRequiredService<IOptionsMonitor<OAuth2IntrospectionOptions>>().Get(_scheme);

        Assert.False(string.IsNullOrEmpty(introspection.NameClaimType));
        Assert.False(string.IsNullOrEmpty(introspection.RoleClaimType));
    }

    [Theory]
    [InlineData("Bearer 0123456789abcdef", _introspectionScheme)]
    [InlineData("bearer 0123456789abcdef", _introspectionScheme)]
    [InlineData("Bearer aaaa.bbbb.cccc", null)]
    [InlineData("Basic 0123456789abcdef", null)]
    [InlineData("Bearer", null)]
    [InlineData("Bearer a b", null)]
    [InlineData("", null)]
    public async Task Both_ForwardsOnlyOpaqueBearerTokensToIntrospection(string authorization, string? expectedScheme)
    {
        await using var provider = BuildProvider(SupportedTokens.Both);
        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(_scheme);
        var context = new DefaultHttpContext();
        if (authorization.Length > 0)
        {
            context.Request.Headers.Authorization = authorization;
        }

        Assert.NotNull(jwt.ForwardDefaultSelector);
        Assert.Equal(expectedScheme, jwt.ForwardDefaultSelector(context));
    }

    private static ServiceProvider BuildProvider(SupportedTokens supportedTokens, Action<ServiceAuthenticationOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication().AddServiceAuthentication(_scheme, options =>
        {
            options.Authority = "https://auth.test";
            options.SupportedTokens = supportedTokens;
            configure?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }
}
