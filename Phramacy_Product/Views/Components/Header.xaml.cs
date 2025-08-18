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

// Add a using statement for your inventory page
using Phramacy_Product.Views.Inventory;

namespace Phramacy_Product.Views.Components
{
    public partial class Header : UserControl
    {
        public Header()
        {
            InitializeComponent();
        }
        private void NavigateToInventory(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Views.Inventory.MedicineInventory());
        }
        private void NavigateToSaleInvoices(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Views.Sales.SaleInvoices());
        }
        private void NavigateToPage(Page page)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var dashboard = mainWindow.FindName("DashboardContent") as UIElement;
                var frame = mainWindow.FindName("MainFrame") as Frame;
                if (dashboard != null && frame != null)
                {
                    dashboard.Visibility = Visibility.Collapsed;
                    frame.Visibility = Visibility.Visible;
                    frame.Navigate(page);
                }
            }

        }
        private void Home(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var dashboard = mainWindow.FindName("DashboardContent") as UIElement;
                var frame = mainWindow.FindName("MainFrame") as Frame;

                if (dashboard != null && frame != null)
                {
                    dashboard.Visibility = Visibility.Visible;
                    frame.Visibility = Visibility.Collapsed;

                    // You might want to clear the frame history here if needed
                }
            }
        }
    }
}
