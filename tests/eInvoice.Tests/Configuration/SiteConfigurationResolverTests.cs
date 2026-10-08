using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;

namespace eInvoice.Tests.Configuration;

public class SiteConfigurationResolverTests
{
    [Fact]
    public async Task Supplied_site_and_extra_fields_take_precedence_without_admin_call()
    {
        var admin = new RecordingAdmin();
        var resolver = new SiteConfigurationResolver(admin);
        var site = new SiteConfiguration { Id = Guid.NewGuid(), Partner = 6 };
        var result = await resolver.ResolveAsync(new SiteInfo
        {
            Site = site,
            ExtraDataMap = [new ExtraFieldMapping { Code = "arrival", ThirdPartyCode = "CheckIn" }],
        }, Guid.NewGuid());
        Assert.Same(site, result.Site);
        Assert.Equal("CheckIn", Assert.Single(result.ExtraFieldMappings).ThirdPartyCode);
        Assert.Null(admin.CalledId);
    }

    [Fact]
    public async Task Missing_request_configuration_uses_admin_site_id_and_cancellation()
    {
        var admin = new RecordingAdmin();
        var siteId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var result = await new SiteConfigurationResolver(admin).ResolveAsync(null, siteId, cancellation.Token);
        Assert.Equal(siteId, admin.CalledId);
        Assert.Equal(cancellation.Token, admin.CalledToken);
        Assert.Equal(siteId, result.Site.Id);
    }

    [Fact]
    public async Task Invalid_supplied_configuration_does_not_fall_back_to_admin()
    {
        var admin = new RecordingAdmin();
        await Assert.ThrowsAsync<SiteConfigurationException>(() =>
            new SiteConfigurationResolver(admin).ResolveAsync(new SiteInfo(), Guid.NewGuid()));
        Assert.Null(admin.CalledId);
    }

    [Fact]
    public async Task Missing_site_id_is_rejected_without_network_lookup() =>
        await Assert.ThrowsAsync<SiteConfigurationException>(() =>
            new SiteConfigurationResolver(new RecordingAdmin()).ResolveAsync(null, null));

    private sealed class RecordingAdmin : IAdminClient
    {
        public Guid? CalledId { get; private set; }
        public CancellationToken CalledToken { get; private set; }
        public Task<ResolvedSite> GetSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
        {
            CalledId = siteId;
            CalledToken = cancellationToken;
            return Task.FromResult(new ResolvedSite(new SiteConfiguration { Id = siteId }, []));
        }
    }
}
