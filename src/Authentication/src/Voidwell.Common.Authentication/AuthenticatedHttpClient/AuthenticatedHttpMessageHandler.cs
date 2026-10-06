using System.Net.Http.Headers;

namespace Voidwell.Common.Authentication.AuthenticatedHttpClient;

/// <summary>Adds the bearer token to outgoing requests</summary>
internal sealed class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly ITokenManager _tokenManager;

    public AuthenticatedHttpMessageHandler(ITokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenManager.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
