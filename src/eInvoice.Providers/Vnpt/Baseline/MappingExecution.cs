using eInvoice.Providers.Common;
namespace eInvoice.Providers.Vnpt.Baseline;
internal static class ExecuteAndTranslateException
{
    internal static T ActionCurrentProject<T>(Func<T> action, object error)
    {
        if (!ReferenceEquals(error, Errors.EzInvoiceHandleResponse)) return ProviderMapping.Run(action, "VNPT");
        try { return action(); }
        catch (Exception ex) when (ex is FormatException or ArgumentException or NullReferenceException or InvalidOperationException or IndexOutOfRangeException or Newtonsoft.Json.JsonException or System.Xml.XmlException)
        { throw new ProviderCallException(ProviderFailure.InvalidResponse, "VNPT_INVALID_RESPONSE", "VNPT response is invalid."); }
    }
    internal static Task<T> ActionThirdPartyAsync<T>(Func<Task<T>> action, object _) => action();
}
internal sealed class BusinessException : Exception
{
    internal object Error { get; } = new();
}
internal static class Errors
{
    internal static readonly object EzInvoiceCreateRequest = new(), ThirdPartyException = new(), EzInvoiceHandleResponse = new(), ThirdPartySpecificError = new(), ThirdPartyUnknownError = new();
}
internal sealed class ResponseModel(int status = 322, string message = "Invalid provider response", object? data = null, string code = "")
{
    public int Status { get; set; } = status;
    public string Message { get; set; } = message;
    public object? Data { get; set; } = data;
    public string Code { get; set; } = code;
    internal static ResponseModel Success(object? data = null) => new(200, "success", data);
    internal static ResponseModel Fail(object error, string message, object? data = null) => new(422, message, data);
    internal static ResponseModel Fail(object error, object? data = null) => new(422, "VNPT rejected the request", data);
    internal static ResponseModel Exception(object error, Exception ex) => throw ex;
}
