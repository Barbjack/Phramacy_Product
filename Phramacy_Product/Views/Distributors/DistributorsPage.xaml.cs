using MaterialDesignThemes.Wpf;
using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Phramacy_Product.Views.Distributors
{
    public partial class DistributorsPage : Page, INotifyPropertyChanged
    {
        private List<DistributorItems> allDistributors = new List<DistributorItems>();
        private List<DistributorItems> filteredDistributors = new List<DistributorItems>();
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private int currentPage = 1;
        private int totalPages = 1;
        private DistributorItems currentDistributorItem;
        private const int pageSize = 10;

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

        public DistributorsPage()
        {
            InitializeComponent();
            DataContext = this;
            this.PreviewMouseDown += Page_PreviewMouseDown;
            SearchBox.TextChanged += SearchBox_TextChanged;
            LoadDistributors();
        }

        private void LoadDistributors()
        {
            allDistributors.Clear(); // Clear the list before reloading
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT  
                            DistributorID,
                            Name, 
                            ContactNumber, 
                            Email, 
                            Address, 
                            CreatedAt, 
                            ModifiedAt
                        FROM Distributors
                        WHERE IsDeleted = 0
                        ORDER BY DistributorID;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            allDistributors.Add(new DistributorItems
                            {
                                DistributorID = (int)reader["DistributorID"],
                                DistributorName = reader["Name"].ToString(),
                                ContactNumber = reader["ContactNumber"].ToString(),
                                Email = reader["Email"].ToString(),
                                Address = reader["Address"].ToString(),
                                CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : (DateTime?)null,
                                ModifiedAt = reader["ModifiedAt"] != DBNull.Value ? Convert.ToDateTime(reader["ModifiedAt"]) : (DateTime?)null
                                //CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.MinValue,
                                //ModifiedAt = reader["ModifiedAt"] != DBNull.Value ? Convert.ToDateTime(reader["ModifiedAt"]) : DateTime.MaxValue
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading distributors: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
           
            SearchBox_TextChanged(null, null);
        }

        private void DisplayCurrentPage()
        {
            // Ensure currentPage is within a valid range
            if (currentPage < 1) currentPage = 1;
            if (currentPage > TotalPages && TotalPages > 0) currentPage = TotalPages;

            var pagedData = filteredDistributors
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            DistributorDataGrid.ItemsSource = pagedData;
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
                filteredDistributors = allDistributors;
            }
            else
            {
                filteredDistributors = allDistributors
                    .Where(x => x.DistributorName?.ToLower().Contains(filter) == true ||
                                 x.ContactNumber?.ToLower().Contains(filter) == true ||
                                 x.Email?.ToLower().Contains(filter) == true ||
                                 x.Address?.ToLower().Contains(filter) == true)
                    .ToList();
            }

            TotalPages = (int)Math.Ceiling(filteredDistributors.Count / (double)pageSize);
            currentPage = 1;
            DisplayCurrentPage();
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

        private void CreateNewDistributor_Click(object sender, RoutedEventArgs e)
        {
             var addNewDistributor = new AddNewDistributor();
            if (addNewDistributor.ShowDialog() == true)
            {
                addNewDistributorToDistributorsPage(addNewDistributor.NewDistributorData);
            } 
        }
        private void addNewDistributorToDistributorsPage(DistributorItems newDistributorItem)
        {
            string query = "INSERT INTO Distributors (name, ContactNumber, Email, Address, CreatedAt) " +
                           "VALUES (@Name, @ContactNumber, @Email, @Address, @CreatedAt);";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand com = new SqlCommand(query, conn))
                {
                    com.Parameters.AddWithValue("@Name", newDistributorItem.DistributorName);
                    com.Parameters.AddWithValue("@ContactNumber", newDistributorItem.ContactNumber);
                    com.Parameters.AddWithValue("@Email", newDistributorItem.Email);
                    com.Parameters.AddWithValue("@Address", newDistributorItem.Address);
                    com.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    conn.Open();
                    int rowsAffected = com.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Distributor added successfully!");
                    }
                    else
                    {
                        MessageBox.Show("Failed to add distributor.");
                    }
                }
            }
            LoadDistributors();
        }
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            currentDistributorItem = (sender as FrameworkElement)?.DataContext as DistributorItems;
            if (currentDistributorItem == null) return;
            else
            {
                // Ensure UI controls are updated correctly
                editDistributorName.Text = currentDistributorItem.DistributorName;
                editContactNumber.Text = currentDistributorItem.ContactNumber;
                editEmail.Text = currentDistributorItem.Email;
                editAddress.Text = currentDistributorItem.Address;
                editCreatedAt.Text = currentDistributorItem.CreatedAt?.ToString("MM-dd-yyyy");
                editModifiedAt.Text = currentDistributorItem.ModifiedAt?.ToString("MM-dd-yyyy");
                EditPanel.Visibility = Visibility.Visible;
            }
        }

        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            if (currentDistributorItem == null)
            {
                MessageBox.Show("No Distributor selected for editing.");
                return;
            }

            string name = editDistributorName.Text;
            string contactNumber = editContactNumber.Text;
            string email = editEmail.Text;
            string address = editAddress.Text;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        UPDATE Distributors
                        SET 
                            Name = @Name,
                            ContactNumber = @ContactNumber,
                            Email = @Email,
                            Address = @Address,
                            ModifiedAt = @ModifiedAt
                        WHERE DistributorID = @DistributorID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@ContactNumber", contactNumber);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Address", address);
                    cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@DistributorID", currentDistributorItem.DistributorID);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        EditPanel.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Distributor record updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        LoadDistributors(); // Refresh the DataGrid
                    }
                    else
                    {
                        MessageBox.Show("No records were updated. Distributor ID may not exist.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as FrameworkElement)?.DataContext as DistributorItems;
            if (selected == null)
            {
                MessageBox.Show("No record selected for deletion.");
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete {selected.DistributorName}?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        SqlCommand com = new SqlCommand("UPDATE Distributors SET IsDeleted = 1, ModifiedAt = @ModifiedAt WHERE DistributorID = @DistributorID", con);
                        com.Parameters.AddWithValue("@DistributorID", selected.DistributorID);
                        com.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                        con.Open();
                        com.ExecuteNonQuery();
                    }
                    MessageBox.Show("Record Deleted successfully.");
                    LoadDistributors();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting record: {ex.Message}");
                }
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
            currentDistributorItem = null;
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