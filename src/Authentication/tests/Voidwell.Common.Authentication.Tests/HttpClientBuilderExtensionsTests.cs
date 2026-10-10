using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Voidwell.Common.Authentication.AuthenticatedHttpClient;

namespace Voidwell.Common.Authentication.Tests;

public class HttpClientBuilderExtensionsTests
{
    private const string _clientName = "api";
    private const string _tokenAddress = "http://auth.test/token";

    [Fact]
    public async Task AddTokenHandler_SendsTheBearerToken_AndRequestsClientCredentials()
    {
        using var tokenEndpoint = new StubHandler(_ => Json("""{"access_token":"abc","expires_in":300}"""));
        using var api = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await using var provider = BuildProvider(tokenEndpoint, api, options =>
        {
            options.TokenServiceAddress = _tokenAddress;
            options.ClientId = "id";
            options.ClientSecret = "secret";
            options.ClientScopes = ["read", "write"];
        });

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(_clientName);
        using var response = await client.GetAsync("http://api.test/resource", TestContext.Current.CancellationToken);
        using var second = await client.GetAsync("http://api.test/resource", TestContext.Current.CancellationToken);

        Assert.Equal(["Bearer abc", "Bearer abc"], api.Authorizations);
        Assert.Single(tokenEndpoint.Bodies);
        Assert.Equal("grant_type=client_credentials&client_id=id&client_secret=secret&scope=read+write", tokenEndpoint.Bodies[0]);
    }

    [Fact]
    public async Task AddTokenHandler_FailsValidation_WhenOptionsAreIncomplete()
    {
        using var tokenEndpoint = new StubHandler(_ => Json("{}"));
        using var api = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await using var provider = BuildProvider(tokenEndpoint, api, options => options.TokenServiceAddress = _tokenAddress);

        var monitor = provider.GetRequiredService<IOptionsMonitor<AuthenticatedHttpClientOptions>>();

        Assert.Throws<OptionsValidationException>(() => monitor.Get(_clientName));
    }

    [Fact]
    public async Task AddTokenHandler_Throws_WhenTheTokenServiceReturnsNoToken()
    {
        using var tokenEndpoint = new StubHandler(_ => Json("""{"access_token":"","expires_in":300}"""));
        using var api = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await using var provider = BuildProvider(tokenEndpoint, api, options =>
        {
            options.TokenServiceAddress = _tokenAddress;
            options.ClientId = "id";
            options.ClientSecret = "secret";
        });

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(_clientName);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("http://api.test/resource", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddTokenHandler_Throws_WithTheResponseContent_WhenTheTokenServiceFails()
    {
        using var tokenEndpoint = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"invalid_client","error_description":"bad secret"}""", Encoding.UTF8, "application/json")
        });
        using var api = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await using var provider = BuildProvider(tokenEndpoint, api, ConfigureValid);

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(_clientName);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("http://api.test/resource", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains(_tokenAddress, exception.Message);
        Assert.Contains("invalid_client", exception.Message);
        Assert.Contains("bad secret", exception.Message);
    }

    [Fact]
    public async Task AddTokenHandler_InvalidatesTheToken_AndResendsTheRequest_OnUnauthorized()
    {
        var tokens = 0;
        using var tokenEndpoint = new StubHandler(_ => Json($$"""{"access_token":"token-{{++tokens}}","expires_in":300}"""));
        using var api = new StubHandler(request => new HttpResponseMessage(
            request.Headers.Authorization?.Parameter == "token-1" ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));

        await using var provider = BuildProvider(tokenEndpoint, api, ConfigureValid);

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(_clientName);
        using var response = await client.GetAsync("http://api.test/resource", TestContext.Current.CancellationToken);
        using var next = await client.GetAsync("http://api.test/resource", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Bearer token-1", "Bearer token-2", "Bearer token-2"], api.Authorizations);
        Assert.Equal(2, tokenEndpoint.Bodies.Count);
    }

    [Fact]
    public async Task AddTokenHandler_ResendsOnlyOnce_WhenTheNewTokenIsAlsoRejected()
    {
        var tokens = 0;
        using var tokenEndpoint = new StubHandler(_ => Json($$"""{"access_token":"token-{{++tokens}}","expires_in":300}"""));
        using var api = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await using var provider = BuildProvider(tokenEndpoint, api, ConfigureValid);

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(_clientName);
        using var response = await client.GetAsync("http://api.test/resource", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(["Bearer token-1", "Bearer token-2"], api.Authorizations);
    }

    private static void ConfigureValid(AuthenticatedHttpClientOptions options)
    {
        options.TokenServiceAddress = _tokenAddress;
        options.ClientId = "id";
        options.ClientSecret = "secret";
    }

    private static ServiceProvider BuildProvider(StubHandler tokenEndpoint, StubHandler api, Action<AuthenticatedHttpClientOptions> configure)
    {
        var services = new ServiceCollection();

        services.AddHttpClient(_clientName)
            .ConfigurePrimaryHttpMessageHandler(() => api)
            .AddTokenHandler((_, options) => configure(options));

        services.AddHttpClient(ClientTokenService.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);

        return services.BuildServiceProvider();
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string?> Authorizations { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorizations.Add(request.Headers.Authorization?.ToString());

            if (request.Content is not null)
            {
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            // Real handlers attach the request to the response.
            var response = respond(request);
            response.RequestMessage = request;
            return response;
        }
    }
}
