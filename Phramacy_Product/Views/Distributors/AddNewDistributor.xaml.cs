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
using System.Windows.Shapes;
using Phramacy_Product.DataModel;

namespace Phramacy_Product.Views.Distributors
{
    public partial class AddNewDistributor : Window
    {
        public DistributorItems NewDistributorData { get; private set; } 

        public AddNewDistributor()
        {
            InitializeComponent();
            NewDistributorData = new DistributorItems(); 
            this.DataContext = NewDistributorData; 
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            NewDistributorData.DistributorName = DistributorNameTextBox.Text;
            NewDistributorData.ContactNumber = ContactNumberTextBox.Text;
            NewDistributorData.Email = EmailTextBox.Text;
            NewDistributorData.Address = AddressTextBox.Text;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}