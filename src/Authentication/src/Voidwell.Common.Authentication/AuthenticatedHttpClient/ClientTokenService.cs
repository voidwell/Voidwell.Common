using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Voidwell.Common.Authentication.AuthenticatedHttpClient;

internal sealed class ClientTokenService : IClientTokenService
{
    internal const string HttpClientName = "Voidwell.Common.Authentication.TokenService";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AuthenticatedHttpClientOptions> _optionsMonitor;

    public ClientTokenService(IHttpClientFactory httpClientFactory, IOptionsMonitor<AuthenticatedHttpClientOptions> optionsMonitor)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<TokenResponse> RequestTokenAsync(string name, CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.Get(name);
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenServiceAddress)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["scope"] = string.Join(' ', options.ClientScopes)
            })
        };

        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Token request at '{options.TokenServiceAddress}' for client '{name}' failed with status {(int)response.StatusCode}: {content}",
                inner: null,
                response.StatusCode);
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        if (token is null || string.IsNullOrEmpty(token.AccessToken))
        {
            throw new InvalidOperationException("The token service returned no access token.");
        }

        return token;
    }
}
