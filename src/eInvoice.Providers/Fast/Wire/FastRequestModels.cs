#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eInvoice.Providers.Fast.Wire
{
	public class FastApiRequest
	{
		public string urlAPI { get; set; }
		public Dictionary<string, string> headersAPI { get; set; }
		public string bodyPost { get; set; }
	}
	public class FastApiSoapRequest
	{
		public string urlAPI { get; set; }
		public string bodyXMLPost { get; set; }
	}

	public class FastInfoToken
	{
		public string clientCode { get; set; }
		public string proxyCode { get; set; }
		public string user { get; set; }
		public string password { get; set; }
		public string UnitCode { get; set; }
		public string PartnerUrl { get; set; }
	}

	public class FastBodyGetPDFBase64
	{
		public string keySearch { get; set; }
	}
	public class FastBodyGetPDFDraftBase64
	{
		public string Key { get; set; }
		public string InvoiceDate { get; set; }
	}
	public class FastBodyGetPDFBase64Soap
	{
		public string key { get; set; }
	}

	public class FastBodyVoidInvoice
	{
		public List<FastBodyVoidInvoiceDetail> key { get; set; }
	}
    public class FastBodyVoidInvoiceDetail
	{
		public string keySearch { get; set; }
		public string reason { get; set; }
	}

	public class FastJsonTokenDB
	{
		public string clientC { get; set; }
		public string proxyC { get; set; }
		public string user { get; set; }
		public string unitC { get; set; }
	}
}
