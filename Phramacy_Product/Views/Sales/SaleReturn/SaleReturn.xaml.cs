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
            // Clear details from previous selections.
            // The logic to handle the search is moved to the view model.
            viewModel.SearchByBillNumber();
        }

        private void SubmitReturnButton_Click(object sender, RoutedEventArgs e)
        {
            var itemsToReturn = viewModel.PagedSaleItems.Where(i => i.IsSelected && i.ReturnQty > 0).ToList();
            if (itemsToReturn.Any())
            {
                viewModel.DbService.UpdateReturnedItems(itemsToReturn);
                MessageBox.Show("Return submitted successfully!");
                // Clear the filtered results after submission
                viewModel.PagedSaleItems.Clear();
                viewModel.CurrentSale = null;
            }
            else
            {
                MessageBox.Show("Please select at least one item to return with a quantity greater than zero.");
            }
        }
    }
}