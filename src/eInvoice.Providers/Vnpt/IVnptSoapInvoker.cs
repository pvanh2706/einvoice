namespace eInvoice.Providers.Vnpt;
public interface IVnptSoapInvoker
{
    Task<T> CallAsync<T>(Type generatedClient, string url, string operation, object request, CancellationToken cancellationToken);
}
