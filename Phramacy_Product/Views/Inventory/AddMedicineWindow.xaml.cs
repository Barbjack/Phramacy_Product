using System;
using System.Windows;
using Phramacy_Product.DataModel;

namespace Phramacy_Product.Views.Inventory
{
    public partial class AddMedicineWindow : Window
    {
        public PharmaMedicine EditedMedicine { get; set; }
        public PharmaMedicine NewMedicine => EditedMedicine;
        public string WindowTitle { get; set; }
        public AddMedicineWindow()
        {
            InitializeComponent();
            this.EditedMedicine = new PharmaMedicine();
            this.DataContext = this;
            this.WindowTitle = "Add Medicine Form";
        }
        public AddMedicineWindow(PharmaMedicine medicineToEdit)
        {
            InitializeComponent();
            this.EditedMedicine = medicineToEdit;
            this.DataContext = this;
            this.WindowTitle = "Edit Medicine Form";
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
