using Microsoft.Win32;
using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Phramacy_Product.Views.Profile
{
    public partial class AboutContent : System.Windows.Controls.UserControl
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private byte[] companyLogoBytes;
        private byte[] signatureBytes;

        public AboutContent()
        {
            InitializeComponent();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate the text boxes with data from the GlobalData class
            PharmacyNameTextBox.Text = GlobalData.pharmacyName;
            EmailTextBox.Text = GlobalData.email;
            PharmacistNameTextBox.Text = GlobalData.LoggedInUser; // Assuming LoggedInUser is the pharmacist's name
            MobileTextBox.Text = GlobalData.mobile;
        }
        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profile = new PharmacyProfile
            {
                pharmacy_name = PharmacyNameTextBox.Text,
                pharmacist_name = PharmacistNameTextBox.Text,
                mobile = MobileTextBox.Text,
                email = EmailTextBox.Text,
                gstin = GstinTextBox.Text,
                panno = PannoTextBox.Text,
                dlno = DlnoTextBox.Text,
                address = AddressTextBox.Text,
                address2 = Address2TextBox.Text,
                area = AreaTextBox.Text,
                pincode = PincodeTextBox.Text,
                city = CityTextBox.Text,
                state = StateTextBox.Text,
                billPath = BillPathTextBox.Text,
                company_logo = companyLogoBytes,
                signature = signatureBytes,
                created_at = DateTime.Now,
                updated_at = DateTime.Now,
                profileId = GlobalData.userId
  
            };

            try
            {
                SaveProfileToDatabase(profile);
                Clear_Form();
                System.Windows.MessageBox.Show("Profile saved successfully!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving profile: {ex.Message}");
            }
        }
        private void SaveProfileToDatabase(PharmacyProfile profile)
        {
            string query = @"
        UPDATE [dbo].[pharmacy_profile]
        SET
            pharmacy_name = @PharmacyName,
            pharmacist_name = @PharmacistName,
            mobile = @Mobile,
            email = @Email,
            address = @Address,
            address2 = @Address2,
            area = @Area,
            pincode = @Pincode,
            city = @City,
            state = @State,
            billPath = @BillPath,
            company_logo = @CompanyLogo,
            signature = @Signature,
            updated_at = GETDATE()
        WHERE
            id = @ProfileId"; 
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PharmacyName", (object)profile.pharmacy_name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PharmacistName", (object)profile.pharmacist_name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Mobile", (object)profile.mobile ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Email", (object)profile.email ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Address", (object)profile.address ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Address2", (object)profile.address2 ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Area", (object)profile.area ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Pincode", (object)profile.pincode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@City", (object)profile.city ?? DBNull.Value);
                    command.Parameters.AddWithValue("@State", (object)profile.state ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BillPath", (object)profile.billPath ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CompanyLogo", (object)profile.company_logo ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Signature", (object)profile.signature ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProfileId", profile.profileId);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Profile updated successfully!");
                    }
                    else
                    {
                         MessageBox.Show("No record found with the specified ID.");
                    }
                }
            }
        }
        private void Clear_Form()
        { 
            PharmacyNameTextBox.Clear();
            PharmacistNameTextBox.Clear();
            MobileTextBox.Clear();
            EmailTextBox.Clear();
            AddressTextBox.Clear();
            Address2TextBox.Clear();
            AreaTextBox.Clear();
            PincodeTextBox.Clear();
            CityTextBox.Clear();
            StateTextBox.Clear();
            BillPathTextBox.Clear();
            companyLogoBytes = null;
            signatureBytes = null;

            CompanyLogoImage.Source = null;
            CompanyLogoImage.Visibility = Visibility.Collapsed;
            CompanyLogoPrompt.Visibility = Visibility.Visible;

            SignatureImage.Source = null;
            SignatureImage.Visibility = Visibility.Collapsed;
            SignaturePrompt.Visibility = Visibility.Visible;
        }

        private void HandleImageFile(string filePath, Image imageControl, TextBlock promptControl, ref byte[] bytesArray)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                bytesArray = File.ReadAllBytes(filePath);
                BitmapImage bitmap = new BitmapImage();
                using (MemoryStream stream = new MemoryStream(bytesArray))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }

                imageControl.Source = bitmap;
                imageControl.Visibility = Visibility.Visible;
                promptControl.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading image: {ex.Message}");
                imageControl.Visibility = Visibility.Collapsed;
                promptControl.Visibility = Visibility.Visible;
            }
        }

        private void CompanyLogoBorder_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    HandleImageFile(files[0], CompanyLogoImage, CompanyLogoPrompt, ref companyLogoBytes);
                }
            }
        }

        private void CompanyLogoBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                HandleImageFile(openFileDialog.FileName, CompanyLogoImage, CompanyLogoPrompt, ref companyLogoBytes);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void StringValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z\\s]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("[^a-zA-Z\\s]+");

                if (regex.IsMatch(pastedText))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
        private void SignatureBorder_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    HandleImageFile(files[0], SignatureImage, SignaturePrompt, ref signatureBytes);
                }
            }
        }

        private void SignatureBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                HandleImageFile(openFileDialog.FileName, SignatureImage, SignaturePrompt, ref signatureBytes);
            }
        }
    }
}