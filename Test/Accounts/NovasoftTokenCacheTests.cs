using Adapters.Accounts;

namespace Test.Accounts;

public sealed class NovasoftTokenCacheTests
{
    [Fact]
    public async Task ParallelRequestsPerformOneLoginAndCacheNeverExceedsFiveMinutes()
    {
        var clock = new AccountsClock();
        using var cache = new NovasoftTokenCache(clock);
        var calls = 0;
        async Task<(string, DateTimeOffset)> Login(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return ("token", clock.Now.AddHours(1));
        }

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => cache.GetAsync(Login, CancellationToken.None)));
        Assert.Equal(1, calls);
        clock.Now = clock.Now.AddMinutes(5);
        await cache.GetAsync(Login, CancellationToken.None);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ShortLivedTokenRefreshesThirtySecondsBeforeExpiration()
    {
        var clock = new AccountsClock();
        using var cache = new NovasoftTokenCache(clock);
        var calls = 0;
        Task<(string, DateTimeOffset)> Login(CancellationToken _)
        {
            calls++;
            return Task.FromResult(($"token{calls}", clock.Now.AddMinutes(1)));
        }

        Assert.Equal("token1", await cache.GetAsync(Login, CancellationToken.None));
        clock.Now = clock.Now.AddSeconds(29);
        Assert.Equal("token1", await cache.GetAsync(Login, CancellationToken.None));
        clock.Now = clock.Now.AddSeconds(1);
        Assert.Equal("token2", await cache.GetAsync(Login, CancellationToken.None));
    }

    [Fact]
    public async Task DelayedRejectionOfOldTokenCannotInvalidateNewToken()
    {
        var clock = new AccountsClock();
        using var cache = new NovasoftTokenCache(clock);
        Task<(string, DateTimeOffset)> Old(CancellationToken _) => Task.FromResult(("old", clock.Now.AddHours(1)));
        Task<(string, DateTimeOffset)> Current(CancellationToken _) => Task.FromResult(("current", clock.Now.AddHours(1)));

        await cache.GetAsync(Old, CancellationToken.None);
        clock.Now = clock.Now.AddMinutes(5);
        await cache.GetAsync(Current, CancellationToken.None);
        await cache.InvalidateAsync("old", CancellationToken.None);

        Assert.Equal("current", await cache.GetAsync(_ => throw new InvalidOperationException("Unexpected login"), CancellationToken.None));
    }
}
