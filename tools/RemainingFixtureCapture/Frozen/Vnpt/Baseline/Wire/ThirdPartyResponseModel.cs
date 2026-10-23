#nullable disable
#pragma warning disable CS1998, CS0219, CS0414
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.Serialization;
using eInvoice.Providers.Baseline;
using eInvoice.Providers.Vnpt.Baseline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Dynamic;
using System.Globalization;
using CoreInvoice = eInvoice.Core.Contracts.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eInvoice.Providers.Vnpt.Baseline
{
    public class ThirdPartyResponseModel
    {
        public string ezInvoiceId { get; set; }
        public string ThirdPartyInvoiceNumber { get; set; }
        public string InvoicePDF { get; set; }
        public string ConvertForStore { get; set; }
    }
}
