using Newtonsoft.Json;
using Phramacy_Product.Views.Components;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net.NetworkInformation;


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
            InternetError
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
                default:
                    MessageBox.Show("An unknown error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private async Task<LoginStatus> AuthenticateUser(string mobile, string password)
        {
            string query = "SELECT id, pharmacist_name, expiryDate, license_key, email FROM pharmacy_profile WHERE mobile = @mobile AND password = @password";

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
                            string licenseKey = reader["license_key"] as string;
                            string email = reader["email"].ToString();
                           
                            if (string.IsNullOrEmpty(licenseKey))
                            {
                                if (!await CreateAndSaveLicense(GlobalData.LoggedInUser, email))
                                {
                                    return LoginStatus.LicenseError;
                                }
                            }
                            else
                            {
                                DateTime? expiryDate = reader["expiryDate"] as DateTime?;
                                if (expiryDate == null || expiryDate.Value.Date < DateTime.Now.Date)
                                {
                                    if (!await ValidateLicense(licenseKey))
                                    {
                                        return LoginStatus.LicenseError;
                                    }
                                }
                            }
                            reader.Close();
                            return LoginStatus.Success;
                        }
                        else
                        {
                            return LoginStatus.InvalidCredentials;      }
                    }
                }
            }
            catch (Exception ex)
            {
                 MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return LoginStatus.DatabaseError;
            }
        }

        private async Task<bool> CreateAndSaveLicense(string name, string email)
        {
            
            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    name = name,
                    email = email,
                    plan = "yearly",
                    duration = 365
                };
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync("https://quickrxbill.com/api/create_license.php", content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                    if (jsonResponse.success == true)
                    {
                        string newLicenseKey = jsonResponse.license_key;
                        DateTime expiryDate = jsonResponse.expiry;
                        UpdateLocalLicense(newLicenseKey, expiryDate);
                         return true;
                    }
                    else
                    {
                         return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Please connect to internet.", "Connection Error");
                    return false;
                }
            }
        }
        private async Task<bool> ValidateLicense(string licenseKey)
        {
            
            using (var client = new HttpClient())
            {
                try
                {
                    string url = $"https://quickrxbill.com/api/validate.php?key={licenseKey}";
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                    if (jsonResponse.success == true && jsonResponse.message== "License valid")
                    {
                        DateTime expiryDate = jsonResponse.expiry;
                        UpdateLocalLicense(licenseKey, expiryDate);
                        MessageBox.Show("License is valid and active.", "Success");
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("License is invalid or expired. Please contact support.", "Warning");
                        UpdateLocalLicense(null, DateTime.MinValue);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Please connect to the internet: {ex.Message}", "Error");
                    return false;
                }
            }
        }

        private void UpdateLocalLicense(string licenseKey, DateTime expiryDate)
        {
            if (!string.IsNullOrEmpty(licenseKey))
            {
                string query = "UPDATE pharmacy_profile SET license_key = @licenseKey, expiryDate = @expiryDate WHERE Id = @userId";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@licenseKey", licenseKey);
                            command.Parameters.AddWithValue("@expiryDate", expiryDate);
                            command.Parameters.AddWithValue("@userId", GlobalData.userId);

                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update local license: {ex.Message}", "Error");
                    // Remove 'return;' as this method is void.
                }
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