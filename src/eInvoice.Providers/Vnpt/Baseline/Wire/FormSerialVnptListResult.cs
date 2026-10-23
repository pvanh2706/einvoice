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
using System.Collections.Generic;

namespace eInvoice.Providers.Vnpt.Baseline
{
    public class FormSerialVnptListResult
    {
        public string status { get; set; }
        public List<FormSerialVnptList> result { get; set; }
    }
    public class FormSerialVnptList
    {
        public string NameInvoice { get; set; }
        public string InvPattern { get; set; }
        public string InvSerial { get; set; }
        public int Quantity { get; set; }
        public int FromNo { get; set; }
        public int ToNo { get; set; }
        public int CurrentNo { get; set; }
        public int Status { get; set; }
        public string StartDate { get; set; }
    }
}
