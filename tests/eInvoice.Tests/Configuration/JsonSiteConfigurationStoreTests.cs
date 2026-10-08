using System.Text.Json;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Configuration;
using eInvoice.Tests.Misa;

namespace eInvoice.Tests.Configuration;

public sealed class JsonSiteConfigurationStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "einvoice-sites-" + Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public async Task Selects_exact_site_and_reads_updates_without_restarting_or_cross_tenant_cache()
    {
        var first = MisaTestData.Site.Site;
        var second = MisaTestData.Site.Site;
        second.Id = Guid.NewGuid();
        second.Username = "second-synthetic-user";
        await Write(first, second);
        var store = new JsonSiteConfigurationStore(_path);
        Assert.Equal(first.Username, (await store.GetSiteAsync(first.Id!.Value)).Site.Username);
        Assert.Equal(second.Username, (await store.GetSiteAsync(second.Id!.Value)).Site.Username);
        first.Password = "rotated-synthetic-secret";
        await Write(first, second);
        Assert.Equal(first.Password, (await store.GetSiteAsync(first.Id.Value)).Site.Password);
    }

    [Fact]
    public async Task Missing_file_and_unknown_site_are_distinct_and_do_not_fall_back()
    {
        var store = new JsonSiteConfigurationStore(_path);
        Assert.Equal(SiteStoreFailure.NotConfigured, (await Assert.ThrowsAsync<SiteStoreException>(() => store.GetSiteAsync(MisaTestData.SiteId))).Failure);
        await Write(MisaTestData.Site.Site);
        Assert.Equal(SiteStoreFailure.NotFound, (await Assert.ThrowsAsync<SiteStoreException>(() => store.GetSiteAsync(Guid.NewGuid()))).Failure);
    }

    [Theory]
    [InlineData("not-json/synthetic-secret")]
    [InlineData("{}")]
    [InlineData("{\"sites\":[null]}")]
    [InlineData("{\"sites\":[{\"site\":{\"id\":null}}]}")]
    [InlineData("{\"sites\":[{\"site\":{\"id\":\"00000000-0000-0000-0000-000000000000\"}}]}")]
    [InlineData("{\"sites\":[],\"unknown\":\"synthetic-secret\"}")]
    public async Task Invalid_file_fails_closed_without_including_file_contents_in_exception(string content)
    {
        await File.WriteAllTextAsync(_path, content);
        var error = await Assert.ThrowsAsync<SiteStoreException>(() => new JsonSiteConfigurationStore(_path).GetSiteAsync(MisaTestData.SiteId));
        Assert.Equal(SiteStoreFailure.InvalidData, error.Failure);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.DoesNotContain(_path, error.ToString());
    }

    [Fact]
    public async Task Duplicate_site_ids_are_rejected_instead_of_selecting_arbitrary_credentials()
    {
        await Write(MisaTestData.Site.Site, MisaTestData.Site.Site);
        var error = await Assert.ThrowsAsync<SiteStoreException>(() => new JsonSiteConfigurationStore(_path).GetSiteAsync(MisaTestData.SiteId));
        Assert.Equal(SiteStoreFailure.InvalidData, error.Failure);
    }

    [Fact]
    public async Task Caller_cancellation_is_preserved()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new JsonSiteConfigurationStore(_path).GetSiteAsync(MisaTestData.SiteId, cancellation.Token));
    }

    private Task Write(params SiteConfiguration[] sites) => File.WriteAllTextAsync(_path, JsonSerializer.Serialize(
        new { sites = sites.Select(site => new SiteInfo { Site = site }) }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    public void Dispose() { if (File.Exists(_path)) File.Delete(_path); }
}
