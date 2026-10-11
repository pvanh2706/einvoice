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
    public string? SiteFilePath { get; init; }

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
