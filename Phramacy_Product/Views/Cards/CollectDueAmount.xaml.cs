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
    public partial class CollectDueAmount : UserControl, INotifyPropertyChanged
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        public ObservableCollection<RecordDueAmount> CollectDueAmounts { get; set; } = new ObservableCollection<RecordDueAmount>();
        public ObservableCollection<RecordDueAmount> FilleteredDueAmounts { get; set; } = new ObservableCollection<RecordDueAmount>();
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
                    updateCollectDueAmount();
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
                updateCollectDueAmount();
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
        public string CollectDueAmountsTotal =>
        CollectDueAmounts.Any() ? CollectDueAmounts
        .Select(x => Decimal.TryParse(x.DueAmount.Replace("₹", "").Replace(",", ""), out var amt) ? amt : 0)
        .Sum().ToString("N0") : "0";
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public CollectDueAmount()
        {
            InitializeComponent();
            DataContext = this;
            LoadDueAmount();
        }
        private void LoadDueAmount()
        {
            CollectDueAmounts.Clear();
            String query = @" 
                   select CustomerName, Mobile, sum(PendingAmount) as PendingAmount
                  from PharmaCustomers 
                  where IsDeleted=0 
                  group by CustomerName, Mobile
                  order by PendingAmount desc;";

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand com = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader reader = com.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        RecordDueAmount obj = new RecordDueAmount();
                        obj.CustomerName = reader["CustomerName"].ToString();
                        obj.MobileNumber = reader["Mobile"].ToString();
                        obj.DueAmount = "₹" + reader["PendingAmount"].ToString();
                        CollectDueAmounts.Add(obj);
                    }
                }

            }
            TotalPages = (int)Math.Ceiling((double)CollectDueAmounts.Count / ItemsPerPage);
            updateCollectDueAmount();
            UpdateButtonStates();
        }
        private void updateCollectDueAmount()
        {
            FilleteredDueAmounts.Clear();
            int startIndex = (CurrentPage - 1) * ItemsPerPage;
            var items = CollectDueAmounts.Skip(startIndex).Take(ItemsPerPage);
            foreach (var item in items)
                FilleteredDueAmounts.Add(item);


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
