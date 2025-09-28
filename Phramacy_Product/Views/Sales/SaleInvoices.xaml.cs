using Phramacy_Product.DataModel;
using Phramacy_Product.Views.DBMaster;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
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
            private set
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

        private void LoadSalesData(DateTime? startDate = null, DateTime? endDate = null)
        {
            allSales.Clear();

            string query = "SELECT BillNumber, CreatedAt, BillDate, CreatedBy, CustomerName, PatientName, " +
                           "TotalAmount, PaymentType, BillPath FROM SaleDetails";

            if (startDate.HasValue && endDate.HasValue)
            {
                query += $" WHERE CreatedAt >= '{startDate.Value.ToString("yyyy-MM-dd")}' AND CreatedAt < '{endDate.Value.AddDays(1).ToString("yyyy-MM-dd")}'";
            }
            else
            {
                query += " WHERE YEAR(CreatedAt) = YEAR(GETDATE()) AND MONTH(CreatedAt) = MONTH(GETDATE())";
            }
            query += " ORDER BY CreatedAt DESC";

            try
            {
                DataTable dt = DBMasterConnection.GD(query);

                if (dt != null && dt.Rows.Count > 0)
                {
                    int srNo = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        allSales.Add(new SaleDetail
                        {
                            SrNo = srNo++,
                            BillNumber = row["BillNumber"]?.ToString(),
                            CreatedAt = row["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(row["CreatedAt"]) : (DateTime?)null,
                            BillDate = row["BillDate"] != DBNull.Value ? Convert.ToDateTime(row["BillDate"]) : (DateTime?)null,
                            CreatedBy = row["CreatedBy"]?.ToString(),
                            CustomerName = row["CustomerName"]?.ToString(),
                            PatientName = row["PatientName"]?.ToString(),
                            TotalAmount = row["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(row["TotalAmount"]) : 0,
                            PaymentStatus = row["PaymentType"]?.ToString(),
                            BillPath = row["BillPath"]?.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading data: {ex.Message}");
            }

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
            }
            else
            {
                filteredSales = allSales
                    .Where(x => x.CustomerName?.ToLower().Contains(filter) == true ||
                                x.BillNumber?.ToLower().Contains(filter) == true)
                    .ToList();
            }

            TotalPages = (int)Math.Ceiling(filteredSales.Count / (double)pageSize);
            currentPage = 1;
            DisplayCurrentPage();
        }

        private void DisplayCurrentPage()
        {
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
            createdAt = editCreatedAt.SelectedDate ?? DateTime.MinValue;
            billDate = editBillDate.SelectedDate ?? DateTime.MinValue;

            try
            {
                string query = $@"
            UPDATE SaleDetails
            SET CustomerName = '{customerName.Replace("'", "''")}',
            CreatedBy = '{entryBy.Replace("'", "''")}',
            TotalAmount = {totalAmount},
            PaymentType = '{paymentStatus.Replace("'", "''")}',
            BillDate = '{billDate.ToString("yyyy-MM-dd HH:mm:ss")}',
            CreatedAt = '{createdAt.ToString("yyyy-MM-dd HH:mm:ss")}',
            BillPath = {(string.IsNullOrWhiteSpace(billPath) ? "NULL" : $"'{billPath.Replace("'", "''")}'")}
            WHERE BillNumber = '{billNumber.Replace("'", "''")}'";

                int rowsAffected = DBMasterConnection.IUD(query);

                if (rowsAffected > 0)
                {
                    MessageBox.Show("Sale record updated successfully.");
                    EditPanel.Visibility = Visibility.Collapsed;
                    LoadSalesData();
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
                string getSaleIdQuery = $"SELECT SaleID FROM SaleDetails WHERE BillNumber='{selected.BillNumber.Replace("'", "''")}'";
                DataTable dt = DBMasterConnection.GD(getSaleIdQuery);

                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("Sale record not found.");
                    return;
                }

                int saleId = Convert.ToInt32(dt.Rows[0]["SaleID"]);
                string deleteItemsQuery = $"DELETE FROM SaleItems WHERE SaleID = {saleId}";
                DBMasterConnection.IUD(deleteItemsQuery);
                string deleteDetailsQuery = $"DELETE FROM SaleDetails WHERE SaleID = {saleId}";
                int rowsAffected = DBMasterConnection.IUD(deleteDetailsQuery);

                if (rowsAffected > 0)
                {
                    MessageBox.Show($"Record Deleted for Bill Number {selected.BillNumber}");
                    LoadSalesData();
                }
                else
                {
                    MessageBox.Show("No record was deleted.");
                }
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
            var newSalePage = new GenerateSaleInvoice.NewSalePage();
            NavigationService?.Navigate(newSalePage);
        }

        private void SaleReturn_Click(object sender, RoutedEventArgs e)
        {
            var newSaleReturn = new SaleReturn.SaleReturn();
            NavigationService?.Navigate(newSaleReturn);
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (startDate.HasValue && endDate.HasValue)
            {
                LoadSalesData(startDate, endDate);
            }
            else
            {
                MessageBox.Show("Please select both a start and end date to filter.", "Missing Dates", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            LoadSalesData(); 
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