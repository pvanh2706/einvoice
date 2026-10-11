using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Minvoice.Mapping;
using eInvoice.Providers.Misa;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Minvoice;

public class MinvoiceMappingTests
{
    [Theory]
    [InlineData("service-site-a", "36342869-8a14-4c31-b7be-7f46f5a92b13")]
    [InlineData("service-site-b", "96443794-55bb-4c12-881f-9e6cc1aa66ba")]
    public void Pinned_service_charge_exceptions_keep_detail_total_and_first_rate(string fixture, string siteId)
    {
        var input = MisaTestData.Invoice(id: "33333333-3333-3333-3333-333333333333");
        input.Invoice!.SearchCode = "SEARCH-001"; input.Details![0].ServiceCharge = 17.25m; input.Details[0].ServiceRate = 7.5m;
        var model = JObject.FromObject(input); model["editmode"] = "1";
        var mapper = new MinvoiceRequestMapper();
        var actual = ProviderMapping.Run(() => JToken.Parse(mapper.convertInvoiceFormat(model, siteId)), "Minvoice");
        var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Minvoice", "Payloads", fixture + ".json")));
        Assert.True(JToken.DeepEquals(expected, actual), actual.ToString());
        Assert.Equal(17.25m, actual["data"]![0]!.Value<decimal>("tongphi_dichvu")); Assert.Equal(7.5m, actual["data"]![0]!.Value<decimal>("pt_phidichvu"));
    }

    [Fact]
    public async Task New_optional_provider_reference_preserves_legacy_misa_adjustment_fingerprint_and_replay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "eInvoice-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var request = MisaTestData.Invoice(); var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            // Legacy canonical shape before the optional field existed, derived by removing it explicitly.
            var node = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(request, json))!.AsObject(); node.Remove("originalProviderInvoiceCode");
            var body = node.ToJsonString(json);
            static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            var attempt = Hash(MisaTestData.SiteId + "|adjust|legacy-adjustment-001");
            var record = new { fingerprint = Hash(body), state = "completed", updatedAtUtc = "2026-10-09T00:00:00Z", result = new { confirmed = true } };
            await File.WriteAllTextAsync(Path.Combine(directory, attempt + ".json"), JsonSerializer.Serialize(record, json));
            var replay = await new MisaWriteJournal(directory, TimeProvider.System).ExecuteAsync(MisaTestData.SiteId, "adjust", "legacy-adjustment-001", request, [], () => throw new InvalidOperationException("Must not dispatch again"), default);
            Assert.True(replay.GetProperty("confirmed").GetBoolean());
        }
        finally
        {
            var allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "eInvoice-tests")) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(directory).StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe cleanup");
            Directory.Delete(directory, true);
        }
    }
}
