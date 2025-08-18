using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Phramacy_Product.Views.Cards
{
    public partial class ExpiringItemCard : UserControl, INotifyPropertyChanged
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        public ObservableCollection<ExpiringItem> ExpiringItems { get; set; } = new ObservableCollection<ExpiringItem>();
        public ObservableCollection<ExpiringItem> FilteredExpiringItems { get; set; } = new ObservableCollection<ExpiringItem>();

        // Pagination properties
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
                    UpdateFilteredItems();
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
                UpdateFilteredItems();
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

        public ExpiringItemCard()
        {
            InitializeComponent();
            DataContext = this;
            LoadExpiringItems();
        }

        private void LoadExpiringItems()
        {
            ExpiringItems.Clear();

            string query = @"
                SELECT  
                ItemName,
                Expiry,
                SUM(Quantity) AS TotalQuantity
            FROM SaleItems
            WHERE IsDeleted = 0 AND Is_Returned = 0 AND Expiry IS NOT NULL
            GROUP BY ItemName, Expiry
            ORDER BY Expiry ASC";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (DateTime.TryParse(reader["Expiry"].ToString(), out DateTime expiryDate))
                        {
                            ExpiringItems.Add(new ExpiringItem
                            {
                                Name = reader["ItemName"].ToString(),
                                ExpiryDate = expiryDate, // Assign the DateTime object
                                Quantity = reader["TotalQuantity"].ToString()
                            });
                        }
                    }
                }
            }

            TotalPages = (int)Math.Ceiling((double)ExpiringItems.Count / ItemsPerPage);
            UpdateFilteredItems();
            UpdateButtonStates(); // Initial state update
        }

        private void UpdateFilteredItems()
        {
            FilteredExpiringItems.Clear();

            int startIndex = (CurrentPage - 1) * ItemsPerPage;
            var itemsToShow = ExpiringItems.Skip(startIndex).Take(ItemsPerPage);

            foreach (var item in itemsToShow)
                FilteredExpiringItems.Add(item);
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