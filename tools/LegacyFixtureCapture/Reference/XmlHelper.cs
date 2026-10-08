using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace ezInvoice.Application.Heldper
{
    public static class XmlHelper
    {
		/// <summary>
		/// Dùng hàm này khi cần chuyển từ dữ liệu không động (không chứa dynamic, object,...) sang XML
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="dataToSerialize"></param>
		/// <returns></returns>
		public static string Serialize<T>(T dataToSerialize)
        {
            if (dataToSerialize == null) return null;

            using (StringWriter stringwriter = new StringWriter())
            {
                var serializer = new XmlSerializer(dataToSerialize.GetType());
                serializer.Serialize(stringwriter, dataToSerialize);
                return stringwriter.ToString();
            }
        }
    }
	public static class XmlHelperObject
	{
		/// <summary>
		/// Dùng hàm này khi cần chuyển từ dữ liệu động (dynamic, object,...) sang XML
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string Serialize(object obj)
		{
			string rootElementName = obj.GetType().Name;
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
			var xmlDoc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(json, rootElementName);

			using (var stringWriter = new StringWriter())
			using (var xmlTextWriter = new XmlTextWriter(stringWriter))
			{
				xmlTextWriter.Formatting = Formatting.Indented;
				xmlDoc.WriteTo(xmlTextWriter);
				return stringWriter.ToString();
			}
		}
	}

	public static class XmlHelperObjectMulti
	{
		/// <summary>
		/// Dùng hàm này khi cần chuyển từ dữ liệu động (dynamic, object,...) sang XML
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string Serialize(object obj)
		{
			string rootElementName = "DSHDon";
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
			var xmlDoc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(json, rootElementName);

			using (var stringWriter = new StringWriter())
			using (var xmlTextWriter = new XmlTextWriter(stringWriter))
			{
				xmlTextWriter.Formatting = Formatting.Indented;
				xmlDoc.WriteTo(xmlTextWriter);
				return stringWriter.ToString();
			}
		}
	}
}
