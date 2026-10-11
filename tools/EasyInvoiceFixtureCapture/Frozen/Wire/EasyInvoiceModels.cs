#nullable disable

using System.Net.NetworkInformation;
using System;
using System.Xml.Serialization;

namespace eInvoice.Capture.EasyInvoice.Wire
{
	#region Cấu trúc XML hóa đơn mẫu
	//<Invoices>
	//	<Inv>
	//		<Invoice>
	//			<TaxAuthorityCode>mã cơ quan thuế.... (xem phụ lục V.10) </TaxAuthorityCode>
	//			<Ikey>Giá trị khóa duy nhất của hóa đơn</Ikey>
	//			<CusCode>Mã khách hàng</CusCode>
	//			<Buyer>Tên người mua hàng</Buyer>
	//			<CusName>Tên khách hàng</CusName>
	//			<Email>Email của khách nhận thông báo phát hành hoá đơn</Email>
	//			<EmailCC>Danh sách email CC (ngăn cách bởi dấu phẩy) nhận thông báo phát hành hoá đơn</EmailCC>
	//			<CusAddress>Địa chỉ khách hàng</CusAddress>
	//			<CusBankName>Tên ngân hàng của khách hàng</CusBankName>
	//			<CusBankNo>Số tài khoản ngân hàng của khách hàng</CusBankNo>
	//			<CusPhone>Điện thoại khách hàng</CusPhone>
	//			<IdentityCardNumber>Căn cước công dân</IdentityCardNumber>
	//			<CusTaxCode>Mã số thuế (Bắt buộc với KHDoanh nghiệp)</CusTaxCode>
	//			<PaymentMethod>Hình thức thanh toán(xem phụ lục V.1)</PaymentMethod>
	//			<ArisingDate>Ngày phát sinh hóa đơn(mặc định là ngày hiện tại, chuỗi định dạng dd/MM/yyyy) </ArisingDate>
	//			<ExchangeRate>Tỷ giá chuyển đổi</ExchangeRate>
	//			<CurrencyUnit>Đơn vị tiền tệ (ví dụ VND, USD) (xem phụ lục V.9) </CurrencyUnit>
	//			<Extra>Thông tin bổ sung(định dạng json có các thuộc tính theo quy ước riêng nếu có phát sinh) </Extra>
	//			<Products>
	//				<Product>
	//					<Code>Mã sản phẩm</Code>
	//					<No>Số thứ tự</No>
	//					<Feature>Loại sản phẩm(xem phụ lục V.8)</Feature>
	//					<ProdName>Tên sản phẩm(xem phụ lục V.3) </ProdName>
	//					<ProdUnit>Đơn vị tính</ProdUnit>
	//					<ProdQuantity>Số lượng</ProdQuantity>
	//					<ProdPrice>Đơn giá</ProdPrice>
	//					<Discount>Tỉ lệ chiết khấu</Discount>
	//					<DiscountAmount>Tổng tiền chiết khấu</DiscountAmount>
	//					<Total>Tổng tiền trước thuế</Total>
	//					<VATRate>Thuế suất (xem phụ lục V.2) </VATRate>
	//					<VATRateOther>Thuế suất trong trường hợp thuế khác(Khi VATRate = -3)</VATRateOther>
	//					<VATAmount>Tiền thuế</VATAmount>
	//					<Amount>Tổng tiền sau thuế</Amount>
	//					<Extra>Thông tin bổ sung (định dạng json có các thuộc tính theo quy ước riêng nếu có phát sinh)</Extra>
	//				</Product>
	//			</Products>
	//			<Total>Tổng tiền trước thuế</Total>
	//			<VATRate>Thuế GTGT (xem phụ lục V.2)</VATRate>
	//			<VATRateOther>Thuế suất trong trường hợp thuế khác(Khi VATRate = -3)</VATRateOther>
	//			<VATAmount>Tiền thuế GTGT</VATAmount>
	//			<Amount>Tổng tiền</Amount>
	//			<GrossValue>Tổng tiền trước thuế thuế KCT</GrossValue> //  Không chịu thuế
	//			<GrossValue0>Tổng tiền trước thuế thuế 0%</GrossValue0>
	//			<GrossValue5>Tổng tiền trước thuế thuế 5%</GrossValue5>
	//			<GrossValue10>Tổng tiền trước thuế thuế 10%</GrossValue10>
	//			<GrossValueNDeclared>Tổng tiền trước thuế thuế KKKN</GrossValueNDeclared>// Không kê khai nộp thuế
	//			<GrossValueContractor>Tổng tiền trước thuế thuế Khác</GrossValueContractor>
	//			<VatAmount0>Tổng tiền thuế thuế 0%</VatAmount0>
	//			<VatAmount5>Tổng tiền thuế thuế 5%</VatAmount5>
	//			<VatAmount10>Tổng tiền thuế thuế 10%</VatAmount10>
	//			<VatAmountNDeclared>Tổng tiền thuế thuế KKKN</VatAmountNDeclared>
	//			<VatAmountContractor>Tổng tiền thuế thuế Khác</VatAmountContractor>
	//			<Amount0>Tổng tiền sau thuế thuế 0%</Amount0>
	//			<Amount5>Tổng tiền sau thuế thuế 5%</Amount5>
	//			<Amount10>Tổng tiền sau thuế thuế 10%</Amount10>
	//			<AmountNDeclared>Tổng tiền sau thuế thuế KKKN</AmountNDeclared>
	//			<AmountOther>Tổng tiền sau thuế thuế Khác</AmountOther>
	//			<GrossValue8>Tổng tiền trước thuế thuế 8%</GrossValue8>
	//			<VatAmount8>Tổng tiền thuế thuế 8%</VatAmount8>
	//			<Amount8>Tổng tiền sau thuế thuế 8%</Amount8>
	//			<AmountInWords>Số tiền viết bằng chữ</AmountInWords>
	//		</Invoice>
	//	</Inv>
	//</Invoices>
	#endregion

