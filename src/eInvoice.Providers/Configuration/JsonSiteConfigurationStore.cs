using System.Text.Json;
using System.Text.Json.Serialization;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Configuration;

/// <summary>Reads an operator-owned file on each lookup; configuration changes do not require a restart.</summary>
public sealed class JsonSiteConfigurationStore(string path) : ISiteConfigurationStore
{
    private readonly string _path = Path.GetFullPath(path);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
    };

    public async Task<ResolvedSite> GetSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        if (siteId == Guid.Empty) throw new SiteConfigurationException("A site ID is required.");
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            // Open first, rather than File.Exists: missing files and permission errors have different classifications.
            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
            var file = await JsonSerializer.DeserializeAsync<SiteFile>(stream, Json, cancellationToken);
            if (file?.Sites is null || file.Sites.Any(item => item?.Site?.Id is null || item.Site.Id == Guid.Empty) ||
                file.Sites.Select(item => item!.Site!.Id).Distinct().Count() != file.Sites.Count)
                throw new SiteStoreException(SiteStoreFailure.InvalidData);
            var configuration = file.Sites.SingleOrDefault(item => item!.Site!.Id == siteId);
            if (configuration is null) throw new SiteStoreException(SiteStoreFailure.NotFound);
            return new ResolvedSite(configuration.Site!, configuration.ExtraDataMap?.ToArray() ?? []);
        }
        catch (FileNotFoundException) { throw new SiteStoreException(SiteStoreFailure.NotConfigured); }
        catch (DirectoryNotFoundException) { throw new SiteStoreException(SiteStoreFailure.NotConfigured); }
        catch (JsonException) { throw new SiteStoreException(SiteStoreFailure.InvalidData); }
        catch (IOException) { throw new SiteStoreException(SiteStoreFailure.Unavailable); }
        catch (UnauthorizedAccessException) { throw new SiteStoreException(SiteStoreFailure.Unavailable); }
        // Caller cancellation is propagated; no fallback to external admin or another site.
    }

    private sealed class SiteFile
    {
        public List<SiteInfo?>? Sites { get; set; }
    }
}
