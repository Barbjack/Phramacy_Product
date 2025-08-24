using Microsoft.Win32;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Phramacy_Product.Views.Profile
{
    public partial class LicenseInputControl : UserControl
    {
        private byte[] documentData;
        private string documentName;
        private string documentType;

        public static readonly DependencyProperty LicenseTitleProperty =
            DependencyProperty.Register("LicenseTitle", typeof(string), typeof(LicenseInputControl), new PropertyMetadata(string.Empty));

        public string LicenseTitle
        {
            get { return (string)GetValue(LicenseTitleProperty); }
            set { SetValue(LicenseTitleProperty, value); }
        }

        public static readonly DependencyProperty SaveButtonTextProperty =
            DependencyProperty.Register("SaveButtonText", typeof(string), typeof(LicenseInputControl), new PropertyMetadata("Save"));

        public string SaveButtonText
        {
            get { return (string)GetValue(SaveButtonTextProperty); }
            set { SetValue(SaveButtonTextProperty, value); }
        }

        public LicenseInputControl()
        {
            InitializeComponent();
        }
       
        private void FileDropBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Document Files|*.pdf;*.jpg;*.jpeg;*.png;*.gif|All Files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                documentName = Path.GetFileName(filePath);
                documentType = Path.GetExtension(filePath);
                documentType = documentType.Substring(1);
                documentData = File.ReadAllBytes(filePath);

                fileNameTextBlock.Text = documentName;
                fileNameTextBlock.Visibility = Visibility.Visible;
                plusSignTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(licenseNumberTextBox.Text))
            {
                MessageBox.Show("Please enter a license number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (documentData == null || documentData.Length == 0)
            {
                MessageBox.Show("Please upload a file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string licenseNumber = licenseNumberTextBox.Text;
            DateTime? expiryDate = expiryDatePicker.SelectedDate;
            string documentName = LicenseTitle;

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO [dbo].[pharmacy_documents] 
                                     ([user_id], [document_name], [document_type], [document_data], [doc_expiry],[created_at], [license_number], [isDeleted])
                                     VALUES 
                                     (@user_id, @document_name, @document_type, @document_data, @doc_expiry, @created_at,@license_number, 0)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@user_id", GlobalData.userId); 
                        command.Parameters.AddWithValue("@document_name", documentName);
                        command.Parameters.AddWithValue("@document_type", documentType);
                        command.Parameters.AddWithValue("@document_data", documentData);
                        command.Parameters.AddWithValue("@doc_expiry", expiryDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@created_at", DateTime.Now);
                        command.Parameters.AddWithValue("@license_number", licenseNumber);

                        command.ExecuteNonQuery();
                        Clear_Form();
                        MessageBox.Show("Document saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    private void Clear_Form()
        {
            licenseNumberTextBox.Text = string.Empty;
            expiryDatePicker.SelectedDate = null;
            fileNameTextBlock.Text = string.Empty;
            fileNameTextBlock.Visibility = Visibility.Collapsed;
            plusSignTextBlock.Visibility = Visibility.Visible;
            documentData = null;
            documentName = null;
            documentType = null;
        }
    }
}