	[XmlRoot("Invoices")]
	public class Invoices
	{
		[XmlElement("Inv")]
		public List<Inv> Inv { get; set; }
	}

	public class Inv
	{
		[XmlElement("Invoice")]
		public Invoice Invoice { get; set; }
	}

	public class Invoice
	{
		[XmlElement("TaxAuthorityCode")]
		public string TaxAuthorityCode { get; set; }

		[XmlElement("Ikey")]
		public string Ikey { get; set; }

		[XmlElement("CusCode")]
		public string CusCode { get; set; }

		[XmlElement("Buyer")]
		public string Buyer { get; set; }

		[XmlElement("CusName")]
		public string CusName { get; set; }

		[XmlElement("Email")]
		public string Email { get; set; }

		[XmlElement("EmailCC")]
		public string EmailCC { get; set; }

		[XmlElement("CusAddress")]
		public string CusAddress { get; set; }

		[XmlElement("CusBankName")]
		public string CusBankName { get; set; }

		[XmlElement("CusBankNo")]
		public string CusBankNo { get; set; }

		[XmlElement("CusPhone")]
		public string CusPhone { get; set; }

		[XmlElement("IdentityCardNumber")]
		public string IdentityCardNumber { get; set; }

		[XmlElement("CusTaxCode")]
		public string CusTaxCode { get; set; }

		[XmlElement("PaymentMethod")]
		public string PaymentMethod { get; set; }

		[XmlElement("ArisingDate")]
		public string ArisingDate { get; set; }

		[XmlElement("ExchangeRate")]
		public string ExchangeRate { get; set; }

		[XmlElement("CurrencyUnit")]
		public string CurrencyUnit { get; set; }

		[XmlElement("Extra")]
		public string Extra { get; set; }

		[XmlArray("Products")]
		[XmlArrayItem("Product")]
		public List<Product> Products { get; set; }

		[XmlElement("Total")]
		public string Total { get; set; }

		[XmlElement("VATRate")]
		public string VATRate { get; set; }

		[XmlElement("VATRateOther")]
		public string VATRateOther { get; set; }

