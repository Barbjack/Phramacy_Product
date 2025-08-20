using Phramacy_Product.DataModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace Phramacy_Product.Views.Sales
{
    public partial class SaleReturn : Page
    {
        private readonly SaleReturnViewModel viewModel;

        public SaleReturn()
        {
            InitializeComponent();
            viewModel = new SaleReturnViewModel();
            this.DataContext = viewModel;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SearchByBillNumber();
        }

        private void SubmitReturnButton_Click(object sender, RoutedEventArgs e)
        {
            if (ownerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an owner before proceeding.");
                return;
            }
            string createdBy = ((System.Windows.Controls.ComboBoxItem)ownerComboBox.SelectedItem).Content.ToString();
            var itemsToReturn = viewModel.PagedSaleItems.Where(i => i.IsSelected && i.ReturnQty > 0).ToList();
            if (itemsToReturn.Any())
            {
                try
                {
                    viewModel.DbService.ProcessSaleReturn(itemsToReturn, viewModel.CurrentSale,createdBy);
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
                    viewModel.PagedSaleItems.Clear();
                    viewModel.CurrentSale = null;
                    viewModel.ReturnTotal = 0;
                }
                catch (System.Exception ex)
                {
                    // The exception message is already handled in the DatabaseService, but you can add a generic message here
                }
            }
            else
            {
                MessageBox.Show("Please select at least one item to return with a quantity greater than zero.");
            }
        }
    }
}