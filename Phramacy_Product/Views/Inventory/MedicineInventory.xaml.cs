using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Phramacy_Product.DataModel;

namespace Phramacy_Product.Views.Inventory
{
    public partial class MedicineInventory : Page,INotifyPropertyChanged
    {
        private readonly DataService dataService = new DataService();
        private int currentPage = 1;
        private readonly int pageSize = 11;
        public ObservableCollection<PharmaMedicine> Medicines { get; set; } = new ObservableCollection<PharmaMedicine>();

        public int CurrentPage
        {
            get => currentPage;
            set
            {
                if (currentPage != value)
                {
                    currentPage = value;
                    OnPropertyChanged();
                    LoadMedicines();
                }
            }
        }
        public int TotalPages { get; set; }
        public MedicineInventory()
        {
            InitializeComponent();
            this.DataContext = this;
            LoadMedicines();
        }

        private void LoadMedicines()
        {
            Medicines.Clear();
            var totalCount = dataService.GetTotalMedicineCount();
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var medicineList = dataService.GetMedicines(currentPage, pageSize);
            foreach (var medicine in medicineList)
            {
                Medicines.Add(medicine);
            }
        }

        private void AddMedicineClick(object sender, RoutedEventArgs e)
        {
            var addMedicineWindow = new AddMedicineWindow();
            if (addMedicineWindow.ShowDialog() == true)
            {
                dataService.AddMedicine(addMedicineWindow.NewMedicine);
                LoadMedicines();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PharmaMedicine selectedMedicine)
            {
                // Pass the selected medicine to the window
                var editWindow = new AddMedicineWindow(selectedMedicine);
                if (editWindow.ShowDialog() == true)
                {
                    dataService.UpdateMedicine(editWindow.NewMedicine);
                    LoadMedicines();
                }
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PharmaMedicine selectedMedicine)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {selectedMedicine.Name}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    dataService.DeleteMedicine(selectedMedicine.Id);
                    LoadMedicines();
                }
            }
        }

        private void FirstPageClick(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                CurrentPage = 1;
                LoadMedicines();
            }
        }

        private void PreviousPageClick(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                CurrentPage--;
                LoadMedicines();
            }
        }

        private void NextPageClick(object sender, RoutedEventArgs e)
        {
            if (currentPage < TotalPages)
            {
                CurrentPage++;
                LoadMedicines();
            }
        }

        private void LastPageClick(object sender, RoutedEventArgs e)
        {
            if (currentPage < TotalPages)
            {
                CurrentPage = TotalPages;
                LoadMedicines();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}