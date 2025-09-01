using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
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

namespace Phramacy_Product.Views.Purchase.PurchasePayment
{
    
    public partial class PaymentOut : Page, INotifyPropertyChanged
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private List<PurchaseDetail> allSalePayment = new List<PurchaseDetail>();
        private ObservableCollection<PurchaseDetail> displayedPayment = new ObservableCollection<PurchaseDetail>();
        private int currentPage = 1;
        private int pageSize = 10;
        private int totalPages = 1;
        private string selectedTimePeriod;
        private decimal totalRevenue;
        public PaymentOut()
        {
           
            InitializeComponent();
            this.DataContext = this;
            SearchBox.TextChanged += SearchBox_TextChanged;
            GetSalesFromDatabase();
            SelectedTimePeriod = "All Time";
        }
  
            public event PropertyChangedEventHandler PropertyChanged;

            public ObservableCollection<PurchaseDetail> DisplayedPayment
            {
                get => displayedPayment;
                set
                {
                    displayedPayment = value;
                    OnPropertyChanged(nameof(DisplayedPayment));
                }
            }

            public int CurrentPage
            {
                get => currentPage;
                set
                {
                    if (currentPage != value)
                    {
                        currentPage = value;
                        OnPropertyChanged(nameof(CurrentPage));
                        DisplayCurrentPage();
                    }
                }
            }

            public int TotalPages
            {
                get => totalPages;
                private set
                {
                    if (totalPages != value)
                    {
                        totalPages = value;
                        OnPropertyChanged(nameof(TotalPages));
                    }
                }
            }

            public string SelectedTimePeriod
            {
                get => selectedTimePeriod;
                set
                {
                    selectedTimePeriod = value;
                    OnPropertyChanged(nameof(SelectedTimePeriod));
                    FilterAndDisplayData(); // Call this to re-filter and update revenue
                }
            }

            public decimal TotalRevenue
            {
                get => totalRevenue;
                set
                {
                    totalRevenue = value;
                    OnPropertyChanged(nameof(TotalRevenue));
                }
            }

            

            private void GetSalesFromDatabase()
            {
                allSalePayment.Clear();
                string query = @"SELECT * FROM PurchaseDetails WHERE isDeleted = 0;";
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        SqlCommand cmd = new SqlCommand(query, conn);
                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            allSalePayment.Add(new PurchaseDetail()
                            {
                                BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber")) ? null : reader.GetString(reader.GetOrdinal("BillNumber")),
                                CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                BillDate = reader.IsDBNull(reader.GetOrdinal("BillDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("BillDate")),
                                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                                DistributorName = reader.IsDBNull(reader.GetOrdinal("DistributorName")) ? null : reader.GetString(reader.GetOrdinal("DistributorName")),
                              
                                PaidAmount = reader.IsDBNull(reader.GetOrdinal("PaidAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("PaidAmount")),
                                PaymentType = reader.IsDBNull(reader.GetOrdinal("PaymentType")) ? null : reader.GetString(reader.GetOrdinal("PaymentType"))
                            });
                        }
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while loading data: {ex.Message}");
                }

                FilterAndDisplayData();
            }

            private void FilterAndDisplayData()
            {
                IEnumerable<PurchaseDetail> timeFilteredData = allSalePayment;
                string timePeriodString = SelectedTimePeriod?.ToString() ?? "All Time";
                string timePeriodContent = null;

                if (timePeriodString.Contains(":"))
                {
                    timePeriodContent = timePeriodString.Split(':')[1].Trim();
                    SelectedTimePeriod = timePeriodContent;
                }
                else
                {
                    timePeriodContent = timePeriodString;
                }
                if (timePeriodContent == "This Month")
                {
                    timeFilteredData = timeFilteredData.Where(s => s.CreatedAt?.Month == DateTime.Now.Month && s.CreatedAt?.Year == DateTime.Now.Year);
                }
                else if (timePeriodContent == "Last Month")
                {
                    DateTime lastMonth = DateTime.Now.AddMonths(-1);
                    timeFilteredData = timeFilteredData.Where(s => s.CreatedAt?.Month == lastMonth.Month && s.CreatedAt?.Year == lastMonth.Year);
                }
                else if (timePeriodContent == "This Year")
                {
                    timeFilteredData = timeFilteredData.Where(s => s.CreatedAt?.Year == DateTime.Now.Year);
                }


                TotalRevenue = timeFilteredData.Sum(s => s.PaidAmount);

                // Now, apply the text search filter to the time-filtered data
                string searchText = SearchBox.Text.ToLower();
                IEnumerable<PurchaseDetail> finalFilteredData = timeFilteredData.Where(s =>
                    (s.DistributorName != null && s.DistributorName.ToLower().Contains(searchText)) ||
                    (s.BillNumber != null && s.BillNumber.ToLower().Contains(searchText)) ||
                    (s.PaymentType != null && s.PaymentType.ToLower().Contains(searchText)) ||
                    s.PaidAmount.ToString().Contains(searchText)
                );

                // Update pagination properties based on the final filtered data
                TotalPages = (int)Math.Ceiling((double)finalFilteredData.Count() / pageSize);
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (CurrentPage < 1 && TotalPages > 0) CurrentPage = 1;
                if (TotalPages == 0) CurrentPage = 0;

                // Display the paged data
                var pagedData = finalFilteredData.Skip((CurrentPage - 1) * pageSize).Take(pageSize).ToList();
                DisplayedPayment = new ObservableCollection<PurchaseDetail>(pagedData);
            }

            private void DisplayCurrentPage()
            {
                FilterAndDisplayData();
            }

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            // Event Handlers
            private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            {
                CurrentPage = 1;
                FilterAndDisplayData();
            }

            private void FirstPageClick(object sender, RoutedEventArgs e)
            {
                CurrentPage = 1;
            }

            private void PreviousPageClick(object sender, RoutedEventArgs e)
            {
                if (CurrentPage > 1)
                {
                    CurrentPage--;
                }
            }

            private void NextPageClick(object sender, RoutedEventArgs e)
            {
                if (CurrentPage < TotalPages)
                {
                    CurrentPage++;
                }
            }

            private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                CurrentPage = 1;
                FilterAndDisplayData();
            }
            private void LastPageClick(object sender, RoutedEventArgs e)
            {
                CurrentPage = TotalPages;
            }
        }

        public class NullToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
}