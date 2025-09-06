using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

namespace Phramacy_Product.Views.Sales.GenerateSaleInvoice
{
    public class PdfPrintManager
    {
        private readonly string _filePath;
        public PdfPrintManager(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified file was not found.", filePath);
            }
            _filePath = filePath;
        }

        public void Print()
        {
            try
            {
                using (var printDocument = new PrintDocument())
                {
                    printDocument.DocumentName = Path.GetFileName(_filePath);
                    var printDialog = new PrintDialog
                    {
                        Document = printDocument
                    };
                    if (printDialog.ShowDialog() == DialogResult.OK)
                    {
                        string printerName = printDialog.PrinterSettings.PrinterName;
                         var psi = new ProcessStartInfo
                        {
                            FileName = _filePath,
                            Verb = "printto",
                            Arguments = $"\"{printerName}\"",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred during printing.", ex);
            }
        }
    }
}