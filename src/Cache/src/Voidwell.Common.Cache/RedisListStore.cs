using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Voidwell.Common.Cache;

/// <summary>
/// Stores lists as Redis sets. Failures are swallowed so that a Redis outage never breaks callers;
/// reads report a miss (<c>null</c>) instead.
/// </summary>
internal sealed class RedisListStore(IOptions<CacheOptions> options) : IListStore, IDisposable
{
    private readonly CacheOptions _options = options.Value;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private static readonly TimeSpan _retryCooldown = TimeSpan.FromSeconds(15);

    private ConnectionMultiplexer? _redis;
    private long _retryAfter;

    public async Task AddAsync(string key, string item)
    {
        try
        {
            var db = await GetDatabaseAsync();
            await db.SetAddAsync(FormatKey(key), item);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public async Task RemoveAsync(string key, string item)
    {
        try
        {
            var db = await GetDatabaseAsync();
            await db.SetRemoveAsync(FormatKey(key), item);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public async Task ClearAsync(string key)
    {
        try
        {
            var db = await GetDatabaseAsync();
            await db.KeyDeleteAsync(FormatKey(key));
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public async Task<IReadOnlyCollection<string>?> GetAsync(string key)
    {
        try
        {
            var db = await GetDatabaseAsync();
            var members = await db.SetMembersAsync(FormatKey(key));
            return members.Select(m => (string)m!).ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<long?> GetLengthAsync(string key)
    {
        try
        {
            var db = await GetDatabaseAsync();
            return await db.SetLengthAsync(FormatKey(key));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<IDatabase> GetDatabaseAsync()
    {
        if (_redis is { IsConnected: true })
        {
            return _redis.GetDatabase();
        }

        ThrowIfCoolingDown();

        await _connectionLock.WaitAsync();
        try
        {
            if (_redis is { IsConnected: true })
            {
                return _redis.GetDatabase();
            }

            // Callers queued behind a failed attempt must not each pay for another one
            ThrowIfCoolingDown();

            try
            {
                // An existing multiplexer reconnects on its own, so only create one when there is none
                _redis ??= await ConnectionMultiplexer.ConnectAsync(_options.RedisConfiguration!);

                if (!_redis.IsConnected)
                {
                    throw new InvalidOperationException("Redis is not connected.");
                }

                return _redis.GetDatabase();
            }
            catch (Exception)
            {
                Volatile.Write(ref _retryAfter, Environment.TickCount64 + (long)_retryCooldown.TotalMilliseconds);
                throw;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    // While Redis is down, fail immediately instead of making every caller wait out a connection or command timeout
    private void ThrowIfCoolingDown()
    {
        if (Environment.TickCount64 < Volatile.Read(ref _retryAfter))
        {
            throw new InvalidOperationException("Redis is unavailable; the connection will be retried shortly.");
        }
    }

    private string FormatKey(string key)
    {
        return string.IsNullOrWhiteSpace(_options.KeyPrefix) ? key : $"{_options.KeyPrefix}_{key}";
    }

    public void Dispose()
    {
        _redis?.Dispose();
        _connectionLock.Dispose();
    }
}
