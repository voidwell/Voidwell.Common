namespace Voidwell.Common.Authentication.AuthenticatedHttpClient;

internal interface IClientTokenService
{
    Task<TokenResponse> RequestTokenAsync(string name, CancellationToken cancellationToken);
}
