using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Admin;

/// <summary>Calls the existing external admin contract; eInvoice naming does not rename its wire keys.</summary>
public sealed class AdminClient(HttpClient httpClient) : IAdminClient
{
    private static readonly JsonSerializerOptions ExternalJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ResolvedSite> GetSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        if (siteId == Guid.Empty) throw new ArgumentException("Site ID cannot be empty.", nameof(siteId));
        cancellationToken.ThrowIfCancellationRequested();
        if (httpClient.BaseAddress is null)
        {
            throw new AdminLookupException(AdminFailure.MissingConfiguration,
                "eInvoiceAdmin base address is not configured.");
        }

        try
        {
            // Existing admin expects SiteId and accepts charset=utf-16. Keep this external contract.
            using var request = new HttpRequestMessage(HttpMethod.Post, "openapi/oa02-site/detail")
            {
                Content = new StringContent(JsonSerializer.Serialize(new SiteDetailRequest(siteId)),
                    Encoding.Unicode, "application/json"),
            };
            request.Headers.Accept.ParseAdd("application/json");
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new AdminLookupException(AdminFailure.HttpError,
                    "eInvoiceAdmin returned an unsuccessful HTTP response.", response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var envelope = JsonSerializer.Deserialize<AdminEnvelope>(json, ExternalJson);
            if (envelope?.Data?.Site is null)
            {
                throw new AdminLookupException(AdminFailure.MissingSite,
                    "eInvoiceAdmin response contains no site configuration.");
            }

            return new ResolvedSite(envelope.Data.Site, envelope.Data.ExtraDataMap?.ToArray() ?? []);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            throw new AdminLookupException(AdminFailure.Timeout, "eInvoiceAdmin request timed out.", innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new AdminLookupException(AdminFailure.Connection,
                "Unable to connect to eInvoiceAdmin.", innerException: ex);
        }
        catch (JsonException ex)
        {
            throw new AdminLookupException(AdminFailure.InvalidResponse,
                "eInvoiceAdmin returned invalid JSON.", innerException: ex);
        }
    }

    private sealed record SiteDetailRequest([property: JsonPropertyName("SiteId")] Guid SiteId);
    private sealed class AdminEnvelope
    {
        public AdminSiteInfo? Data { get; set; }
    }
    private sealed class AdminSiteInfo
    {
        public SiteConfiguration? Site { get; set; }
        public List<ExtraFieldMapping>? ExtraDataMap { get; set; }
    }
}
