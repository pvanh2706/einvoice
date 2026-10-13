using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Contracts;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Api;

public class CyberbillCompletionTests
{
    private static MisaApiTestFactory Factory(Func<MisaApiTestFactory.ProviderCall, HttpResponseMessage>? respond = null) => new()
    {
        Resolve = (_, _) => { var site = MisaTestData.Site; site.Site.Partner = 7; site.Site.PartnerUrl = "https://cyberbill.example.test/"; return site; },
        Respond = call => respond?.Invoke(call) ?? Json(call.Url.EndsWith("GetToken")
            ? "{\"result\":{\"access_token\":\"synthetic-token\"}}"
            : "{\"success\":true,\"result\":{\"maketqua\":\"01\",\"magiaodich\":\"TX-001\",\"sohoadon\":\"123\",\"macapcqt\":\"TAX\"}}"),
    };
    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };
    private static AdjustOrReplaceInvoiceRequest Input()
    {
        var input = MisaTestData.Invoice(); input.Invoice!.PaymentMethod = "3"; input.Invoice.PaymentMethodCode = "3"; input.Invoice.InvoiceNo = 1; input.OriginalInvoiceId = "OLD-001"; return input;
    }
    [Theory]
    [InlineData("create","create","GuiHoadonGoc")]
    [InlineData("change","create","GuiHoadonGoc")]
    [InlineData("replace","replace","GuiHoadonThayThe")]
    [InlineData("adjust","adjust","GuiHoadonDieuChinh")]
    [InlineData("publish-existing","sign","KyHoaDonHSM")]
    [InlineData("delete","delete","XoaHoaDon")]
    public async Task Completed_operations_match_independent_baseline_wire_fixtures(string route, string fixture, string endpoint)
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "cyber-complete-001");
        ISiteScopedRequest request = route switch
        {
            "publish-existing" => new PublishExistingInvoiceRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0 },
            "delete" => new DeleteInvoiceRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0 },
            _ => Input(),
        };
        var contract = route == "create" ? typeof(CreateInvoiceRequest) : request.GetType();
        using var response = await client.PostAsync("/api/eInvoice/" + route, MisaApiTestFactory.Body(request, contract));
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var expected = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Cyberbill", "Payloads", fixture + ".json")));
        var call = factory.Calls.Last(); Assert.EndsWith(endpoint, call.Url); Assert.True(JToken.DeepEquals(expected, JObject.Parse(call.Body!)), call.Body);
        if (route != "delete")
        {
            using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(route != "publish-existing", result.RootElement.GetProperty("data").GetProperty("requiresSigning").GetBoolean());
        }
        using var replay = await client.PostAsync("/api/eInvoice/" + route, MisaApiTestFactory.Body(request, contract));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode); Assert.Equal(2, factory.Calls.Count);
    }
    [Fact]
    public async Task Create_does_not_sign_and_explicit_sign_uses_returned_provider_code()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "cyber-create-001");
        using var create = await client.PostAsync("/api/eInvoice/create", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode); Assert.DoesNotContain(factory.Calls, c => c.Url.Contains("KyHoaDonHSM"));
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "cyber-sign-002");
        using var sign = await client.PostAsync("/api/eInvoice/publish-existing", MisaApiTestFactory.Body(new PublishExistingInvoiceRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0 }));
        Assert.Equal(HttpStatusCode.OK, sign.StatusCode); Assert.Single(factory.Calls, c => c.Url.Contains("KyHoaDonHSM"));
    }
    [Fact]
    public async Task Invalid_payment_code_fails_before_supplier_call_and_is_correctable_with_same_key()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "cyber-payment-001");
        var input = Input(); input.Invoice!.PaymentMethod = "TM/CK";
        using var invalid = await client.PostAsync("/api/eInvoice/create", MisaApiTestFactory.Body(input, typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode); Assert.Empty(factory.Calls);
        using var corrected = await client.PostAsync("/api/eInvoice/create", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
    }
}
