using Newtonsoft.Json;
using Phramacy_Product.Views.Components;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Phramacy_Product.DataModel.GenerateToken;


namespace Phramacy_Product
{
    public partial class MainWindow : Window
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;

        public MainWindow()
        {
            InitializeComponent();
        }

        public enum LoginStatus
        {
            Success,
            InvalidCredentials,
            DatabaseError,
            LicenseError,
            InternetError,
            TokenError
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string mobile = MobileNumberTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(mobile) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both mobile number and password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoginStatus status = await AuthenticateUser(mobile, password);

            switch (status)
            {
                case LoginStatus.Success:
                    Dashboard dashboardWindow = new Dashboard();
                    this.Close();
                    dashboardWindow.Show();
                    Clear_Form();
                    break;
                case LoginStatus.InvalidCredentials:
                    MessageBox.Show("Invalid mobile number or password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case LoginStatus.LicenseError:
                       break;
                case LoginStatus.DatabaseError:
                    break;
                case LoginStatus.InternetError:
                    break;
                case LoginStatus.TokenError:
                        break;
                default:
                    MessageBox.Show("An unknown error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }
        private async Task<LoginStatus> AuthenticateUser(string mobile, string password)
        {
            string query = "SELECT id, pharmacist_name, mobile FROM pharmacy_profile WHERE mobile = @mobile AND password = @password";

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
                            GlobalData.userId = (int)reader["id"];
                            //string mobile = reader["mobile"].ToString();
                            reader.Close();

                            var tokenManager = new TokenManager();
                            var tokenStatus = await tokenManager.GetOrRefreshToken(mobile);

                            if (tokenStatus != TokenManager.TokenStatus.Success)
                            {
                                return LoginStatus.TokenError;
                            }

                            return LoginStatus.Success;
                        }
                        else
                        {
                            return LoginStatus.InvalidCredentials;
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return LoginStatus.DatabaseError;
            }
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