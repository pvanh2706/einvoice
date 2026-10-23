using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Vnpt;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Api;

public class RemainingProviderTests
{
    internal static ResolvedSite Site(int partner) => new(new SiteConfiguration
    {
        Id = MisaTestData.SiteId, Partner = partner, Name = "Synthetic hotel", TaxNumber = "TEST-TAX", Address = "Synthetic address",
        PartnerUrl = partner == 8 ? "https://fast.example.test/" : "https://vnpt.example.test/",
        PartnerUrl2 = partner == 8 ? "https://fast-soap.example.test/" : null,
        Username = partner == 8 ? "{\"clientC\":\"CLIENT\",\"proxyC\":\"PROXY\",\"user\":\"synthetic-user\",\"unitC\":\"UNIT\"}" : "synthetic-account",
        Username2 = partner == 8 ? "{\"clientC\":\"CLIENT2\",\"proxyC\":\"PROXY2\",\"user\":\"synthetic-user2\",\"unitC\":\"UNIT2\"}" : "synthetic-user",
        Password = "synthetic-secret", Password2 = "synthetic-secret2",
    }, []);
    internal static AdjustOrReplaceInvoiceRequest Input(bool mtt = false, int type = 0, string id = "INV-001")
    {
        var input = MisaTestData.Invoice(type, mtt ? 1 : 0, id);
        input.OriginalInvoiceId = "OLD-001"; input.Invoice!.TotalAmountInWords = "Synthetic amount";
        input.HotelExtra = new HotelInformation(); input.Invoice.HotelExtra = input.HotelExtra;
        return input;
    }
    private static ISiteScopedRequest Request(string route, bool mtt = false, int type = 0) => route switch
    {
        "health-check" => new ProviderHealthCheckRequest(), "form-serials" or "certificate" => new GetFormSerialsRequest(),
        "status" or "tax/send" => new SendToTaxAuthorityRequest { InvoiceType = type, InvoiceIds = ["INV-001"], FormNo = "1", Serial = mtt ? "C26MAA" : "C26TAA" },
        "search" => new SearchInvoiceRequest { InvoiceId = "INV-001", FormNo = "1" },
        "document" or "document/draft" => new DownloadPdfRequest { InvoiceId = "INV-001", SearchCode = "SEARCH", InvoiceType = type, Serial = mtt ? "C26MAA" : "C26TAA", FormNo = "1", InvoiceDate = "08/10/2026", ElectronicInvoiceType = mtt ? 1 : 0 },
        "publish-existing" => new PublishExistingInvoiceRequest { InvoiceId = "INV-001", InvoiceType = type, FormNo = "1", Serial = "C26TAA" },
        "void" => new VoidInvoiceRequest { InvoiceId = "INV-001", SearchCode = "SEARCH", InvoiceType = type, FormNo = "1", Serial = mtt ? "C26MAA" : "C26TAA", InvoiceDate = "08/10/2026", ElectronicInvoiceType = mtt ? 1 : 0, Reason = "Synthetic reason" },
        "delete" => new DeleteInvoiceRequest { InvoiceId = "INV-001", InvoiceType = type, FormNo = "1", Serial = "C26TAA" },
        "publish-batch" => new InvoiceBatchRequest { Invoices = [Input(mtt, type)] },
        _ => Input(mtt, type),
    };
    private static StringContent Body(string route, ISiteScopedRequest input) => MisaApiTestFactory.Body(input,
        route is "publish" or "draft" or "draft/edit" or "create" ? typeof(CreateInvoiceRequest) : input.GetType());
    public static IEnumerable<object[]> VnptCases()
    {
        foreach (var partner in new[] { 1, 5 })
        {
            var provider = partner == 1 ? ProviderId.Vnpt : ProviderId.VnptV5;
            var routes = new VnptInvoiceProvider(null!, new(), TimeProvider.System).Routes;
            if (partner == 5) routes = new VnptV5InvoiceProvider(null!, new(), TimeProvider.System).Routes;
            foreach (var route in routes.Values.Distinct()) yield return [partner, route, false, 0];
            foreach (var route in new[] { "publish", "draft", "document", "void", "replace", "adjust/increase" })
                yield return [partner, route, false, 2];
            if (partner == 5)
                foreach (var route in new[] { "publish", "draft", "document", "document/draft", "void", "replace", "adjust/increase" })
                    yield return [partner, route, true, 0];
        }
    }
    [Theory]
    [MemberData(nameof(VnptCases))]
    public async Task Vnpt_routes_preserve_typed_generated_operation_and_return_valid_result(int partner, string route, bool mtt, int type)
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => Site(partner) };
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "vnpt-route-001");
        var input = Request(route, mtt, type);
        if (input is InvoiceBatchRequest batch) batch.Invoices![0].Invoice!.InvoiceId = "22222222-2222-2222-2222-222222222222";
        using var response = await client.PostAsync("/api/eInvoice/" + route, Body(route, input));
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{partner}/{route}/{mtt}/{type}: {await response.Content.ReadAsStringAsync()}");
        Assert.NotEmpty(factory.SoapCalls); Assert.Empty(factory.Calls);
        foreach (var call in factory.SoapCalls)
        {
            Assert.EndsWith(".asmx", call.Url);
            Assert.NotNull(call.Request.GetType().GetField("Body"));
            Assert.Equal("http://tempuri.org/" + call.Operation.Replace("Async", ""),
                VnptSoapInvoker.ResolveOperation(call.Client, call.Operation, call.Request.GetType()).GetCustomAttributes(typeof(System.ServiceModel.OperationContractAttribute), false)
                    .Cast<System.ServiceModel.OperationContractAttribute>().Single().Action);
        }
        if (partner == 5 && route == "publish") Assert.Contains("deleteInvoiceByFkeyAsync", factory.SoapCalls.First().Operation);
        if (route == "draft/edit") Assert.DoesNotContain(factory.SoapCalls, c => c.Operation == "deleteInvoiceByFkeyAsync");
    }
    [Theory]
    [InlineData(1,17)] [InlineData(5,21)] [InlineData(8,13)]
    public async Task Catalogue_matches_every_live_baseline_action(int partner, int count)
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => Site(partner) };
        using var client = factory.Client(); using var response = await client.GetAsync("/api/eInvoice/capabilities");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actions = doc.RootElement.GetProperty("actions").EnumerateArray().Select(a => a.GetProperty("action").GetInt32()).Order().ToArray();
        Assert.Equal(count, actions.Length);
        Assert.Equal(ProviderCapabilityCatalog.GetBaselineActions((ProviderId)partner).Select(a => (int)a).Order(), actions);
        Assert.Empty(factory.SoapCalls); Assert.Empty(factory.Calls);
    }
    private static HttpResponseMessage FastResponse(MisaApiTestFactory.ProviderCall call)
    {
        var payload = "{\"Success\":1,\"Code\":\"0\",\"Message\":\"[{\\\"key\\\":\\\"INV001\\\",\\\"invoiceNo\\\":\\\"123\\\",\\\"keySearch\\\":\\\"SEARCH\\\",\\\"verificationCode\\\":\\\"TAX\\\"}]\"}";
        var body = call.Body!.TrimStart();
        if (body.StartsWith('<'))
        {
            var operation = XDocument.Parse(body).Descendants().First(e => e.Name.LocalName == "Body").Elements().Single();
            var name = operation.Name.LocalName;
            var result = name switch { "CheckKey" => "Ok:SYNTHETIC-CHECK", "GetKey" => "SYNTHETIC-TOKEN,10", _ => payload };
            if (name == "ExcuteCommand" && operation.Elements().Any(e => e.Name.LocalName == "method" && e.Value is "380" or "312" || e.Name.LocalName == "action" && e.Value == "600")) result = "JVBERi0xLjQKc3ludGhldGlj";
            payload = new XElement("Envelope", new XElement("Body", new XElement(name + "Response", new XElement(name + "Result", result)))).ToString();
        }
        else if (call.Url.Contains("method=738") || call.Url.Contains("action=600")) payload = "JVBERi0xLjQKc3ludGhldGlj";
        return new(HttpStatusCode.OK) { Content = new StringContent(payload, Encoding.UTF8) };
    }
    public static IEnumerable<object[]> FastCases()
    {
        foreach (var mtt in new[] { true, false })
            foreach (var route in new[] { "publish", "publish-batch", "preview", "document", "document/draft", "void", "replace", "adjust/increase", "adjust/decrease", "adjust/information" }) yield return [route, mtt];
        yield return ["draft", true];
    }
    [Theory]
    [MemberData(nameof(FastCases))]
    public async Task Fast_rest_and_soap_routes_keep_wire_payload_and_tenant_credentials(string route, bool mtt)
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => Site(8), Respond = FastResponse };
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "fast-route-001");
        using var response = await client.PostAsync("/api/eInvoice/" + route, Body(route, Request(route, mtt)));
        Assert.True(response.StatusCode == HttpStatusCode.OK, route + ": " + await response.Content.ReadAsStringAsync());
        Assert.Equal(mtt || route == "void" ? 1 : 3, factory.Calls.Count);
        var first = factory.Calls.First(); Assert.Contains(mtt ? "fast.example.test" : "fast-soap.example.test", first.Url);
        if (!mtt && route != "void")
        {
            Assert.Contains("CheckKey", first.Body); Assert.Contains("þSYNTHETIC-CHECK", factory.Calls.ElementAt(1).Body);
            Assert.Contains("SYNTHETIC-TOKEN", factory.Calls.Last().Body);
        }
        Assert.DoesNotContain("synthetic-secret", string.Join('\n', factory.Logs));
    }
    [Fact]
    public async Task Fast_health_validates_configuration_without_network_or_connected_claim()
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => Site(8) }; using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.GetProperty("connected").GetBoolean()); Assert.False(doc.RootElement.GetProperty("connectionVerified").GetBoolean());
        Assert.Empty(factory.Calls);
    }
    [Theory]
    [InlineData(1)] [InlineData(5)]
    public async Task Vnpt_completed_write_replays_and_timeout_keeps_pending_claim(int partner)
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => Site(partner) };
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "vnpt-replay-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", Body("publish", Input()));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); var calls = factory.SoapCalls.Count;
        using var replay = await client.PostAsync("/api/eInvoice/publish", Body("publish", Input()));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode); Assert.Equal(calls, factory.SoapCalls.Count);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "vnpt-timeout-002");
        factory.SoapRespond = _ => throw new ProviderCallException(ProviderFailure.Timeout, "VNPT_TIMEOUT", "Synthetic timeout");
        using var timeout = await client.PostAsync("/api/eInvoice/publish", Body("publish", Input(id: "INV-002")));
        Assert.Equal(HttpStatusCode.GatewayTimeout, timeout.StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "vnpt-newkey-003");
        using var blocked = await client.PostAsync("/api/eInvoice/publish", Body("publish", Input(id: "INV-002")));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
    }
}
