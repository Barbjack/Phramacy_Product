using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Phramacy_Product.DataModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phramacy_Product.Views.Sales
{
    public class PdfInvoiceGenerator
    {
        public static string GenerateInvoice(SalePdfInvoice sale, List<Medicine> billingItems)
        {
            // Create document
            Document doc = new Document();
            Style normal = doc.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 9;

            Section section = doc.AddSection();

            // Seller Header (Template from screenshot)
            var sellerInfoTable = section.AddTable();
            sellerInfoTable.Borders.Width = 0;
            sellerInfoTable.AddColumn("8cm");
            sellerInfoTable.AddColumn("8cm");

            var sellerHeaderRow1 = sellerInfoTable.AddRow();
            var sellerHeaderPara = sellerHeaderRow1.Cells[0].AddParagraph("Royal Pharma");
            sellerHeaderPara.Format.Font.Size = 14;
            sellerHeaderPara.Format.Font.Bold = true;
            sellerHeaderRow1.Cells[0].MergeRight = 1;

            var sellerHeaderRow2 = sellerInfoTable.AddRow();
            sellerHeaderRow2.Cells[0].AddParagraph("Engineering Chauraha, Jankipuram Extension");
            sellerHeaderRow2.Cells[1].AddParagraph($"INV NO: {sale.BillNo}");

            var sellerHeaderRow3 = sellerInfoTable.AddRow();
            sellerHeaderRow3.Cells[0].AddParagraph("Lucknow, Uttar Pradesh 226031");
            sellerHeaderRow3.Cells[1].AddParagraph($"DATE: {sale.Date:dd-MMM-yyyy}");

            var sellerHeaderRow4 = sellerInfoTable.AddRow();
            sellerHeaderRow4.Cells[0].AddParagraph("Phone: 9995559998");
            sellerHeaderRow4.Cells[1].AddParagraph($"ROUTE: UttarPradesh");

            var sellerHeaderRow5 = sellerInfoTable.AddRow();
            sellerHeaderRow5.Cells[0].AddParagraph("GSTIN: 09AAEct1234f1z8");
            sellerHeaderRow5.Cells[1].AddParagraph($"PAN NO: AAECt1234F");

            var sellerHeaderRow6 = sellerInfoTable.AddRow();
            sellerHeaderRow6.Cells[0].AddParagraph("DL No: UP3212024, UP3212025");
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
            medHeader.Cells[4].AddParagraph("Qty");
            medHeader.Cells[5].AddParagraph("Rate");
            medHeader.Cells[6].AddParagraph("Disc%");
            medHeader.Cells[7].AddParagraph("Amount");

            foreach (var item in billingItems)
            {
                var row = medTable.AddRow();
                row.Cells[0].AddParagraph(sale.BillNo); // HSN Code from Bill Number
                row.Cells[1].AddParagraph(item.ProductName);
                row.Cells[2].AddParagraph(item.BatchNumber);
                row.Cells[3].AddParagraph(item.Expiry.ToString("MM/yy"));
                row.Cells[4].AddParagraph(item.QtyF > 0 ? item.QtyF.ToString() : item.QtyL.ToString()); // Show QtyF or QtyL
                row.Cells[5].AddParagraph(item.MRP.ToString("0.00"));
                row.Cells[6].AddParagraph(item.Discount.ToString("0.00"));
                row.Cells[7].AddParagraph(item.Total.ToString("0.00"));
            }

            // Totals Section
            decimal totalAmount = billingItems.Sum(i => i.Total);
            decimal totalGST = billingItems.Sum(i => i.GST * i.Total / 100);
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
            row1.Cells[1].AddParagraph($"Net Amount: {totalNet.ToString("0.00")}");

            var row2 = totalsTable.AddRow();
            row2.Cells[0].AddParagraph($"Payment Mode: {sale.PaymentType}");
            row2.Cells[1].AddParagraph($"SGST: {sgst.ToString("0.00")}");

            var row3 = totalsTable.AddRow();
            row3.Cells[0].AddParagraph("");
            row3.Cells[1].AddParagraph($"CGST: {cgst.ToString("0.00")}");

            var row4 = totalsTable.AddRow();
            row4.Cells[0].AddParagraph("");
            var grandTotalPara = row4.Cells[1].AddParagraph($"Grand Total: {totalAmount.ToString("0.00")}");
            grandTotalPara.Format.Font.Bold = true;

            section.AddParagraph("\n");

            var terms = section.AddParagraph("Terms & Conditions: Goods once sold will not be taken back or exchanged.");
            terms.Format.Font.Size = 8;
            var thanks = section.AddParagraph("Thank you for your business!");
            thanks.Format.Alignment = ParagraphAlignment.Center;
            thanks.Format.Font.Size = 8;

            // PDF rendering and saving
            var renderer = new PdfDocumentRenderer(true) { Document = doc };
            renderer.RenderDocument();
            string folderPath = @"C:\Users\Developer\Documents\WPF Application\Phramacy_Product\SaleInvoices\";
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            string fileName = $"Invoice_{sale.BillNo}.pdf";
            string fullPath = Path.Combine(folderPath, fileName);
            renderer.PdfDocument.Save(fullPath);
            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });

            return fullPath;
        }
    }
}