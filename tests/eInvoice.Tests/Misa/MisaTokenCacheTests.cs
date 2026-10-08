using eInvoice.Providers.Misa;

namespace eInvoice.Tests.Misa;

public class MisaTokenCacheTests
{
    [Fact]
    public async Task Concurrent_requests_authenticate_once_and_expire_in_utc_with_margin()
    {
        var clock = new MutableClock();
        var cache = new MisaTokenCache(clock);
        var calls = 0;
        async Task<string> Authenticate(CancellationToken token)
        {
            var number = Interlocked.Increment(ref calls);
            await Task.Delay(10, token);
            return "token-" + number;
        }
        var tasks = Enumerable.Range(0, 20).Select(_ => cache.GetAsync(MisaTestData.Site.Site, false, "appid", Authenticate));
        Assert.All(await Task.WhenAll(tasks), token => Assert.Equal("token-1", token));
        Assert.Equal(1, calls);
        clock.Utc = clock.Utc.AddDays(7).AddMinutes(-6);
        Assert.Equal("token-1", await cache.GetAsync(MisaTestData.Site.Site, false, "appid", Authenticate));
        clock.Utc = clock.Utc.AddMinutes(1);
        Assert.Equal("token-2", await cache.GetAsync(MisaTestData.Site.Site, false, "appid", Authenticate));
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("site")]
    [InlineData("password")]
    [InlineData("username")]
    [InlineData("tax")]
    [InlineData("endpoint")]
    [InlineData("appid")]
    public async Task Credential_and_tenant_changes_do_not_reuse_tokens(string changed)
    {
        var cache = new MisaTokenCache(new MutableClock());
        var site = MisaTestData.Site.Site;
        var calls = 0;
        Task<string> Authenticate(CancellationToken _) => Task.FromResult("token-" + ++calls);
        Assert.Equal("token-1", await cache.GetAsync(site, false, "appid", Authenticate));
        switch (changed)
        {
            case "site": site.Id = Guid.NewGuid(); break;
            case "password": site.Password = "rotated-synthetic-secret"; break;
            case "username": site.Username = "other-user"; break;
            case "tax": site.TaxNumber = "OTHER-TAX"; break;
            case "endpoint": site.PartnerUrl += "other/"; break;
        }
        Assert.Equal("token-2", await cache.GetAsync(site, false, changed == "appid" ? "other-appid" : "appid", Authenticate));
    }

    [Fact]
    public async Task Failed_login_is_not_cached_and_receipt_tokens_are_not_reused()
    {
        var cache = new MisaTokenCache(new MutableClock());
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetAsync(MisaTestData.Site.Site, false, "appid", _ => throw new InvalidOperationException()));
        var calls = 0;
        Task<string> Authenticate(CancellationToken _) => Task.FromResult("token-" + ++calls);
        Assert.Equal("token-1", await cache.GetAsync(MisaTestData.Site.Site, false, "appid", Authenticate));
        Assert.Equal("token-2", await cache.GetAsync(MisaTestData.Site.Site, true, "appid", Authenticate));
        Assert.Equal("token-3", await cache.GetAsync(MisaTestData.Site.Site, true, "appid", Authenticate));
        Assert.Equal("token-1", await cache.GetAsync(MisaTestData.Site.Site, false, "appid", Authenticate));
    }

    [Fact]
    public async Task Invalidating_in_flight_login_does_not_restore_its_stale_token()
    {
        var cache = new MisaTokenCache(new MutableClock());
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = cache.GetAsync(MisaTestData.Site.Site, false, "appid", async _ =>
        {
            started.SetResult();
            await release.Task;
            return "stale";
        });
        await started.Task;
        cache.Invalidate(MisaTestData.Site.Site, false, "appid");
        release.SetResult();
        Assert.Equal("stale", await first);
        Assert.Equal("fresh", await cache.GetAsync(MisaTestData.Site.Site, false, "appid", _ => Task.FromResult("fresh")));
    }

    [Fact]
    public async Task Cancelling_a_waiter_does_not_cancel_the_authentication_owner()
    {
        var cache = new MisaTokenCache(new MutableClock());
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = cache.GetAsync(MisaTestData.Site.Site, false, "appid", async _ =>
        {
            started.SetResult();
            await release.Task;
            return "owner-token";
        });
        await started.Task;
        using var cancelled = new CancellationTokenSource();
        var waiter = cache.GetAsync(MisaTestData.Site.Site, false, "appid", _ => throw new InvalidOperationException(), cancelled.Token);
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
        release.SetResult();
        Assert.Equal("owner-token", await owner);
    }

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Utc { get; set; } = DateTimeOffset.Parse("2026-10-08T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => Utc;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("test-minus-7", TimeSpan.FromHours(-7), "test", "test");
    }
}
