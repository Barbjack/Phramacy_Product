using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
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

namespace Phramacy_Product.Views.Cards
{
    public partial class SalesMargin : UserControl, INotifyPropertyChanged
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        public ObservableCollection<SaleItems> AllFastestSaleItems { get; set; } = new ObservableCollection<SaleItems>();
        public ObservableCollection<SaleItems> FilteredFastestSaleItems { get; set; } = new ObservableCollection<SaleItems>();
        private int currentPage = 1;
        public int CurrentPage
        {
            get => currentPage;
            set
            {
                if (currentPage != value)
                {
                    currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                    UpdateFilteredFastestSale();
                    UpdateButtonStates(); // Call this to update button states
                }
            }
        }

        private int itemsPerPage = 8;
        public int ItemsPerPage
        {
            get => itemsPerPage;
            set
            {
                itemsPerPage = value;
                OnPropertyChanged(nameof(ItemsPerPage));
                UpdateFilteredFastestSale();
                UpdateButtonStates(); // Call this to update button states
            }
        }

        private int totalPages;
        public int TotalPages
        {
            get => totalPages;
            set
            {
                totalPages = value;
                OnPropertyChanged(nameof(TotalPages));
                UpdateButtonStates(); // Call this to update button states
            }
        }

        // New properties for button states
        private bool isPreviousEnabled = false;
        public bool IsPreviousEnabled
        {
            get => isPreviousEnabled;
            set
            {
                if (isPreviousEnabled != value)
                {
                    isPreviousEnabled = value;
                    OnPropertyChanged(nameof(IsPreviousEnabled));
                }
            }
        }

        private bool isNextEnabled = false;
        public bool IsNextEnabled
        {
            get => isNextEnabled;
            set
            {
                if (isNextEnabled != value)
                {
                    isNextEnabled = value;
                    OnPropertyChanged(nameof(IsNextEnabled));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public string TotalSaleAmount =>
      AllFastestSaleItems.Any()
       ? " ₹" + AllFastestSaleItems
           .Select(x => Decimal.TryParse(x.NetAmount.Replace("₹", "").Replace(",", ""), out var amt) ? amt : 0)
           .Sum()
           .ToString("N0")
       : " ₹0";

        public SalesMargin()
        {
            InitializeComponent();
            DataContext = this;
            LoadFastestSaleItems();
        }
        private void LoadFastestSaleItems()
        {
            AllFastestSaleItems.Clear();
            String query = @"SELECT ItemName,Batch,MRP,ItemId,SUM(Quantity) AS TotalSold,sum(NetAmount) as SoldAmount
                           FROM SaleItems WHERE IsDeleted = 0 AND Is_Returned = 0 AND CreatedAt >= DATEADD(month, -3, GETDATE())
                           GROUP BY ItemName,Batch,MRP,ItemId
                           ORDER BY TotalSold DESC;";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand com = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SaleItems saleItems = new SaleItems();
                            saleItems.ItemName = reader["ItemName"].ToString();
                            saleItems.Batch= reader["Batch"].ToString();
                            saleItems.MRP = Convert.ToDecimal(reader["MRP"]);
                            saleItems.FullQty = Convert.ToInt32(reader["TotalSold"]);
                            saleItems.NetAmount = "₹" + Convert.ToDecimal(reader["SoldAmount"]).ToString();
                            AllFastestSaleItems.Add(saleItems);
                        }
                    }
                }
            }
            TotalPages = (int)Math.Ceiling((double)AllFastestSaleItems.Count / ItemsPerPage);
            UpdateFilteredFastestSale();
            UpdateButtonStates();
        }
        private void UpdateFilteredFastestSale()
        {
            FilteredFastestSaleItems.Clear();

            int startIndex = (CurrentPage - 1) * ItemsPerPage;
            var itemsToShow = AllFastestSaleItems.Skip(startIndex).Take(ItemsPerPage);

            foreach (var item in itemsToShow)
                FilteredFastestSaleItems.Add(item);
        }
        private void UpdateButtonStates()
        {
            IsPreviousEnabled = CurrentPage > 1;
            IsNextEnabled = CurrentPage < TotalPages;
        }

        private void FirstPageClick(object sender, RoutedEventArgs e)
        {
            if (IsPreviousEnabled)
            {
                CurrentPage = 1;

            }
        }

        private void PreviousPageClick(object sender, RoutedEventArgs e)
        {
            if (IsPreviousEnabled)
            {
                CurrentPage--;
            }
        }

        private void NextPageClick(object sender, RoutedEventArgs e)
        {
            if (IsNextEnabled)
            {
                CurrentPage++;
            }
        }

        private void LastPageClick(object sender, RoutedEventArgs e)
        {
            if (IsNextEnabled)
            {
                CurrentPage = TotalPages;

            }
        }
    }
}
