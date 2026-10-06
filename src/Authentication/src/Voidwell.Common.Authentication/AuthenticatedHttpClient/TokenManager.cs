using Microsoft.Extensions.DependencyInjection;

namespace Voidwell.Common.Authentication.AuthenticatedHttpClient;

internal sealed class TokenManager : ITokenManager
{
    private static readonly TimeSpan _expiryBuffer = TimeSpan.FromSeconds(30);

    private readonly string _name;
    private readonly TimeProvider _timeProvider;
    private readonly IClientTokenService _clientTokenService;

    private readonly SemaphoreSlim _lock = new(1, 1);

    // Token and expiry are swapped as one object so lock-free readers never see a mismatched pair.
    private CachedToken? _cached;

    public TokenManager([ServiceKey] string name, TimeProvider timeProvider, IClientTokenService clientTokenService)
    {
        _name = name;
        _timeProvider = timeProvider;
        _clientTokenService = clientTokenService;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (TryGetCachedToken(out var cached))
        {
            return cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Another caller may have refreshed the token while we waited.
            if (TryGetCachedToken(out cached))
            {
                return cached;
            }

            var response = await _clientTokenService.RequestTokenAsync(_name, cancellationToken);

            Volatile.Write(ref _cached, new CachedToken(response.AccessToken, _timeProvider.GetUtcNow().AddSeconds(response.ExpiresIn)));

            return response.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool TryGetCachedToken(out string token)
    {
        var cached = Volatile.Read(ref _cached);
        if (cached is not null && _timeProvider.GetUtcNow() < cached.Expiration - _expiryBuffer)
        {
            token = cached.Value;
            return true;
        }

        token = string.Empty;
        return false;
    }

    public void Invalidate(string rejectedToken)
    {
        // Only clear it if nobody has already replaced the rejected token. Lock-free, so it never waits on a refresh in flight.
        var cached = Volatile.Read(ref _cached);
        if (cached?.Value == rejectedToken)
        {
            Interlocked.CompareExchange(ref _cached, null, cached);
        }
    }

    private sealed record CachedToken(string Value, DateTimeOffset Expiration);
}
