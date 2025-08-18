using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Phramacy_Product.Views.Sales
{
    public partial class SaleInvoices : Page, INotifyPropertyChanged
    {
        private List<SaleDetail> allSales = new List<SaleDetail>();
        private List<SaleDetail> filteredSales = new List<SaleDetail>();
        private int currentPage = 1;
        private int totalPages = 1;
        private readonly int pageSize = 11;
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private SaleDetail currentEditSale;

        public int CurrentPage
        {
            get => currentPage;
            set
            {
                if (currentPage != value)
                {
                    currentPage = value;
                    OnPropertyChanged();
                    DisplayCurrentPage();
                }
            }
        }

        public int TotalPages
        {
            get => totalPages;
            private set // Changed to private set to control updates internally
            {
                if (totalPages != value)
                {
                    totalPages = value;
                    OnPropertyChanged();
                }
            }
        }

        public SaleInvoices()
        {
            InitializeComponent();
            DataContext = this;
            this.PreviewMouseDown += Page_PreviewMouseDown;
            SearchBox.TextChanged += SearchBox_TextChanged;
            LoadSalesData();
        }

        private void LoadSalesData()
        {
            allSales.Clear();
            string query = "SELECT BillNumber, CreatedAt, BillDate, CreatedBy, CustomerName, PatientName, " +
                           "TotalAmount, PaymentType, BillPath FROM SaleDetails";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand com = new SqlCommand(query, conn))
                    using (SqlDataReader reader = com.ExecuteReader())
                    {
                        int srNo = 1;
                        while (reader.Read())
                        {
                            allSales.Add(new SaleDetail
                            {
                                SrNo = srNo++,
                                BillNumber = reader["BillNumber"]?.ToString(),
                                CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : (DateTime?)null,
                                BillDate = reader["BillDate"] != DBNull.Value ? Convert.ToDateTime(reader["BillDate"]) : (DateTime?)null,
                                CreatedBy = reader["CreatedBy"]?.ToString(),
                                CustomerName = reader["CustomerName"]?.ToString(),
                                PatientName = reader["PatientName"]?.ToString(),
                                TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                                PaymentStatus = reader["PaymentType"]?.ToString(),
                                BillPath = reader["BillPath"]?.ToString()
                            });
                        }
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

            // After loading all data, apply the current search filter
            SearchBox_TextChanged(null, null);
            DisplayCurrentPage();
        }
        private void FirstPageClick(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                CurrentPage = 1;
            }
        }

        private void PreviousPageClick(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                CurrentPage--;
            }
        }

        private void NextPageClick(object sender, RoutedEventArgs e)
        {
            if (currentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        private void LastPageClick(object sender, RoutedEventArgs e)
        {
            if (currentPage < TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(filter))
            {
                filteredSales = allSales;
                TotalPages = (int)Math.Ceiling(allSales.Count / (double)pageSize);
                CurrentPage = 1;
                DisplayCurrentPage();
            }
            else
            {
                filteredSales = allSales
                    .Where(x => x.CustomerName?.ToLower().Contains(filter) == true ||
                                x.BillNumber?.ToLower().Contains(filter) == true)
                    .ToList();
                SalesDataGrid.ItemsSource = filteredSales;
                TotalPages = (int)Math.Ceiling(filteredSales.Count / (double)pageSize);
                currentPage = 1;
                DisplayCurrentPage();
            }
        }
       
        private void DisplayCurrentPage()
        {
            // Ensure currentPage is not out of bounds
            if (currentPage < 1) currentPage = 1;
            if (currentPage > TotalPages) currentPage = TotalPages;

            var pagedData = filteredSales
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            SalesDataGrid.ItemsSource = pagedData;
        }
        

        private void ViewPDF_Click(object sender, RoutedEventArgs e)
        {
            string path = (sender as Button)?.Tag as string;

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("No PDF file path is available for this record.", "No Path", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (System.IO.File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"The specified PDF file was not found at: {path}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while trying to open the PDF: {ex.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            currentEditSale = (sender as FrameworkElement)?.DataContext as SaleDetail;
            if (currentEditSale == null) return;
            else
            {
                editBillNumber.Text = currentEditSale.BillNumber;
                editCreatedAt.SelectedDate = currentEditSale.CreatedAt;
                editBillDate.SelectedDate = currentEditSale.BillDate;
                editEntryBy.Text = currentEditSale.CreatedBy;
                editCustomerName.Text = currentEditSale.CustomerName;
                editBillAmount.Text = currentEditSale.TotalAmount.ToString();
                editPayMode.Text = currentEditSale.PaymentStatus;
                editBillPdf.Text = currentEditSale.BillPath;
                EditPanel.Visibility = Visibility.Visible;
            }
        }

        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            if (currentEditSale == null)
            {
                MessageBox.Show("No sale selected for editing.");
                return;
            }

            string billNumber = editBillNumber.Text;
            string customerName = editCustomerName.Text;
            string entryBy = editEntryBy.Text;
            string billPath = editBillPdf.Text;
            string paymentStatus = editPayMode.Text;
            decimal totalAmount;
            DateTime billDate, createdAt;

            if (!decimal.TryParse(editBillAmount.Text, out totalAmount))
            {
                MessageBox.Show("Invalid bill amount.");
                return;
            }

            // Using SelectedDate from DatePicker, which is more reliable than parsing a string from Text
            createdAt = editCreatedAt.SelectedDate ?? DateTime.MinValue;
            billDate = editBillDate.SelectedDate ?? DateTime.MinValue;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"
                        UPDATE SaleDetails
                        SET CustomerName = @CustomerName,
                        CreatedBy = @CreatedBy,
                        TotalAmount = @TotalAmount,
                        PaymentType = @PaymentType,
                        BillDate = @BillDate,
                        CreatedAt = @CreatedAt,
                        BillPath = @BillPath
                        WHERE BillNumber = @BillNumber";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@CustomerName", customerName);
                    cmd.Parameters.AddWithValue("@CreatedBy", entryBy);
                    cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    cmd.Parameters.AddWithValue("@PaymentType", paymentStatus);
                    cmd.Parameters.AddWithValue("@BillDate", billDate);
                    cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                    cmd.Parameters.AddWithValue("@BillPath", string.IsNullOrWhiteSpace(billPath) ? (object)DBNull.Value : billPath);
                    cmd.Parameters.AddWithValue("@BillNumber", billNumber);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Sale record updated successfully.");
                        EditPanel.Visibility = Visibility.Collapsed;
                        LoadSalesData(); // Refresh the DataGrid
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating record: " + ex.Message);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as FrameworkElement)?.DataContext as SaleDetail;
            if (selected == null)
            {
                MessageBox.Show("No record selected for deletion.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand com = new SqlCommand("DELETE FROM SaleDetails WHERE BillNumber=@BillNumber", con);
                    com.Parameters.AddWithValue("@BillNumber", selected.BillNumber);
                    con.Open();
                    com.ExecuteNonQuery();
                }
                MessageBox.Show($"Record Deleted for Bill Number {selected.BillNumber}");
                LoadSalesData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting record: {ex.Message}");
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
            currentEditSale = null;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Visibility = Visibility.Visible;
            SearchBox.Focus();
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Keep the search box visible if it has content, for better user experience
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Visibility = Visibility.Collapsed;
            }
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SearchBox.Visibility == Visibility.Visible &&
                !SearchBox.IsKeyboardFocusWithin &&
                !SearchBox.IsMouseOver &&
                !SearchButton.IsMouseOver)
            {
                SearchBox.Visibility = Visibility.Collapsed;
            }
        }

        private void NewSaleButton_Click(object sender, RoutedEventArgs e)
        {
            var newSalePage = new NewSalePage();
            NavigationService?.Navigate(newSalePage);
        }

        private void SaleReturn_Click(object sender, RoutedEventArgs e)
        {
            var newSaleReturn = new SaleReturn();
            NavigationService?.Navigate(newSaleReturn);
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