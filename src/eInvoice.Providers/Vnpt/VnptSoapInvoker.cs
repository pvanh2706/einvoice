using System.Reflection;
using System.ServiceModel;
using System.Xml;
using eInvoice.Providers.Common;
using eInvoice.Providers.Vnpt.Baseline;

namespace eInvoice.Providers.Vnpt;

// Invoke only the WSDL-generated client and its exact typed operation.
public sealed class VnptSoapInvoker(VnptOptions options) : IVnptSoapInvoker
{
    internal Uri Validate(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            !options.AllowedHosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "VNPT_ENDPOINT_NOT_ALLOWED", "VNPT endpoint is not allowed.");
        return uri;
    }
    public async Task<T> CallAsync<T>(Type generatedClient, string url, string operation, object request, CancellationToken ct)
    {
        var endpoint = Validate(url);
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
        {
            MaxReceivedMessageSize = 32 * 1024 * 1024,
            ReaderQuotas = XmlDictionaryReaderQuotas.Max,
            SendTimeout = TimeSpan.FromSeconds(60), ReceiveTimeout = TimeSpan.FromSeconds(60),
        };
        var client = Activator.CreateInstance(generatedClient, binding, new EndpointAddress(endpoint))!;
        var channel = (ICommunicationObject)client;
        using var registration = ct.Register(channel.Abort);
        try
        {
            ct.ThrowIfCancellationRequested();
            var method = ResolveOperation(generatedClient, operation, request.GetType());
            if (MarksWrite(operation)) WriteOperationJournal.MarkDispatched();
            var task = (Task)method.Invoke(client, [request])!;
            await task.WaitAsync(ct);
            return (T)task.GetType().GetProperty("Result")!.GetValue(task)!;
        }
        catch (Exception error) when (error is CommunicationException or TimeoutException or TargetInvocationException)
        {
            var cause = error is TargetInvocationException wrapped ? wrapped.InnerException : error;
            if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
            throw new ProviderCallException(cause is TimeoutException ? ProviderFailure.Timeout : ProviderFailure.Connection,
                cause is TimeoutException ? "VNPT_TIMEOUT" : "VNPT_CONNECTION_ERROR", "VNPT SOAP call failed.");
        }
        finally { channel.Abort(); }
    }
    internal static MethodInfo ResolveOperation(Type client, string operation, Type request) =>
        client.GetInterfaces().SelectMany(i => i.GetMethods()).Single(m => m.Name == operation &&
            m.GetParameters() is [var parameter] && parameter.ParameterType == request);
    internal static bool MarksWrite(string operation) => VnptMappingScope.IsWrite && operation is not
        ("GetCertInfoAsync" or "getPublishInvsAsync" or "GetMCCQThueByFkeysAsync" or "listInvByCusFkeyAsync" or
        "GetInvDataByFkeyAsync" or "downloadInvPDFFkeyAsync" or "downloadInvPDFFkeyNoPayAsync" or
        "downloadInvPDFFkeyNoPayErrorAsync" or "downloadNewInvPDFFkeyAsync" or "getInvViewFkeyAsync");
}
