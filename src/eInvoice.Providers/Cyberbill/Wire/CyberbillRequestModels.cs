#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eInvoice.Providers.Cyberbill.Wire
{
	public class CyberbillCreateAndPublishInvoiceModel
	{
		public CreateInvoiceCyberbillModel Body { get; set; }
		public CyberbillConnectionInfo Company { get; set; }
	}

	public class CyberbillReplaceInvoiceModel
	{
		public CreateInvoiceCyberbillModel Body { get; set; }
		public CyberbillConnectionInfo Company { get; set; }
	}

	public class CyberbillAdjustInvoiceModel
	{
		public CreateInvoiceCyberbillModel Body { get; set; }
		public CyberbillConnectionInfo Company { get; set; }
	}

	public class CyberbillSignAndDownloadPDFInvoiceModel
	{
		public SignAndDownloadPDFInvoiceCyberbillModel Body { get; set; }
		public CyberbillConnectionInfo Company { get; set; }
	}
	public class CyberbillVoidInvoiceModel
	{
		public VoidInvoiceCyberbillModel Body { get; set; }
		public CyberbillConnectionInfo Company { get; set; }
	}
	public class CyberbillDeleteInvoiceModel
	{
		public DeleteInvoiceCyberbillModel Body { get; set; }
		public CyberbillConnectionInfo Company { get; set; }
	}
}
