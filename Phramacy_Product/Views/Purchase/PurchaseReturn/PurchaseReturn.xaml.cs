using Phramacy_Product.DataModel;
using Phramacy_Product.Views.Purchase;
using Phramacy_Product.Views.Purchase.PurchaseReturn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Phramacy_Product.Views.Purchase.PurchaseReturn
{
    public partial class PurchaseReturn : Page
    {
        private readonly PurchaseReturnViewModel viewModel = new PurchaseReturnViewModel();
        
        public PurchaseReturn()
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
                var itemsToReturn = viewModel.PagedPurchaseItems.Where(i => i.IsSelected && i.ReturnQty > 0).ToList();
                if (itemsToReturn.Any())
                {
                    try
                    {
                        viewModel.DbService.ProcessPurchaseReturn(itemsToReturn, viewModel.CurrentPurchase, createdBy);
                        MessageBox.Show("Return submitted successfully!");
                        var updatedPurchaseItems = viewModel.DbService.GetPurchaseItemsBySaleId(viewModel.CurrentPurchase.PurchaseID);
                       
                        viewModel.PagedPurchaseItems.Clear();
                        viewModel.CurrentPurchase = null;
                        viewModel.ReturnTotal = 0;
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"An error occurred while processing the return: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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