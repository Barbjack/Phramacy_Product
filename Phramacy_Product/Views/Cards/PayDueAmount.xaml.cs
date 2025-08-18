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
    public partial class PayDueAmount : UserControl, INotifyPropertyChanged
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        public ObservableCollection<NeedToPayItem> AllNeedToPayItems { get; set; } = new ObservableCollection<NeedToPayItem>();
        public ObservableCollection<NeedToPayItem> FilteredNeedToPayItems { get; set; } = new ObservableCollection<NeedToPayItem>();
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
                    UpdateFilteredNeedToPay();
                    UpdateButtonStates(); // Call this to update button states
                }
            }
        }

        private int itemsPerPage = 7;
        public int ItemsPerPage
        {
            get => itemsPerPage;
            set
            {
                itemsPerPage = value;
                OnPropertyChanged(nameof(ItemsPerPage));
                UpdateFilteredNeedToPay();
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
        public string TotalPendingPayAmount =>
      AllNeedToPayItems.Any()
       ? "- ₹" + AllNeedToPayItems
           .Select(x => Decimal.TryParse(x.PendingAmount.Replace("₹", "").Replace(",", ""), out var amt) ? amt : 0)
           .Sum()
           .ToString("N0")
       : "- ₹0";

        public PayDueAmount()
        {
            InitializeComponent();
            DataContext = this;
            LoadNeedToPay();
        }
        private void LoadNeedToPay()
        {
            AllNeedToPayItems.Clear();
            String query = @"
                   select DistributorName,BillNumber, sum(PendingAmount) as PendingAmount 
                   from PurchaseDetails
                   where IsDeleted = 0 and Status = 'Pending'
                   group by DistributorName,BillNumber
                   order by PendingAmount desc";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand com = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            NeedToPayItem needToPayItem = new NeedToPayItem();
                            needToPayItem.DistributorName = reader["DistributorName"].ToString();
                            needToPayItem.BillNumber = reader["BillNumber"].ToString();
                            needToPayItem.PendingAmount = "₹" + Convert.ToDecimal(reader["PendingAmount"]).ToString();
                            AllNeedToPayItems.Add(needToPayItem);
                        }
                    }
                }
            }
            TotalPages = (int)Math.Ceiling((double)AllNeedToPayItems.Count / ItemsPerPage);
            UpdateFilteredNeedToPay();
            UpdateButtonStates();
        }
        private void UpdateFilteredNeedToPay()
        {
            FilteredNeedToPayItems.Clear();

            int startIndex = (CurrentPage - 1) * ItemsPerPage;
            var itemsToShow = AllNeedToPayItems.Skip(startIndex).Take(ItemsPerPage);

            foreach (var item in itemsToShow)
                FilteredNeedToPayItems.Add(item);
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
