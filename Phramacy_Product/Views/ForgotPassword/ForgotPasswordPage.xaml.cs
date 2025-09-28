using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Phramacy_Product.Views.ForgotPassword
{
    public partial class ForgotPasswordPage : Page
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private MainWindow mainWindow;
        private string otp; // Variable to store the generated OTP

        public ForgotPasswordPage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void BackToLogin(object sender, RoutedEventArgs e)
        {
            mainWindow.GoToLoginScreen();
        }

        private void SendOtpButton_Click(object sender, RoutedEventArgs e)
        {
            string mobileNumber = MobileNumberTextBox.Text;

            if (string.IsNullOrEmpty(mobileNumber))
            {
                MessageBox.Show("Please enter your mobile number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (CheckUserByMobile(mobileNumber))
            {
                // Simulate sending an OTP
                otp = GenerateOtp();
                MessageBox.Show($"OTP has been sent to your mobile number. Your OTP is: {otp}", "OTP Sent", MessageBoxButton.OK, MessageBoxImage.Information);

                MobileInputSection.Visibility = Visibility.Collapsed;
                OtpAndNewPasswordSection.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Mobile number not found. Please check and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredOtp = OtpTextBox.Text;
            string newPassword = NewPasswordBox.Password;
            string confirmNewPassword = ConfirmNewPasswordBox.Password;

            if (string.IsNullOrEmpty(enteredOtp) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmNewPassword))
            {
                MessageBox.Show("Please fill out all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newPassword != confirmNewPassword)
            {
                PasswordMismatchMessage.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                PasswordMismatchMessage.Visibility = Visibility.Collapsed;
            }

            if (enteredOtp == otp)
            {
                if (UpdatePassword(MobileNumberTextBox.Text, newPassword))
                {
                    MessageBox.Show("Password has been reset successfully. You can now log in with your new password.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    mainWindow.GoToLoginScreen();
                }
                else
                {
                    MessageBox.Show("Failed to update password. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Invalid OTP. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckUserByMobile(string mobileNumber)
        {
            string query = "SELECT COUNT(1) FROM pharmacy_profile WHERE mobile = @mobile";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@mobile", mobileNumber);
                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private string GenerateOtp()
        {
            Random rand = new Random();
            return rand.Next(100000, 999999).ToString();
        }

        private bool UpdatePassword(string mobileNumber, string newPassword)
        {
            string query = "UPDATE pharmacy_profile SET password = @newPassword WHERE mobile = @mobile";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@newPassword", newPassword);
                        command.Parameters.AddWithValue("@mobile", mobileNumber);
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}