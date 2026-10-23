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

namespace eInvoice.Providers.Vnpt.Baseline.V5
{
    [XmlRoot(ElementName = "DieuChinhHD")]
    public class DieuChinhHD
    {

        [XmlElement(ElementName = "key")]
        public string key { get; set; }

        [XmlElement(ElementName = "Type")]
        public string Type { get; set; }

        [XmlElement(ElementName = "TTChung")]
        public TTChung TTChung { get; set; }

        [XmlElement(ElementName = "NDHDon")]
        public NDHDon NDHDon { get; set; }
    }
}
