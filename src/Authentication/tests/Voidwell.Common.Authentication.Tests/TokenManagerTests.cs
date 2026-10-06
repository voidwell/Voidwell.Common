using Voidwell.Common.Authentication.AuthenticatedHttpClient;

namespace Voidwell.Common.Authentication.Tests;

public class TokenManagerTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly FakeClientTokenService _tokenService = new();
    private readonly TokenManager _subject;

    public TokenManagerTests()
    {
        _subject = new TokenManager("test", _time, _tokenService);
    }

    [Fact]
    public async Task GetTokenAsync_ReusesTheCachedToken()
    {
        var first = await _subject.GetTokenAsync(TestContext.Current.CancellationToken);
        var second = await _subject.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("token-1", first);
        Assert.Equal("token-1", second);
        Assert.Equal(1, _tokenService.Requests);
    }

    [Fact]
    public async Task GetTokenAsync_RequestsANewToken_OnceTheCachedOneIsAboutToExpire()
    {
        await _subject.GetTokenAsync(TestContext.Current.CancellationToken);

        // The token lives 300s and is refreshed 30s early
        _time.Advance(TimeSpan.FromSeconds(271));
        var token = await _subject.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("token-2", token);
    }

    [Fact]
    public async Task Invalidate_ClearsTheRejectedToken()
    {
        var rejected = await _subject.GetTokenAsync(TestContext.Current.CancellationToken);

        _subject.Invalidate(rejected);
        var token = await _subject.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("token-2", token);
    }

    [Fact]
    public async Task Invalidate_KeepsTheToken_WhenItWasAlreadyReplaced()
    {
        var current = await _subject.GetTokenAsync(TestContext.Current.CancellationToken);

        _subject.Invalidate("some-older-token");
        var token = await _subject.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(current, token);
        Assert.Equal(1, _tokenService.Requests);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class FakeClientTokenService : IClientTokenService
    {
        public int Requests { get; private set; }

        public Task<TokenResponse> RequestTokenAsync(string name, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new TokenResponse { AccessToken = $"token-{Requests}", ExpiresIn = 300 });
        }
    }
}
