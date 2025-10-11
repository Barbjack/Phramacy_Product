using Newtonsoft.Json;
using Phramacy_Product.DataModel.GenerateToken;
using Phramacy_Product.Views.Components;
using Phramacy_Product.Views.DBMaster;
using Phramacy_Product.Views.RegisterUser; 
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Phramacy_Product
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (Properties.Settings.Default.IsLoggedInBefore &&
            !string.IsNullOrEmpty(Properties.Settings.Default.LastMobileNumber) &&
            !string.IsNullOrEmpty(Properties.Settings.Default.EncryptedPassword))
            {
                MobileNumberTextBox.Text = Properties.Settings.Default.LastMobileNumber;
                RememberMeCheckBox.IsChecked = true;
                string decryptedPassword = DBMasterConnection.Decrypt(Properties.Settings.Default.EncryptedPassword);

                if (!string.IsNullOrEmpty(decryptedPassword))
                {
                    PasswordBox.Password = decryptedPassword;
                }
            }
        }
        
        private void Clear_Saved_Credentials()
        {
            Properties.Settings.Default.LastMobileNumber = string.Empty;
            Properties.Settings.Default.EncryptedPassword = string.Empty;
            Properties.Settings.Default.IsLoggedInBefore = false;
            Properties.Settings.Default.Save();
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

            LoginButton.IsEnabled = false;
            LoginStatus status = await AuthenticateUser(mobile, password);
            LoginButton.IsEnabled = true;

            switch (status)
            {
                case LoginStatus.Success:
                    if (RememberMeCheckBox.IsChecked == true)
                    {
                        Properties.Settings.Default.LastMobileNumber = mobile;
                        Properties.Settings.Default.EncryptedPassword = DBMasterConnection.Encrypt(password);
                        Properties.Settings.Default.IsLoggedInBefore = true;
                    }
                    else
                    {
                        Clear_Saved_Credentials();
                    }
                    Properties.Settings.Default.Save();
                    Dashboard dashboardWindow = new Dashboard();
                    this.Close();
                    dashboardWindow.Show();
                    Clear_Form();
                    break;
                case LoginStatus.InvalidCredentials:
                    MessageBox.Show("Invalid mobile number or password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case LoginStatus.TokenError:
                    MessageBox.Show("Failed to get or refresh token. Please try again.", "Token Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case LoginStatus.DatabaseError:
                    MessageBox.Show("A database error occurred. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case LoginStatus.LicenseError: 
                case LoginStatus.InternetError: 
                default:
                    MessageBox.Show("An unknown error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }
        private async Task<LoginStatus> AuthenticateUser(string mobile, string password)
        {
            //string query = "SELECT id,pharmacy_name,pharmacist_name, mobile,email FROM pharmacy_profile WHERE mobile = @mobile AND password = @password";
            string query = $"SELECT id,pharmacy_name,pharmacist_name, mobile,email FROM pharmacy_profile " +
                $"WHERE mobile = '{mobile}' AND password = '{password}'";
            try
            {
                DataTable dt = DBMasterConnection.GD(query);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow reader = dt.Rows[0];
                    GlobalData.LoggedInUser = reader["pharmacist_name"].ToString();
                    GlobalData.userId = (int)reader["id"];
                    GlobalData.pharmacyName = reader["pharmacy_name"].ToString();
                    GlobalData.mobile = reader["mobile"].ToString();
                    GlobalData.email = reader["email"].ToString();

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
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return LoginStatus.DatabaseError;
            }
        }

        private void RegisterChemist(object sender, RoutedEventArgs e)
        {
            LoginStackPanel.Visibility = Visibility.Collapsed;
            MainFrame.Visibility = Visibility.Visible;

            MainFrame.Navigate(new RegisterNewChemist(this)); 
        }
        public void GoToLoginScreen()
        {
            MainFrame.Visibility = Visibility.Collapsed;
            LoginStackPanel.Visibility = Visibility.Visible;
            Clear_Form();
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            LoginStackPanel.Visibility = Visibility.Collapsed;
            MainFrame.Visibility = Visibility.Visible;
            MainFrame.Navigate(new Views.ForgotPassword.ForgotPasswordPage(this));
        }
        private void Clear_Form()
        {
            MobileNumberTextBox.Clear();
            PasswordBox.Clear();
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}