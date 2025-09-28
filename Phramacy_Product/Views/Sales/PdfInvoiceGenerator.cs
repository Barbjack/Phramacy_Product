using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Phramacy_Product.DataModel;
using Phramacy_Product.Views.DBMaster;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phramacy_Product.Views.Sales
{
    public class PdfInvoiceGenerator
    {
        [System.Obsolete]
        public static string GenerateInvoice(SalePdfInvoice sale, List<Medicine> billingItems)
        {
            // Create document
            Document doc = new Document();
            Style normal = doc.Styles["Normal"];
            normal.Font.Name = "cour";
            normal.Font.Size = 9;

            Section section = doc.AddSection();

            //Seller Header
            PharmacyProfile pharmaProfile = getPharmaProfileDetails(GlobalData.userId);
            var sellerInfoTable = section.AddTable();
            sellerInfoTable.Borders.Width = 0;
            sellerInfoTable.AddColumn("8cm");
            sellerInfoTable.AddColumn("8cm");

            var sellerHeaderRow1 = sellerInfoTable.AddRow();
            var sellerHeaderPara = sellerHeaderRow1.Cells[0].AddParagraph($"{pharmaProfile.pharmacy_name}");
            sellerHeaderPara.Format.Font.Size = 14;
            sellerHeaderPara.Format.Font.Bold = true;
            sellerHeaderRow1.Cells[0].MergeRight = 1;

            var sellerHeaderRow2 = sellerInfoTable.AddRow();
            sellerHeaderRow2.Cells[0].AddParagraph($"{pharmaProfile.address}" + "," + $"{pharmaProfile.address2}" + "," + $"{pharmaProfile.area}");
            sellerHeaderRow2.Cells[1].AddParagraph($"INV NO: {sale.BillNo}");

            var sellerHeaderRow3 = sellerInfoTable.AddRow();

            sellerHeaderRow3.Cells[0].AddParagraph($"{pharmaProfile.city}" + "," + $"{pharmaProfile.state}" + " " + $"{pharmaProfile.pincode}");
            sellerHeaderRow3.Cells[1].AddParagraph($"DATE: {sale.Date:dd-MMM-yyyy}");

            var sellerHeaderRow4 = sellerInfoTable.AddRow();
            sellerHeaderRow4.Cells[0].AddParagraph($"Phone: {pharmaProfile.mobile}");
            sellerHeaderRow4.Cells[1].AddParagraph($"ROUTE: {pharmaProfile.state}");

            var sellerHeaderRow5 = sellerInfoTable.AddRow();
            sellerHeaderRow5.Cells[0].AddParagraph($"GSTIN: {pharmaProfile.gstin}");
            sellerHeaderRow5.Cells[1].AddParagraph($"PAN NO: {pharmaProfile.panno}");

            var sellerHeaderRow6 = sellerInfoTable.AddRow();
            sellerHeaderRow6.Cells[0].AddParagraph($"DL No: {pharmaProfile.dlno}");
            sellerHeaderRow6.Cells[1].AddParagraph("");

            section.AddParagraph("\n");

            // Customer Info
            var customerInfoPara = section.AddParagraph($"CUSTOMER NAME: {sale.CustomerName}");
            customerInfoPara.Format.Font.Bold = true;
            section.AddParagraph("\n");

            // Medicine Table
            var medTable = section.AddTable();
            medTable.Borders.Width = 0.5;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("5cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("1cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("1cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("1cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;

            // Table Header
            var medHeader = medTable.AddRow();
            medHeader.Shading.Color = Colors.LightGray;
            medHeader.Format.Font.Bold = true;
            medHeader.Cells[0].AddParagraph("HSN Code");
            medHeader.Cells[1].AddParagraph("Item Description");
            medHeader.Cells[2].AddParagraph("Batch No");
            medHeader.Cells[3].AddParagraph("Exp Date");
            medHeader.Cells[4].AddParagraph("Qty Full");
            medHeader.Cells[5].AddParagraph("Qty Loose");
            medHeader.Cells[6].AddParagraph("Rate");
            medHeader.Cells[7].AddParagraph("Disc%");
            medHeader.Cells[8].AddParagraph("Amount");

            foreach (var item in billingItems)
            {
                var row = medTable.AddRow();
                row.Cells[0].AddParagraph(sale.BillNo); // HSN Code from Bill Number
                row.Cells[1].AddParagraph(item.ProductName);
                row.Cells[2].AddParagraph(item.BatchNumber);
                row.Cells[3].AddParagraph(item.Expiry.ToString("MM/yy"));
                row.Cells[4].AddParagraph(item.QtyF.ToString());
                row.Cells[5].AddParagraph(item.QtyL.ToString());
                row.Cells[6].AddParagraph(item.MRP.ToString("0.00"));
                row.Cells[7].AddParagraph(item.Discount.ToString("0.00"));
                row.Cells[8].AddParagraph(item.Total.ToString("0.00"));
            }

            // Totals Section
            decimal totalAmount = billingItems.Sum(i => i.Total);
            // Corrected GST calculation for original invoice
            decimal totalGST = billingItems.Sum(i => i.Total * (i.GST / (100 + i.GST)));
            decimal totalNet = totalAmount - totalGST;
            decimal sgst = totalGST / 2;
            decimal cgst = totalGST / 2;

            section.AddParagraph("\n");

            var totalsTable = section.AddTable();
            totalsTable.Borders.Width = 0;
            totalsTable.AddColumn("10cm");
            totalsTable.AddColumn("6cm");

            var row1 = totalsTable.AddRow();
            row1.Cells[0].AddParagraph($"Total Items: {billingItems.Count}");
            row1.Cells[1].AddParagraph($"Net Amount: {totalNet:0.00}");

            var row2 = totalsTable.AddRow();
            row2.Cells[0].AddParagraph($"Payment Mode: {sale.PaymentType}");
            row2.Cells[1].AddParagraph($"SGST: {sgst:0.00}");

            var row3 = totalsTable.AddRow();
            row3.Cells[0].AddParagraph("");
            row3.Cells[1].AddParagraph($"CGST: {cgst:0.00}");

            var row4 = totalsTable.AddRow();
            row4.Cells[0].AddParagraph("");
            var grandTotalPara = row4.Cells[1].AddParagraph($"Grand Total: {totalAmount:0.00}");
            grandTotalPara.Format.Font.Bold = true;

            section.AddParagraph("\n");

            var terms = section.AddParagraph("Terms & Conditions: Goods once sold will not be taken back or exchanged.");
            terms.Format.Font.Size = 8;
            var thanks = section.AddParagraph("Thank you for your business!");
            thanks.Format.Alignment = ParagraphAlignment.Center;
            thanks.Format.Font.Size = 8;

            var renderer = new PdfDocumentRenderer(true) { Document = doc };
            renderer.RenderDocument();
            string folderPath = @"C:\Users\Developer\Desktop\SaleInvoices";
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            string fileName = $"Invoice_{sale.BillNo}.pdf";
            string fullPath = Path.Combine(folderPath, fileName);
            renderer.PdfDocument.Save(fullPath);
            // Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            return fullPath;
        }

        [System.Obsolete]
        public static string GenerateRevisedInvoice(SalePdfInvoice sale, List<SaleItemReturn> allSaleItems, List<SaleItemReturn> returnedItems)
        {
            // If no items remain, do not generate the PDF
            if (!allSaleItems.Any())
            {
                return "All items have been returned. No revised invoice generated.";
            }

            // Create document
            Document doc = new Document();
            Style normal = doc.Styles["Normal"];
            normal.Font.Name = "cour";
            normal.Font.Size = 9;

            Section section = doc.AddSection();

            // Add "REVISED INVOICE" header
            var headerPara = section.AddParagraph("REVISED INVOICE");
            headerPara.Format.Font.Size = 16;
            headerPara.Format.Font.Bold = true;
            headerPara.Format.Font.Color = Colors.Red;
            headerPara.Format.Alignment = ParagraphAlignment.Center;
            section.AddParagraph("\n");

            // Seller Header
            PharmacyProfile pharmaProfile = getPharmaProfileDetails(GlobalData.userId);
            var sellerInfoTable = section.AddTable();
            sellerInfoTable.Borders.Width = 0;
            sellerInfoTable.AddColumn("8cm");
            sellerInfoTable.AddColumn("8cm");

            var sellerHeaderRow1 = sellerInfoTable.AddRow();
            var sellerHeaderPara = sellerHeaderRow1.Cells[0].AddParagraph($"{pharmaProfile.pharmacy_name}");
            sellerHeaderPara.Format.Font.Size = 14;
            sellerHeaderPara.Format.Font.Bold = true;
            sellerHeaderRow1.Cells[0].MergeRight = 1;

            var sellerHeaderRow2 = sellerInfoTable.AddRow();
            sellerHeaderRow2.Cells[0].AddParagraph($"{pharmaProfile.address}" + "," + $"{pharmaProfile.address2}" + "," + $"{pharmaProfile.area}");
            sellerHeaderRow2.Cells[1].AddParagraph($"INV NO: {sale.BillNo}");

            var sellerHeaderRow3 = sellerInfoTable.AddRow();
            sellerHeaderRow3.Cells[0].AddParagraph($"{pharmaProfile.city}" + "," + $"{pharmaProfile.state}" + "," + $"{pharmaProfile.pincode}");
            sellerHeaderRow3.Cells[1].AddParagraph($"DATE: {sale.Date:dd-MMM-yyyy}");

            var sellerHeaderRow4 = sellerInfoTable.AddRow();
            sellerHeaderRow4.Cells[0].AddParagraph($"Phone: {pharmaProfile.mobile}");
            sellerHeaderRow4.Cells[1].AddParagraph($"ROUTE: {pharmaProfile.state}");

            var sellerHeaderRow5 = sellerInfoTable.AddRow();
            sellerHeaderRow5.Cells[0].AddParagraph($"GSTIN: {pharmaProfile.gstin}");
            sellerHeaderRow5.Cells[1].AddParagraph($"PAN NO: {pharmaProfile.panno}");

            var sellerHeaderRow6 = sellerInfoTable.AddRow();
            sellerHeaderRow6.Cells[0].AddParagraph($"DL No: {pharmaProfile.dlno}");
            sellerHeaderRow6.Cells[1].AddParagraph("");

            section.AddParagraph("\n");

            // Customer Info
            var customerInfoPara = section.AddParagraph($"CUSTOMER NAME: {sale.CustomerName}");
            customerInfoPara.Format.Font.Bold = true;
            section.AddParagraph("\n");

            // Remaining Items Table
            var medTable = section.AddTable();
            medTable.Borders.Width = 0.5;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("5cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("1cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("1cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("1cm").Format.Alignment = ParagraphAlignment.Center;
            medTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center;

            // Table Header
            var medHeader = medTable.AddRow();
            medHeader.Shading.Color = Colors.LightGray;
            medHeader.Format.Font.Bold = true;
            medHeader.Cells[0].AddParagraph("HSN Code");
            medHeader.Cells[1].AddParagraph("Item Description");
            medHeader.Cells[2].AddParagraph("Batch No");
            medHeader.Cells[3].AddParagraph("Exp Date");
            medHeader.Cells[4].AddParagraph("Full Qty");
            medHeader.Cells[5].AddParagraph("Loose Qty");
            medHeader.Cells[6].AddParagraph("Rate");
            medHeader.Cells[7].AddParagraph("Disc%");
            medHeader.Cells[8].AddParagraph("Amount");

            // Populate the table with only the remaining items
            foreach (var item in allSaleItems)
            {
                var row = medTable.AddRow();
                row.Cells[0].AddParagraph(sale.BillNo);
                row.Cells[1].AddParagraph(item.ItemName);
                row.Cells[2].AddParagraph(item.Batch);
                row.Cells[3].AddParagraph(item.Expiry);
                row.Cells[4].AddParagraph(item.FullQty.ToString());
                row.Cells[5].AddParagraph(item.LooseQty.ToString());
                row.Cells[6].AddParagraph(item.MRP.ToString("0.00"));
                row.Cells[7].AddParagraph(item.Discount.ToString("0.00"));
                row.Cells[8].AddParagraph(item.NetAmount.ToString("0.00"));
            }

            // Totals Section (Calculated from remainingItems)
            decimal newTotalAmount = allSaleItems.Sum(i => i.NetAmount);

            // Corrected GST calculation for revised invoice
            decimal totalGST = allSaleItems.Sum(i => i.NetAmount * (i.GST / (100 + i.GST)));

            decimal totalNet = newTotalAmount - totalGST;
            decimal sgst = totalGST / 2;
            decimal cgst = totalGST / 2;

            section.AddParagraph("\n");

            var totalsTable = section.AddTable();
            totalsTable.Borders.Width = 0;
            totalsTable.AddColumn("10cm");
            totalsTable.AddColumn("6cm");

            var row1 = totalsTable.AddRow();
            row1.Cells[0].AddParagraph($"Total Items: {allSaleItems.Count}");
            row1.Cells[1].AddParagraph($"Net Amount: {totalNet:0.00}");

            var row2 = totalsTable.AddRow();
            row2.Cells[0].AddParagraph($"Payment Mode: {sale.PaymentType}");
            row2.Cells[1].AddParagraph($"SGST: {sgst:0.00}");

            var row3 = totalsTable.AddRow();
            row3.Cells[0].AddParagraph("");
            row3.Cells[1].AddParagraph($"CGST: {cgst:0.00}");

            var row4 = totalsTable.AddRow();
            row4.Cells[0].AddParagraph("");
            var grandTotalPara = row4.Cells[1].AddParagraph($"Grand Total: {newTotalAmount:0.00}");
            grandTotalPara.Format.Font.Bold = true;
            grandTotalPara.Format.Font.Size = 14;
            grandTotalPara.Format.Font.Color = Colors.DarkRed;

            section.AddParagraph("\n");

            var terms = section.AddParagraph("Terms & Conditions: This is a revised invoice. The returned items have been credited.");
            terms.Format.Font.Size = 8;
            var thanks = section.AddParagraph("Thank you for your business!");
            thanks.Format.Alignment = ParagraphAlignment.Center;
            thanks.Format.Font.Size = 8;

            // PDF rendering and saving
            var renderer = new PdfDocumentRenderer(true) { Document = doc };
            renderer.RenderDocument();
            string folderPath = @"C:\Users\Developer\Desktop\SaleInvoices";


            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            string fileName = $"RevisedInvoice_{sale.BillNo}.pdf";
            string fullPath = Path.Combine(folderPath, fileName);
            renderer.PdfDocument.Save(fullPath);
            //Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });

            return fullPath;
        }
        private static PharmacyProfile getPharmaProfileDetails(int userId)
        {
            string query = $@"SELECT id, pharmacy_name, pharmacist_name, panno, dlno, gstin, mobile, email, password, address, address2, area, pincode, city, state, company_logo, signature, created_at, updated_at, is_deleted 
    FROM pharmacy_profile WHERE id = {userId} and is_deleted = 0";

            PharmacyProfile profile = null;

            try
            {
                DataTable dt = DBMasterConnection.GD(query);

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow reader = dt.Rows[0];
                    profile = new PharmacyProfile
                    {
                        pharmacy_name = reader["pharmacy_name"] as string ?? string.Empty,
                        pharmacist_name = reader["pharmacist_name"] as string ?? string.Empty,
                        mobile = reader["mobile"] as string ?? string.Empty,
                        email = reader["email"] as string ?? string.Empty,
                        gstin = reader["gstin"] as string ?? string.Empty,
                        panno = reader["panno"] as string ?? string.Empty,
                        dlno = reader["dlno"] as string ?? string.Empty,
                        address = reader["address"] as string ?? string.Empty,
                        address2 = reader["address2"] as string ?? string.Empty,
                        area = reader["area"] as string ?? string.Empty,
                        pincode = Convert.ToString(reader["pincode"]) ?? string.Empty,
                        city = reader["city"] as string ?? string.Empty,
                        state = reader["state"] as string ?? string.Empty,
                        company_logo = reader["company_logo"] as byte[],
                        signature = reader["signature"] as byte[],
                        created_at = reader.Field<DateTime?>("created_at") ?? DateTime.MinValue,
                        updated_at = reader.Field<DateTime?>("updated_at") ?? DateTime.MinValue
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving pharmacy profile: {ex.Message}");
            }
            return profile;
        }
    }
}