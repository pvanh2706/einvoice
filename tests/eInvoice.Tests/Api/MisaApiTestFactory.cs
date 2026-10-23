using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;
using eInvoice.Providers.Viettel;
using eInvoice.Providers.Cyberbill;
using eInvoice.Providers.Sun;
using eInvoice.Providers.Minvoice;
using eInvoice.Providers.EasyInvoice;
using eInvoice.Providers.Fast;
using eInvoice.Providers.Vnpt;
using eInvoice.Providers.Vnpt.Baseline;
using eInvoice.Providers.Common;
using eInvoice.Providers.Configuration;
using eInvoice.Tests.Misa;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Api;

internal sealed class MisaApiTestFactory : WebApplicationFactory<Program>
{
    private const string Key = "synthetic-misa-full-api-test-key";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "eInvoice-tests", Guid.NewGuid().ToString("N"));
    public ConcurrentQueue<ProviderCall> Calls { get; } = new();
    public ConcurrentQueue<string> Logs { get; } = new();
    public Func<ProviderCall, HttpResponseMessage>? Respond { get; set; }
    public Func<SiteInfo?, Guid?, ResolvedSite>? Resolve { get; set; }
    public ConcurrentQueue<SoapCall> SoapCalls { get; } = new();
    public Func<SoapCall, object>? SoapRespond { get; set; }
    public string? SiteFilePath { get; init; }
    public TimeProvider? Clock { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Clients:0:ClientId"] = "misa-full-api-test",
            ["Authentication:Clients:0:AllowedSiteIds:0"] = MisaTestData.SiteId.ToString(),
            ["Authentication:Clients:0:Keys:0:KeyId"] = "test",
            ["Authentication:Clients:0:Keys:0:Sha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Key))),
            ["Authentication:Clients:0:Keys:0:ExpiresAtUtc"] = "2099-01-01T00:00:00Z",
        }));
        builder.ConfigureLogging(logging => logging.AddProvider(new LogProvider(Logs)));
        builder.ConfigureServices(services =>
        {
            if (Clock is not null) { services.RemoveAll<TimeProvider>(); services.AddSingleton(Clock); }
            if (SiteFilePath is null)
            {
                services.RemoveAll<ISiteConfigurationResolver>();
                services.AddSingleton<ISiteConfigurationResolver>(new Resolver(this));
            }
            else
            {
                services.RemoveAll<ISiteConfigurationStore>();
                services.AddSingleton<ISiteConfigurationStore>(new JsonSiteConfigurationStore(SiteFilePath));
            }
            services.RemoveAll<MisaLoginOptions>();
            services.AddSingleton(new MisaLoginOptions { AllowedHosts = ["misa.example.test"] });
            services.RemoveAll<ViettelOptions>();
            services.AddSingleton(new ViettelOptions { AllowedHosts = ["viettel.example.test"] });
            services.AddHttpClient<ViettelTransport>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.RemoveAll<CyberbillOptions>();
            services.AddSingleton(new CyberbillOptions { AllowedHosts = ["cyberbill.example.test"] });
            services.AddHttpClient<CyberbillTransport>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.RemoveAll<SunOptions>();
            services.AddSingleton(new SunOptions { AllowedHosts = ["sun.example.test", "sun-cancel.example.test"] });
            services.AddHttpClient<SunTransport>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.RemoveAll<MinvoiceOptions>();
            services.AddSingleton(new MinvoiceOptions { AllowedHosts = ["minvoice.example.test", "mst.minvoice.com.vn"] });
            services.AddHttpClient<MinvoiceTransport>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.RemoveAll<EasyInvoiceOptions>();
            services.AddSingleton(new EasyInvoiceOptions { AllowedHosts = ["easyinvoice.example.test"] });
            services.AddHttpClient<EasyInvoiceTransport>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.RemoveAll<FastOptions>();
            services.AddSingleton(new FastOptions { AllowedHosts = ["fast.example.test", "fast-soap.example.test"] });
            services.AddHttpClient<FastTransport>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.RemoveAll<VnptOptions>();
            services.AddSingleton(new VnptOptions { AllowedHosts = ["vnpt.example.test"] });
            services.RemoveAll<IVnptSoapInvoker>();
            services.AddSingleton<IVnptSoapInvoker>(new SoapInvoker(this));
            services.RemoveAll<MisaWriteJournal>();
            services.AddSingleton(new MisaWriteJournal(_directory, TimeProvider.System));
            services.AddHttpClient<MisaLoginClient>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.AddHttpClient<IMisaReceiptLoginClient, MisaReceiptLoginClient>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.AddHttpClient<MisaBusinessTransport>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
            services.AddHttpClient<IMisaFormSerialClient, MisaFormSerialClient>().ConfigurePrimaryHttpMessageHandler(() => new Handler(this));
        });
    }

    public HttpClient Client(bool authenticated = true, Guid? siteId = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (authenticated) client.DefaultRequestHeaders.Add("X-Api-Key", Key);
        client.DefaultRequestHeaders.Add("X-Site-Id", (siteId ?? MisaTestData.SiteId).ToString());
        return client;
    }

    public static StringContent Body(object value, Type? contract = null) =>
        new(JsonSerializer.Serialize(value, contract ?? value.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json");

    public static HttpResponseMessage Envelope(object? data, bool success = true) => new(HttpStatusCode.BadRequest)
    {
        Content = new StringContent(JsonSerializer.Serialize(new { Success = success, Data = data is string text ? text : JsonSerializer.Serialize(data), Errors = Array.Empty<string>() })),
    };

    private HttpResponseMessage DefaultResponse(ProviderCall call)
    {
        if (call.Url.EndsWith("auth/token", StringComparison.Ordinal)) return Envelope("synthetic-v3-token");
        if (call.Url.Contains("/templates?", StringComparison.Ordinal)) return Envelope(Array.Empty<object>());
        if (call.Url.Contains("user/token?", StringComparison.Ordinal)) return Envelope("synthetic-receipt-token");
        if (call.Url.EndsWith("iptemplatepublish", StringComparison.Ordinal))
            return Envelope(new[] { new { TemplateCode = "R26TEST", InvoiceSeries = "C26RAA", InvoiceType = "2" } });
        if (call.Url.Contains("invoicelinkview", StringComparison.Ordinal)) return Envelope("https://document.example.test/preview");
        if (call.Url.Contains("download", StringComparison.Ordinal)) return Envelope(new[] { new { TransactionID = "transaction-001", Data = "c3ludGhldGljLXBkZg==" } });
        if (call.Url.EndsWith("xmlhash", StringComparison.Ordinal)) return Envelope(new[] { new { MessageCode = "MSG-001", MessageHash = "synthetic-hash" } });
        if (call.Url.Contains("invoice-status", StringComparison.Ordinal)) return Envelope(new[] { new { RefID = "INV-001", SendTaxStatus = 2 } });
        if (call.Url.EndsWith("/cancel", StringComparison.Ordinal) || call.Url.EndsWith("/deleted", StringComparison.Ordinal)) return Envelope(null);
        if (call.Url.EndsWith("/tax", StringComparison.Ordinal)) return Envelope("synthetic-tax-result");
        var payload = JToken.Parse(call.Body!);
        var ids = payload is JArray array ? array.Select(item => item.Value<string>("RefID") ?? item["OrgInvoiceData"]?.Value<string>("RefID") ?? item["OriginalInvoiceData"]?.Value<string>("RefID")!).ToArray()
            : payload["InvoiceDataList"]!.Select(item => item.Value<string>("RefID")!).ToArray();
        return Envelope(ids.Select(id => new { RefID = id, InvNo = "000001", InvoiceNumber = "000001", TransactionID = "transaction-001", InvCode = "synthetic-tax-code", InvoiceData = "<synthetic-invoice/>" }).ToArray());
    }

    private sealed class Resolver(MisaApiTestFactory factory) : ISiteConfigurationResolver
    {
        public Task<ResolvedSite> ResolveAsync(SiteInfo? suppliedConfiguration, Guid? siteId, CancellationToken cancellationToken = default) =>
            Task.FromResult(factory.Resolve?.Invoke(suppliedConfiguration, siteId) ?? MisaTestData.Site);
    }
    private sealed class Handler(MisaApiTestFactory factory) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var call = new ProviderCall(request.RequestUri!.AbsoluteUri, request.Method,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value)),
                request.Content?.Headers.ContentType?.CharSet);
            factory.Calls.Enqueue(call);
            return factory.Respond?.Invoke(call) ?? factory.DefaultResponse(call);
        }
    }
    public sealed record ProviderCall(string Url, HttpMethod Method, string? Body, Dictionary<string, string> Headers, string? Encoding);
    public sealed record SoapCall(Type Client, string Url, string Operation, object Request, Type Response);
    private sealed class SoapInvoker(MisaApiTestFactory factory) : IVnptSoapInvoker
    {
        public Task<T> CallAsync<T>(Type client, string url, string operation, object request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _ = VnptSoapInvoker.ResolveOperation(client, operation, request.GetType());
            if (VnptSoapInvoker.MarksWrite(operation)) WriteOperationJournal.MarkDispatched();
            var call = new SoapCall(client, url, operation, request, typeof(T));
            factory.SoapCalls.Enqueue(call);
            if (factory.SoapRespond is not null) return Task.FromResult((T)factory.SoapRespond(call));
            var result = operation switch
            {
                "getPublishInvsAsync" => "{\"status\":\"OK\",\"result\":[{\"NameInvoice\":\"Synthetic form\",\"InvPattern\":\"1\",\"InvSerial\":\"C26TAA\",\"Quantity\":100,\"FromNo\":1,\"ToNo\":100,\"CurrentNo\":1,\"Status\":1}]}",
                "GetCertInfoAsync" => Convert.ToBase64String(Encoding.UTF8.GetBytes("<Certificate><SerialNumber>SYNTHETIC-CERT</SerialNumber></Certificate>")),
                "GetMCCQThueByFkeysAsync" => Convert.ToBase64String(Encoding.UTF8.GetBytes("<DSHDon><HDon><Fkey>INV-001</Fkey><SHDon>123</SHDon><TThai>1</TThai></HDon></DSHDon>")),
                "downloadInvPDFFkeyAsync" or "downloadInvPDFFkeyNoPayAsync" or "downloadInvPDFFkeyNoPayErrorAsync" or "downloadNewInvPDFFkeyAsync" => "JVBERi0xLjQKc3ludGhldGlj",
                "getInvViewFkeyAsync" => "https://vnpt.example.test/synthetic-document",
                "listInvByCusFkeyAsync" => "<Data><Item><cusCode>INV-001</cusCode><signStatus>1</signStatus><invNum>123</invNum></Item></Data>",
                "getInvByFkeyAsync" => Convert.ToBase64String(Encoding.UTF8.GetBytes("<DSHDon><HDon><Fkey>INV-001</Fkey><SHDon>123</SHDon><TThai>1</TThai></HDon></DSHDon>")),
                "deleteInvoiceByFkeyAsync" or "cancelInvAsync" or "cancelInvNoPayAsync" or "SendInvMTTFkeyAsync" => "OK",
                _ => "OK:#INV-001_123",
            };
            var body = request.GetType().GetField("Body")!.GetValue(request)!;
            var xml = body.GetType().GetField("xmlInvData")?.GetValue(body) as string;
            if (xml is not null && operation == "ImportAndPublishInvAsync")
            {
                var keys = System.Xml.Linq.XDocument.Parse(xml).Descendants().Where(e => e.Name.LocalName == "key").Select(e => e.Value);
                result = "OK:1;C26TAA-" + string.Join(',', keys.Select(k => k + "_123"));
            }
            return Task.FromResult((T)SoapResponse(typeof(T), result));
        }
    }
    internal static object SoapResponse(Type type, string value)
    {
        var response = Activator.CreateInstance(type)!;
        var bodyField = type.GetField("Body")!;
        var body = Activator.CreateInstance(bodyField.FieldType)!;
        body.GetType().GetFields().Single(f => f.FieldType == typeof(string)).SetValue(body, value);
        bodyField.SetValue(response, body);
        return response;
    }
    private sealed class LogProvider(ConcurrentQueue<string> logs) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturedLogger(logs);
        public void Dispose() { }
        private sealed class CapturedLogger(ConcurrentQueue<string> logs) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => logs.Enqueue(formatter(state, exception));
        }
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        var allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "eInvoice-tests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(_directory).StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe cleanup path");
        if (disposing && Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
