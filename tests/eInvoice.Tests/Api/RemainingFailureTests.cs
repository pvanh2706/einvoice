using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Text.Json;
using System.Xml;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Vnpt;
using eInvoice.Providers.Vnpt.Baseline;
using eInvoice.Tests.Misa;

namespace eInvoice.Tests.Api;

public class RemainingFailureTests
{
    [Fact]
    public async Task Certificate_read_failure_before_tax_submission_releases_write_reservation()
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => RemainingProviderTests.Site(5),
            SoapRespond = _ => throw new ProviderCallException(ProviderFailure.Timeout, "VNPT_TIMEOUT", "Certificate read timed out.") };
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "vnpt-tax-preflight-001");
        var request = new SendToTaxAuthorityRequest { InvoiceType = 0, InvoiceIds = ["INV-001"], FormNo = "1", Serial = "C26MAA" };
        using var first = await client.PostAsync("/api/eInvoice/tax/send", MisaApiTestFactory.Body(request));
        Assert.Equal(HttpStatusCode.GatewayTimeout, first.StatusCode);
        Assert.Single(factory.SoapCalls); Assert.Equal("GetCertInfoAsync", factory.SoapCalls.Single().Operation);
        factory.SoapRespond = null;
        using var retry = await client.PostAsync("/api/eInvoice/tax/send", MisaApiTestFactory.Body(request));
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode); Assert.Equal("SendInvMTTFkeyAsync", factory.SoapCalls.Last().Operation);
    }
    [Theory]
    [InlineData("OK:#INV-001_123")]
    [InlineData("ERR:6#||OK:#INV-001_123")]
    [InlineData("ERR:6#||ERR:15#||OK:#INV-001_123")]
    public async Task Mixed_vnpt_sign_results_use_success_segment_without_false_rejection(string result)
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => RemainingProviderTests.Site(1), SoapRespond = c => MisaApiTestFactory.SoapResponse(c.Response, result) };
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "vnpt-mixed-001");
        using var response = await client.PostAsync("/api/eInvoice/publish-existing", MisaApiTestFactory.Body(new PublishExistingInvoiceRequest { InvoiceId = "INV-001", InvoiceType = 0, FormNo = "1", Serial = "C26TAA" }));
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("0000123", json.RootElement.GetProperty("data").GetProperty("providerInvoiceNumber").GetString());
        Assert.Single(factory.SoapCalls);
    }
    [Fact]
    public void Actual_generated_wcf_formatter_preserves_soap_action_argument_casing_and_xml_text()
    {
        var site = RemainingProviderTests.Site(5).Site;
        using var scope = new VnptMappingScope(site, true, TimeProvider.System);
        var request = VnptConvertRequest.ConvertCreateAndPublishInvoiceRequestV5(new VnptMappingContext { Company = site, Data = RemainingProviderTests.Input() });
        var operation = VnptSoapInvoker.ResolveOperation(typeof(PublishService_V5.PublishServiceSoapClient), "ImportAndPublishInvAsync", request.GetType());
        var action = operation.GetCustomAttributes(typeof(OperationContractAttribute), false).Cast<OperationContractAttribute>().Single().Action;
        var converter = TypedMessageConverter.Create(request.GetType(), action, new XmlSerializerFormatAttribute());
        using var message = converter.ToMessage(request, MessageVersion.Soap11);
        using var stream = new MemoryStream();
        using (var writer = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, false)) { message.WriteMessage(writer); writer.Flush(); }
        var xml = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("<Account>synthetic-account</Account>", xml); Assert.Contains("<ACpass>synthetic-secret</ACpass>", xml);
        Assert.Contains("<username>synthetic-user</username>", xml); Assert.Contains("<password>synthetic-secret2</password>", xml);
        Assert.Contains("&lt;DSHDon&gt;", xml); Assert.DoesNotContain("&lt;Invoices", xml);
        Assert.Equal("http://tempuri.org/ImportAndPublishInv", action);
    }
    [Theory]
    [InlineData("ERR:1",422)] [InlineData("",422)] [InlineData("garbage",422)]
    public async Task Vnpt_rejected_write_blocks_blind_retry_and_does_not_expose_credentials(string supplier, int status)
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => RemainingProviderTests.Site(1), SoapRespond = c => MisaApiTestFactory.SoapResponse(c.Response, supplier) };
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "vnpt-rejected-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(RemainingProviderTests.Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(status, (int)response.StatusCode);
        Assert.DoesNotContain("synthetic-secret", await response.Content.ReadAsStringAsync());
        using var attempt = await client.GetAsync("/api/eInvoice/operations/publish/vnpt-rejected-001");
        using var json = JsonDocument.Parse(await attempt.Content.ReadAsStringAsync()); Assert.Equal("pending", json.RootElement.GetProperty("state").GetString());
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "vnpt-rejected-002");
        using var retry = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(RemainingProviderTests.Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode); Assert.Single(factory.SoapCalls);
    }
    [Theory]
    [InlineData(1)] [InlineData(5)] [InlineData(8)]
    public async Task Invalid_endpoint_fails_before_any_transport_dispatch(int partner)
    {
        var site = RemainingProviderTests.Site(partner); site.Site.PartnerUrl = "https://not-allowed.example.test/";
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => site }; using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "invalid-host-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(RemainingProviderTests.Input(partner == 8), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Empty(factory.Calls); Assert.Empty(factory.SoapCalls);
    }
    [Theory]
    [InlineData("{}",502)]
    [InlineData("{\"Success\":1,\"Message\":\"[]\"}",502)]
    [InlineData("{\"Success\":1,\"Message\":\"[{\\\"key\\\":\\\"OTHER\\\"}]\"}",502)]
    [InlineData("{\"Success\":0,\"Message\":\"synthetic-secret rejected\"}",422)]
    public async Task Fast_malformed_or_rejected_write_keeps_pending_and_blocks_duplicate(string supplier, int status)
    {
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => RemainingProviderTests.Site(8), Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(supplier, Encoding.UTF8) } };
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "fast-error-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(RemainingProviderTests.Input(true), typeof(CreateInvoiceRequest)));
        Assert.Equal(status, (int)response.StatusCode); Assert.DoesNotContain("synthetic-secret", await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "fast-error-002");
        using var retry = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(RemainingProviderTests.Input(true), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode); Assert.Single(factory.Calls);
    }
    [Fact]
    public async Task Fast_invalid_json_credentials_are_rejected_without_supplier_call()
    {
        var site = RemainingProviderTests.Site(8); site.Site.Username = "not JSON";
        using var factory = new MisaApiTestFactory { Resolve = (_, _) => site }; using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Empty(factory.Calls);
    }
}
