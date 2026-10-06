namespace Voidwell.Common.Authentication.AuthenticatedHttpClient;

/// <summary>
/// Provides cached bearer tokens for a single authenticated HTTP client.
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// Gets a valid access token, requesting a new one if the cached token is missing or about to expire.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The access token.</returns>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards the cached token if it is still <paramref name="rejectedToken"/>.
    /// </summary>
    /// <param name="rejectedToken">The token that was rejected by the server.</param>
    void Invalidate(string rejectedToken);
}
