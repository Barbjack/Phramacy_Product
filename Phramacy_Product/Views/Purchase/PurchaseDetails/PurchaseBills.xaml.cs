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

namespace Phramacy_Product.Views.Purchase.PurchaseDetails
{
    public partial class PurchaseBills : Page, INotifyPropertyChanged
    {
        private List<PurchaseDetail> allPurchases = new List<PurchaseDetail>();
        private List<PurchaseDetail> filteredPurchases = new List<PurchaseDetail>();
        private int currentPage = 1;
        private int totalPages = 1;
        private readonly int pageSize = 11;
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private PurchaseDetail currentEditPurchase;

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

        public PurchaseBills()
        {
            InitializeComponent();
            DataContext = this;
            this.PreviewMouseDown += Page_PreviewMouseDown;
            SearchBox.TextChanged += SearchBox_TextChanged;
            LoadPurchaseData();
        }

        private void LoadPurchaseData()
        {
            allPurchases.Clear();
            string query = @"select * from PurchaseDetails where isDeleted=0;";
                //"SELECT BillNumber, CreatedAt, BillDate, CreatedBy, DistributorName, PaidAmount, PendingAmount, ReturnAmount, PaymentType FROM PurchaseDetails WHERE IsDeleted = 0";

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
                            allPurchases.Add(new PurchaseDetail
                            {
                                SrNo = srNo++,
                                BillNumber = reader["BillNumber"]?.ToString(),
                                CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime?)reader["CreatedAt"] : null,
                                BillDate = reader["BillDate"] != DBNull.Value ? (DateTime?)reader["BillDate"] : null,
                                CreatedBy = reader["CreatedBy"]?.ToString(),
                                DistributorName = reader["DistributorName"]?.ToString(),
                                PaidAmount = reader["PaidAmount"] != DBNull.Value ? Convert.ToDecimal(reader["PaidAmount"]) : 0,
                                PendingAmount = reader["PendingAmount"] != DBNull.Value ? Convert.ToDecimal(reader["PendingAmount"]) : 0,
                                ReturnAmount = reader["ReturnAmount"] != DBNull.Value ? Convert.ToDecimal(reader["ReturnAmount"]) : 0,
                                PaymentType = reader["PaymentType"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Database error: {ex.Message}");
                Trace.WriteLine($"SQL Error: {ex.Message}"); // Use Trace.WriteLine for detailed logging
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading data: {ex.Message}");
                Trace.WriteLine($"General Error: {ex.Message}");
            }

            // After loading all data, apply the current search filter
            SearchBox_TextChanged(null, null);
            //DisplayCurrentPage(); // This is called inside SearchBox_TextChanged
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
                filteredPurchases = allPurchases;
            }
            else
            {
                filteredPurchases = allPurchases
                    .Where(x => x.DistributorName?.ToLower().Contains(filter) == true ||
                                x.BillNumber?.ToLower().Contains(filter) == true)
                    .ToList();
            }

            TotalPages = (int)Math.Ceiling(filteredPurchases.Count / (double)pageSize);
            currentPage = 1; // Reset to the first page for the new search results
            DisplayCurrentPage();
        }

        private void DisplayCurrentPage()
        {
            if (filteredPurchases.Count == 0)
            {
                PurchasesDataGrid.ItemsSource = null;
                return;
            }

            if (currentPage < 1) currentPage = 1;
            if (currentPage > TotalPages) currentPage = TotalPages;

            var pagedData = filteredPurchases
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            PurchasesDataGrid.ItemsSource = pagedData;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            currentEditPurchase = (sender as FrameworkElement)?.DataContext as PurchaseDetail;
            if (currentEditPurchase == null) return;

            editBillNumber.Text = currentEditPurchase.BillNumber;
            editDistributorName.Text = currentEditPurchase.DistributorName;
            editCreatedAt.SelectedDate = currentEditPurchase.CreatedAt;
            editBillDate.SelectedDate = currentEditPurchase.BillDate;
            editCreatedBy.Text = currentEditPurchase.CreatedBy;
            editPaidAmount.Text = currentEditPurchase.PaidAmount.ToString();
            editPendingAmount.Text = currentEditPurchase.PendingAmount.ToString();
            editReturnAmount.Text = currentEditPurchase.ReturnAmount.ToString();
            editPaymentType.Text = currentEditPurchase.PaymentType;

            EditPanel.Visibility = Visibility.Visible;
        }

        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            if (currentEditPurchase == null)
            {
                MessageBox.Show("No purchase selected for editing.");
                return;
            }

