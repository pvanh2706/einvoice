#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eInvoice.Providers.Fast.Wire
{
	public class RequestData
	{
		public string voucherBook { get; set; }
		public InvoiceData data { get; set; }
	}

	public class RequestDataAdjustOrReplace : RequestData
	{
		public string originalInvoice { get; set; } = "";
		public string adjustmentType { get; set; } = "";
	}

	public class InvoiceData
	{
		public Structure structure { get; set; }
		public List<Invoice> invoices { get; set; }
	}

	public class Structure
	{
		public List<string> master { get; set; }
		public List<string> detail { get; set; }
	}

	public class Invoice
	{
		public List<object> master { get; set; }
		public List<object[]> detail { get; set; }
	}

}
