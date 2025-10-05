using Phramacy_Product.DataModel;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows;
using System.Windows.Controls;
using Phramacy_Product.Views.Sales.SaleReturn;
using System.Data.SqlClient;
namespace Phramacy_Product.Views.Sales.SaleReturn
{
    public partial class SaleReturn : Page
    {
        private readonly SaleReturnViewModel viewModel = new SaleReturnViewModel();
        public SaleReturn()
        {
            InitializeComponent();
            viewModel.SelectedMember = GlobalData.LoggedInUser;
            this.DataContext = viewModel;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SearchByBillNumber();
        }

        [System.Obsolete]
        private void SubmitReturnButton_Click(object sender, RoutedEventArgs e)
        {
            if (formCreatedBy.Text == null)
            {
                MessageBox.Show("Please select an owner before proceeding.");
                return;
            }
            string createdBy = formCreatedBy.Text;
            var itemsToReturn = viewModel.PagedSaleItems.Where(i => i.IsSelected && i.ReturnQty > 0).ToList();
            if (itemsToReturn.Any())
            {
                try
                {
                    viewModel.DbService.ProcessSaleReturn(itemsToReturn, viewModel.CurrentSale, createdBy);
                    MessageBox.Show("Return submitted successfully!");
                    var updatedSaleItems = viewModel.DbService.GetSaleItemsBySaleId(viewModel.CurrentSale.SaleID);
                    var invoiceData = new SalePdfInvoice
                    {
                        BillNo = viewModel.CurrentSale.BillNumber,
                        CustomerName = viewModel.CurrentSale.CustomerName,
                        Date = (System.DateTime)viewModel.CurrentSale.BillDate,
                        PaymentType = viewModel.CurrentSale.PaymentStatus

                    };
                    string billPath = PdfInvoiceGenerator.GenerateRevisedInvoice(invoiceData, updatedSaleItems, itemsToReturn);
                    viewModel.DbService.updateInvoiceBill(billPath, invoiceData.BillNo);
                    viewModel.PagedSaleItems.Clear();
                    viewModel.CurrentSale = null;
                    viewModel.ReturnTotal = 0;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"An error occurred while processing the return: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select at least one item to return with a quantity greater than zero.");
            }
        }
        
    }
}