            string distributorName = editDistributorName.Text;
            string entryBy = editCreatedBy.Text;
            string paymentType = editPaymentType.Text;
            DateTime? createdAt = editCreatedAt.SelectedDate;
            DateTime? billDate = editBillDate.SelectedDate;

            if (!decimal.TryParse(editPaidAmount.Text, out decimal paidAmount))
            {
                MessageBox.Show("Invalid paid amount.");
                return;
            }

            if (!decimal.TryParse(editPendingAmount.Text, out decimal pendingAmount))
            {
                MessageBox.Show("Invalid pending amount.");
                return;
            }

            if (!decimal.TryParse(editReturnAmount.Text, out decimal returnAmount))
            {
                MessageBox.Show("Invalid return amount.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"
                        UPDATE PurchaseDetails
                        SET DistributorName = @DistributorName,
                        CreatedBy = @CreatedBy,
                        PaidAmount = @PaidAmount,
                        PendingAmount = @PendingAmount,
                        ReturnAmount = @ReturnAmount,
                        PaymentType = @PaymentType,
                        BillDate = @BillDate,
                        CreatedAt = @CreatedAt
                        WHERE BillNumber = @BillNumber";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@DistributorName", distributorName);
                    cmd.Parameters.AddWithValue("@CreatedBy", entryBy);
                    cmd.Parameters.AddWithValue("@PaidAmount", paidAmount);
                    cmd.Parameters.AddWithValue("@PendingAmount", pendingAmount);
                    cmd.Parameters.AddWithValue("@ReturnAmount", returnAmount);
                    cmd.Parameters.AddWithValue("@PaymentType", paymentType);
                    cmd.Parameters.AddWithValue("@BillDate", (object)billDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", (object)createdAt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BillNumber", currentEditPurchase.BillNumber);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Purchase record updated successfully.");
                        EditPanel.Visibility = Visibility.Collapsed;
                        LoadPurchaseData(); // Refresh the DataGrid
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating record: " + ex.Message);
                Trace.WriteLine($"Update Error: {ex.Message}");
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as FrameworkElement)?.DataContext as PurchaseDetail;
            if (selected == null)
            {
                MessageBox.Show("No record selected for deletion.");
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the purchase bill with number {selected.BillNumber}?",
                                      "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        // Using a soft delete (updating IsDeleted to 1) is generally safer than a hard delete.
                        string query = "UPDATE PurchaseDetails SET IsDeleted = 1 WHERE BillNumber = @BillNumber";
                        SqlCommand com = new SqlCommand(query, con);
                        com.Parameters.AddWithValue("@BillNumber", selected.BillNumber);
                        con.Open();
                        com.ExecuteNonQuery();
                    }
                    MessageBox.Show($"Record for Bill Number {selected.BillNumber} has been deleted.");
                    LoadPurchaseData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting record: {ex.Message}");
                    Trace.WriteLine($"Delete Error: {ex.Message}");
                }
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
            currentEditPurchase = null;
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
            if (SearchBox.Visibility == Visibility.Visible && !SearchBox.IsKeyboardFocusWithin && !SearchButton.IsMouseOver)
            {
                SearchBox.Visibility = Visibility.Collapsed;
            }
        }

        // Navigation methods
        private void NewPurchaseButton_Click(object sender, RoutedEventArgs e)
        {
          NavigationService?.Navigate(new PurchaseGenerate.NewPurchaseGenerate());
        }

        private void PurchaseReturn_Click(object sender, RoutedEventArgs e)
        {
          NavigationService?.Navigate(new PurchaseReturn.PurchaseReturn());
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