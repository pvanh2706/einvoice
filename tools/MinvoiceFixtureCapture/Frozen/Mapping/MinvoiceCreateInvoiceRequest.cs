#nullable disable
using eInvoice.Core.Contracts;
namespace eInvoice.Capture.Minvoice.Mapping;
internal sealed class MinvoiceCreateInvoiceRequest : AdjustOrReplaceInvoiceRequest
{
    public string editmode { get; set; }
}
