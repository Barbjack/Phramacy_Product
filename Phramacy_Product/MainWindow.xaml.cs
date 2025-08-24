using Phramacy_Product.Views.Components;
using Phramacy_Product.Views.Sales; // Add this using statement
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Phramacy_Product
{
    public partial class MainWindow : Window
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string mobile = MobileNumberTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(mobile) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both mobile number and password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AuthenticateUser(mobile, password))
            {
                Dashboard dashboardWindow = new Dashboard();
                this.Close();
                dashboardWindow.Show();
                MessageBox.Show("Logged in Successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Clear_Form();
            }
            else
            {
                MessageBox.Show("Invalid mobile number or password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AuthenticateUser(string mobile, string password)
        {
            bool isAuthenticated = false;
            string query = "SELECT * FROM pharmacy_profile WHERE mobile = @mobile AND password = @password";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@mobile", mobile);
                        command.Parameters.AddWithValue("@password", password);

                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            GlobalData.LoggedInUser = reader["pharmacist_name"].ToString();
                            GlobalData.userId = (int)reader["Id"];
                            isAuthenticated = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return isAuthenticated;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Clear_Form()
        {
            MobileNumberTextBox.Clear();
            PasswordBox.Clear();
        }
    }
}