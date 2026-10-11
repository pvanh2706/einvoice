#nullable disable
// Ported from pinned baseline 66a9c74; UTC+7 dates approved 2026-10-11.
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.Viettel.Wire;
using eInvoice.Providers.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace eInvoice.Providers.Viettel.Mapping
{
    internal static class ViettelRequestMapper
    {
        public static ViettelConnectionInfo LoginRequest(ViettelMappingContext model)
        {
            return CompanyInfo(model);
        }
        public static ViettelCreateAndPublishMultiInvoiceModel ConvertCreateAndPublishMultiInvoiceRequest(ViettelMappingContext model)
        {
            List<CreateInvoiceRequest> invoiceRequest = model.Data as List<CreateInvoiceRequest>;
            dynamic action= Convert.ToInt32(model.Action);
            List<CreateInvoiceViettelModel> viettelInvoices = new List<CreateInvoiceViettelModel>();
            foreach (CreateInvoiceRequest item in invoiceRequest)
            {
                viettelInvoices.Add(ConvertInvoiceViettel(action, item, null, model.ExtraData));
            }

            ViettelCreateAndPublishMultiInvoiceModel result = new ViettelCreateAndPublishMultiInvoiceModel
            {
                Company = CompanyInfo(model),
                Body = new CreateMultiInvoiceViettelModel
                {
                    commonInvoiceInputs = viettelInvoices,
                },
            };
            return result;
        }

        public static ViettelCreateAndPublishInvoiceModel ConvertAdjustReplaceRequest(ViettelMappingContext model)
        {
            AdjustOrReplaceInvoiceRequest invoiceRequest = model.Data as AdjustOrReplaceInvoiceRequest;
            CreateInvoiceViettelModel invoice = ConvertInvoiceViettel(Convert.ToInt32(model.Action), invoiceRequest, invoiceRequest.OriginalProviderInvoiceNumber, model.ExtraData);
            ViettelCreateAndPublishInvoiceModel result = new ViettelCreateAndPublishInvoiceModel
            {
                Company = CompanyInfo(model),
                Body = invoice,
            };
            return result;
        }

        public static ViettelCreateAndPublishInvoiceModel ConvertPreviewInvoiceRequest(ViettelMappingContext model)
        {
            CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
            CreateInvoiceViettelModel invoice = ConvertInvoiceViettel(Convert.ToInt32(model.Action), invoiceRequest, null, model.ExtraData);
            ViettelCreateAndPublishInvoiceModel result = new ViettelCreateAndPublishInvoiceModel
            {
                Company = CompanyInfo(model),
                Body = invoice,
            };
            return result;
        }

        public static ViettelCreateAndPublishInvoiceModel ConvertCreateDraftInvoiceRequest(ViettelMappingContext model)
        {
            CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
            CreateInvoiceViettelModel invoice = ConvertInvoiceViettel(Convert.ToInt32(model.Action), invoiceRequest, null, model.ExtraData);
            ViettelCreateAndPublishInvoiceModel result = new ViettelCreateAndPublishInvoiceModel
            {
                Company = CompanyInfo(model),
                Body = invoice,
            };
            return result;
        }
        public static ViettelCreateAndPublishInvoiceModel ConvertCreateAndPublishInvoiceRequest(ViettelMappingContext model)
        {
            CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
            CreateInvoiceViettelModel invoice = ConvertInvoiceViettel(Convert.ToInt32(model.Action), invoiceRequest, null, model.ExtraData);
            ViettelCreateAndPublishInvoiceModel result = new ViettelCreateAndPublishInvoiceModel
            {
                Company = CompanyInfo(model),
                Body = invoice,
            };
            return result;
        }

        private static CreateInvoiceViettelModel ConvertInvoiceViettel(int Action, CreateInvoiceRequest invoiceRequest, string OriginalProviderInvoiceNumber, List<ExtraFieldMapping> extraDataMap)
        {
            Invoice pmsInvoice = invoiceRequest.Invoice;
            CreateInvoiceViettelModel invoice = new CreateInvoiceViettelModel();
            Adjust type = GetAdjustmentType(Action);
            long invoiceIssuedDate = ProviderDate.ToUnixMilliseconds(pmsInvoice.InvoiceDate);

            int? buyerIdType = null;
            string buyerIdNo = "";
            if (pmsInvoice.IdentityCard != "" && pmsInvoice.IdentityCard != null)
            {
                buyerIdType = 1;
                buyerIdNo= pmsInvoice.IdentityCard;
			}
            else if (pmsInvoice.PassportNum != "" && pmsInvoice.PassportNum != null)
            {
				buyerIdType = 3;
				buyerIdNo = pmsInvoice.PassportNum;
			}
            bool isTaxReductionItem = (pmsInvoice.IsTaxReduction43 ?? false) && invoiceRequest.Details.Any(e => (e.TaxReductionType ?? 0) != 0);
            bool isTaxReductionBill = (pmsInvoice.IsTaxReduction43 ?? false) && !invoiceRequest.Details.Any(e => (e.TaxReductionType ?? 0) != 0) && (pmsInvoice.TaxReductionType ?? 0) != 0;

            // Trường Validation  = 0:bỏ qua việc kiểm tra ràng buộc và tính toán lại các trường thông tin tiền của hàng hóa và hóa đơn
            // Sẽ bắt buộc truyền dữ liệu exchangeRate và totalAmountAfterDiscount (tổng tiền trước thuế)
            bool notValidation = (pmsInvoice.Validation ?? 0) == 0;
            invoice.generalInvoiceInfo = new generalInvoiceInfo
            {
                invoiceType = "1",
                templateCode = pmsInvoice.FormNo,
                invoiceSeries = pmsInvoice.Serial,
                transactionUuid = pmsInvoice.InvoiceId.ToString(),
                //invoiceIssuedDate = (long)DateTime.ParseExact(pmsInvoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds, //(long)1666942252000,
                invoiceIssuedDate = invoiceIssuedDate,
                currencyCode = pmsInvoice.CurrencyCode,
                adjustmentType = type.adjustmentType,
                adjustmentInvoiceType = type.adjustmentInvoiceType,
                paymentStatus = true,
                paymentType = pmsInvoice.PaymentMethod,
                paymentTypeName = pmsInvoice.PaymentMethod,
                cusGetInvoiceRight = true,
                originalInvoiceId = OriginalProviderInvoiceNumber,
                validation = pmsInvoice.Validation,
                adjustAmount20 = isTaxReductionItem ? 0 : (isTaxReductionBill ? pmsInvoice.TaxReductionType : null),
                exchangeRate = notValidation ? 1 : null,
            };
            var buyerNotGetInvoice = pmsInvoice.BuyerNotGetInvoice ??
                (string.IsNullOrWhiteSpace(pmsInvoice.CompanyName) ? 1 : 0);

			invoice.buyerInfo = new buyerInfo
            {
                buyerName = pmsInvoice.CustomerName,
                buyerLegalName = pmsInvoice.CompanyName,
                buyerTaxCode = pmsInvoice.CustomerTax,
                buyerAddressLine = pmsInvoice.CustomerAddress,
                buyerPhoneNumber = pmsInvoice.CustomerPhone,
                buyerEmail = pmsInvoice.CustomerEmail,
                buyerBankAccount = pmsInvoice.BankAccount,
                buyerBankName = pmsInvoice.BankName,
                buyerNotGetInvoice = buyerNotGetInvoice, // Tạm fix mặc định không lấy HĐ nếu Pms không gửi giá trị sang
                buyerIdType  = buyerIdType,
                buyerIdNo = buyerIdNo,
                buyerBudgetCode = pmsInvoice.BudgetCode,
			};
            invoice.payments = new List<payments>();
            invoice.payments.Add(new payments
            {
                paymentMethodName = pmsInvoice.PaymentMethod,
                paymentMethod = pmsInvoice.PaymentMethodCode,
			});
            invoice.itemInfo = invoiceRequest.Details.Select((e, index) => new itemInfo
            {
                lineNumber = (index + 1).ToString(),
                itemCode = "",
                itemName = e.ItemName,
                unitCode = "",
                unitName = e.UnitName,
                unitPrice = Math.Round(e.Price ?? 0, pmsInvoice.PricePrecision ?? 0),
                quantity = e.Quantity ?? 0,
                itemTotalAmountWithoutTax = (e.SubAmount ?? 0) + (e.DiscountAmount ?? 0),
                itemTotalAmountAfterDiscount = Action == (int)InvoiceAction.AdjustDecrease ? (-1) * (e.SubAmount ?? 0) : (e.SubAmount ?? 0),
                itemTotalAmountWithTax = e.AfterTaxAmount ?? 0,
                taxPercentage = isTaxReductionItem ? -2 : e.TaxRate ?? 0,
                taxAmount = e.TaxAmount ?? 0,
                itemDiscount = e.DiscountAmount ?? 0,
                discount = e.DiscountPercent == 0 ? null : e.DiscountPercent ?? null,
                isIncreaseItem = GetItemisIncreaseItem(Action),
                adjustRatio = isTaxReductionItem ? e.TaxReductionType : null,
                expDate = e.TransactionDate,
                batchNo = e.RoomCode,
            }).ToList();
            invoice.summarizeInfo = new summarizeInfo
            {
                sumOfTotalLineAmountWithoutTax = pmsInvoice.BeforeTaxAmount ?? 0,
                totalAmountWithoutTax = pmsInvoice.BeforeTaxAmount ?? 0,
                totalTaxAmount = pmsInvoice.TaxAmount ?? 0,
                totalAmountWithTax = pmsInvoice.AfterTaxAmount ?? 0,
                totalAmountWithTaxInWords = MoneyToText.DocTienBangChu(pmsInvoice.AfterTaxAmount ?? 0),
                totalAmountAfterDiscount = notValidation ? (pmsInvoice.BeforeTaxAmount ?? 0) : null,
            };
            if(Action == (int)InvoiceAction.AdjustDecrease)
            {
                invoice.summarizeInfo.isTotalAmountPos= false;
                invoice.summarizeInfo.isTotalTaxAmountPos = false;
                invoice.summarizeInfo.isTotalAmtWithoutTaxPos = false;
                invoice.summarizeInfo.totalAmountWithTax *= -1;
            }
            if (invoiceRequest.TaxSummaries != null && invoiceRequest.TaxSummaries.Any())
            {
                invoice.taxBreakdowns = invoiceRequest.TaxSummaries.Select(e => new taxBreakdowns
                {
                    taxPercentage = e.TaxRate ?? 0,
                    taxAmount = e.TaxAmount ?? 0,
                    taxableAmount = e.BeforeTaxAmount ?? 0,
                    taxableAmountPos = Action == (int)InvoiceAction.AdjustDecrease ? false : null,
                    taxAmountPos = Action == (int)InvoiceAction.AdjustDecrease ? false : null,
                }).ToList();
            }
            if (extraDataMap != null && extraDataMap.Any())
            {
                if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
                {
                    List<ExtraFieldMapping> extraDatas = extraDataMap
                        .Where(i => (
                                        ((i.FormNo ?? "") == "" || i.FormNo == pmsInvoice.FormNo) &&
									    ((i.Serial ?? "") == "" || i.Serial == pmsInvoice.Serial)
									)
                               )
                        .ToList();
					List<metadata> convertExtraViettels = invoiceRequest.ExtraData.Join(extraDatas, p => p.Code, e => e.Code, (p, e) => new metadata
                    {
                        keyTag = e.ThirdPartyCode,
                        valueType = e.ThirdPartyDataType,
                        keyLabel = e.ThirdPartyLabel,
                        stringValue = p.StringValue,
                    }).ToList();
                    invoice.metadata = convertExtraViettels;
					// Kiểm tra nếu có metadata Customername thì phải gán buyerName theo rule của Viettel EAS-23676
					metadata metaDataCustomername = convertExtraViettels.FirstOrDefault(i => i.keyTag == "Customername");
                    if (metaDataCustomername != null && (invoice.buyerInfo.buyerLegalName ?? "") != "")
                    {
                        invoice.buyerInfo.buyerName = "";
					}
				}
                else if (invoiceRequest.HotelExtra != null)
                {
                    if (!string.IsNullOrEmpty(invoiceRequest.HotelExtra.FolioNum))
                    {
                        ExtraFieldMapping mapViettelFolioNum = extraDataMap.FirstOrDefault(e => e.Code == "FolioNum");
                        if (mapViettelFolioNum != null)
                        {
                            invoice.metadata = new List<metadata>
                            {
                                new metadata
                                {
                                    keyTag = mapViettelFolioNum.ThirdPartyCode,
                                    valueType = mapViettelFolioNum.ThirdPartyDataType,
                                    keyLabel = mapViettelFolioNum.ThirdPartyLabel,
                                    stringValue = invoiceRequest.HotelExtra.FolioNum
                                }
                            };
                        }
                    }
                }
            }
            return invoice;
        }

        private static bool? GetItemisIncreaseItem(int action)
        {
            return action == (int)InvoiceAction.AdjustIncrease ? true : (action == (int)InvoiceAction.AdjustDecrease ? false : null);
        }

        private static Adjust GetAdjustmentType(int action)
        {
            Adjust result = new Adjust
            {
                adjustmentType = "1",
                adjustmentInvoiceType = null,
            };
            switch (action)
            {
                case (int)InvoiceAction.AdjustInformation:
                    result.adjustmentType = "5";
                    result.adjustmentInvoiceType = "2";
                    break;
                case (int)InvoiceAction.AdjustIncrease:
                case (int)InvoiceAction.AdjustDecrease:
                    result.adjustmentType = "5";
                    result.adjustmentInvoiceType = "1";
                    break;
                case (int)InvoiceAction.Replace:
                    result.adjustmentType = "3";
                    break;
                case (int)InvoiceAction.Void:
                    result.adjustmentType = "7";
                    break;
            }
            return result;
        }

        private class Adjust
        {
            public string adjustmentType { get; set; }
            public string adjustmentInvoiceType { get; set; }
        }
        public static ViettelDownloadPdfRequest ConvertDownloadPdfRequest(ViettelMappingContext model)
        {
            ViettelDownloadPdfRequest result = new ViettelDownloadPdfRequest()
            {
                Company = CompanyInfo(model),
                Body = new ViettelDownloadPdfModel
                {
                    fileType = "PDF",
                    invoiceNo = model.Data.ProviderInvoiceNumber,
                    templateCode = model.Data.FormNo,
                    supplierTaxCode = model.Company.TaxNumber,
                    transactionUuid = model.Data.InvoiceId,
                }
            };
            return result;
        }

        public static ViettelVoidInvoiceRequest ConvertVoidInvoiceRequest(ViettelMappingContext model)
        {
            VoidInvoiceRequest invoice = model.Data as VoidInvoiceRequest;
            invoice.AdditionalReferenceDate ??= model.Now.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
			long invoiceDate =
                ProviderDate.ToUnixMilliseconds(invoice.InvoiceDate);
			long additionalReferenceDate =
				ProviderDate.ToUnixMilliseconds(invoice.AdditionalReferenceDate);


			ViettelVoidInvoiceRequest result = new ViettelVoidInvoiceRequest()
            {
                Company = CompanyInfo(model),
                Body = new ViettelVoidInvoiceModel
                {
                    invoiceNo = invoice.ProviderInvoiceNumber,
                    templateCode = invoice.FormNo,
                    supplierTaxCode = model.Company.TaxNumber,
					strIssueDate = invoiceDate.ToString(CultureInfo.InvariantCulture),
					additionalReferenceDate = additionalReferenceDate.ToString(CultureInfo.InvariantCulture),
                    //strIssueDate = invoice.InvoiceDate == null ? "" : DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString(),// "1666942252000",
                    //additionalReferenceDate = DateTime.ParseExact(invoice.AdditionalReferenceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString(),
                    additionalReferenceDesc = $"Hủy hóa đơn {invoice.ProviderInvoiceNumber}",
                    reasonDelete = invoice.Reason,
                }
            };
            return result;
        }

        public static ViettelSearchByIdRequest ConvertSearchByIdRequest(ViettelMappingContext model)
        {
            SearchInvoiceRequest invoice = model.Data as SearchInvoiceRequest;
            ViettelSearchByIdRequest result = new ViettelSearchByIdRequest()
            {
                Company = CompanyInfo(model),
                Body = new ViettelSearchByIdModel
                {
                    supplierTaxCode = model.Company.TaxNumber,
                    transactionUuid = invoice.InvoiceId,
                }
            };
            return result;
        }

        private static ViettelConnectionInfo CompanyInfo(ViettelMappingContext model)
        {
            return new ViettelConnectionInfo
            {
                CompanyId = Convert.ToString(model.Company.Id),
                PartnerUrl = model.Company.PartnerUrl,
                Username = model.Company.Username,
                Password = model.Company.Password,
                TaxNumber = model.Company.TaxNumber,
            };
        }
    }
}