		[XmlElement("VATAmount")]
		public string VATAmount { get; set; }

		[XmlElement("Amount")]
		public string Amount { get; set; }

		[XmlElement("GrossValue")]
		public string GrossValue { get; set; }

		[XmlElement("GrossValue0")]
		public string GrossValue0 { get; set; }

		[XmlElement("GrossValue5")]
		public string GrossValue5 { get; set; }

		[XmlElement("GrossValue10")]
		public string GrossValue10 { get; set; }

		[XmlElement("GrossValueNDeclared")]
		public string GrossValueNDeclared { get; set; }

		[XmlElement("GrossValueContractor")]
		public string GrossValueContractor { get; set; }

		[XmlElement("VatAmount0")]
		public string VatAmount0 { get; set; }

		[XmlElement("VatAmount5")]
		public string VatAmount5 { get; set; }

		[XmlElement("VatAmount10")]
		public string VatAmount10 { get; set; }

		[XmlElement("VatAmountNDeclared")]
		public string VatAmountNDeclared { get; set; }

		[XmlElement("VatAmountContractor")]
		public string VatAmountContractor { get; set; }

		[XmlElement("Amount0")]
		public string Amount0 { get; set; }

		[XmlElement("Amount5")]
		public string Amount5 { get; set; }

		[XmlElement("Amount10")]
		public string Amount10 { get; set; }

		[XmlElement("AmountNDeclared")]
		public string AmountNDeclared { get; set; }

		[XmlElement("AmountOther")]
		public string AmountOther { get; set; }

		[XmlElement("GrossValue8")]
		public string GrossValue8 { get; set; }

		[XmlElement("VatAmount8")]
		public string VatAmount8 { get; set; }

		[XmlElement("Amount8")]
		public string Amount8 { get; set; }

		[XmlElement("AmountInWords")]
		public string AmountInWords { get; set; }

		[XmlElement("CusIdentification")]
		public string CusIdentification { get; set; }

		[XmlElement("PassportNo")]
		public string PassportNo { get; set; }

		[XmlElement("CusEmails")]
		public string CusEmails { get; set; }

		[XmlElement("BudgetaryRelationshipCode")]
		public string BudgetaryRelationshipCode { get; set; }

	}

	public class Product
	{
		[XmlElement("Code")]
		public string Code { get; set; }

		[XmlElement("No")]
		public string No { get; set; }

		[XmlElement("Feature")]
		public string Feature { get; set; }

		[XmlElement("ProdName")]
		public string ProdName { get; set; }

		[XmlElement("ProdUnit")]
		public string ProdUnit { get; set; }

		[XmlElement("ProdQuantity")]
		public string ProdQuantity { get; set; }

		[XmlElement("ProdPrice")]
		public string ProdPrice { get; set; }

		[XmlElement("Discount")]
		public string Discount { get; set; }

		[XmlElement("DiscountAmount")]
		public string DiscountAmount { get; set; }

		[XmlElement("Total")]
		public string Total { get; set; }

		[XmlElement("VATRate")]
		public string VATRate { get; set; }

		[XmlElement("VATRateOther")]
		public string VATRateOther { get; set; }

		[XmlElement("VATAmount")]
		public string VATAmount { get; set; }

		[XmlElement("Amount")]
		public string Amount { get; set; }

		[XmlElement("Extra")]
		public string Extra { get; set; }
	}

	public class AdjustInv : Invoice
	{
		[XmlElement("Type")]
		public string Type { get; set; }
	}

	public class ReplaceInv : Invoice { }

	public class ExtraDataInvoice
	{
		public decimal? serviceChargeRate { get; set; }
		public decimal? serviceCharge { get; set; }
		public decimal? SubTotal { get; set; }
		public string bkNo { get; set; }
		public string folioNo { get; set; }
		public string arrivalDate { get; set; }
		public string departureDate { get; set; }
		public int hide { get; set; }
		public string EmployeeName { get; set; }
	}

	public class ExtraDataProduct
	{
		public int icheck { get; set; }
	}
}
