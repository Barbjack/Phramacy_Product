using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Phramacy_Product.Views.RegisterUser
{
    public partial class RegisterNewChemist : Page
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private MainWindow _mainWindow; 
        public RegisterNewChemist(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string pharmacyName = PharmacyNameTextBox.Text;
            string pharmacistName = PharmacistNameTextBox.Text;
            string mobileNumber = MobileNumberTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrEmpty(pharmacyName) || string.IsNullOrEmpty(pharmacistName) || string.IsNullOrEmpty(mobileNumber) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show("Please fill out all fields.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Passwords do not match. Please re-enter them.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var validationResult = CheckExistingUser(mobileNumber, email);
                if (validationResult != string.Empty)
                {
                    MessageBox.Show(validationResult, "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                RegisterUser(pharmacyName, pharmacistName, mobileNumber, email, password);
                MessageBox.Show("Registration successful! You can now log in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _mainWindow.GoToLoginScreen();
            }
            catch (SqlException ex)
            {
                
                if (ex.Number == 2627 || ex.Number == 2601) 
                {
                    MessageBox.Show("This mobile number is already registered. Please use a different one.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Registration failed: {ex.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string CheckExistingUser(string mobileNumber, string email)
        {
            string mobileQuery = "SELECT COUNT(1) FROM pharmacy_profile WHERE mobile = @mobileNumber";
            string emailQuery = "SELECT COUNT(1) FROM pharmacy_profile WHERE email = @email";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand mobileCommand = new SqlCommand(mobileQuery, connection))
                {
                    mobileCommand.Parameters.AddWithValue("@mobileNumber", mobileNumber);
                    int mobileCount = (int)mobileCommand.ExecuteScalar();
                    if (mobileCount > 0)
                    {
                        return "An account with this mobile number already exists. Please use a different one.";
                    }
                }
                using (SqlCommand emailCommand = new SqlCommand(emailQuery, connection))
                {
                    emailCommand.Parameters.AddWithValue("@email", email);
                    int emailCount = (int)emailCommand.ExecuteScalar();
                    if (emailCount > 0)
                    {
                        return "An account with this email address already exists. Please provide a different email.";
                    }
                }
            }
            return string.Empty; 
        }
        private void RegisterUser(string pharmacyName, string pharmacistName, string mobileNumber, string email, string password)
        {
            string query = "INSERT INTO pharmacy_profile (pharmacy_name,pharmacist_name, mobile, email, password) VALUES (@pharmacyName,@pharmacistName, @mobileNumber, @email, @password)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@pharmacyName", pharmacyName);
                    command.Parameters.AddWithValue("@pharmacistName", pharmacistName);
                    command.Parameters.AddWithValue("@mobileNumber", mobileNumber);
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@password", password);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        private void BackToLogin(object sender, RoutedEventArgs e)
        {
            _mainWindow.GoToLoginScreen();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            CheckPasswordsMatch();
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            CheckPasswordsMatch();
        }

        private void CheckPasswordsMatch()
        {
            if (string.IsNullOrEmpty(PasswordBox.Password) || string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                PasswordMismatchMessage.Visibility = Visibility.Collapsed;
                return;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                PasswordMismatchMessage.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordMismatchMessage.Visibility = Visibility.Collapsed;
            }
        }
    }
}