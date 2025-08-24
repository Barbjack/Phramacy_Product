using Phramacy_Product.DataModel;
using Phramacy_Product.Views.Sales.GenerateSaleInvoice;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
//using System.Text.RegularExpressions;
namespace Phramacy_Product.Views.Sales
{
    public partial class NewSalePage : Page
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private readonly List<Medicine> medicineBilling = new List<Medicine>();
        readonly SalesDBManager saleDBManager = new SalesDBManager();
        private string selectedMember;

        
        public string SelectedMember
        {
            get { return selectedMember; }
            set
            {
                selectedMember = value;
                
            }
        }
        public NewSalePage()
        {
           
             InitializeComponent();
             this.selectedMember = GlobalData.LoggedInUser; 
             this.DataContext = this;

        }
        // Adding Drug and making Bill of it
        private void AddBill_CustomerDetails(object sender, RoutedEventArgs e)
        {
            string productName = SearchTextBox.Text;
            string quantityType = qtyType.Text;
            string qtyText = quantity.Text.Trim();

            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity greater than 0.", "Invalid Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (productName.Length < 3)
            {
                MessageBox.Show("Please enter at least 3 characters in Search Medicine.");
                return;
            }

            if (string.IsNullOrWhiteSpace(quantityType))
            {
                MessageBox.Show("Please select a Sale Type (QTY(F) or QTY(L)).");
                return;
            }

            string gst = formGSTOption.Text;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT id,Batch, price, Expiry, Discount, GST, pack_size_label, isnull(price, 0.0) as price, isnull(Quantity, 0) as Quantity
                    FROM Pharma_Medicines
                    WHERE name = @name";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandTimeout = 120;
                    cmd.Parameters.AddWithValue("@name", productName);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Medicine medicineObject = new Medicine
                            {
                                ProductName = productName,
                                BatchNumber = reader["Batch"].ToString(),
                                Expiry = Convert.ToDateTime(reader["Expiry"]),
                                StripInfo = reader["pack_size_label"].ToString(),
                                Discount = Convert.ToDecimal(reader["Discount"]),
                                MRP = Convert.ToDecimal(reader["price"]),
                                ItemId = (int)Convert.ToInt64(reader["id"])

                            };

                            if (gst == "With GST")
                            {
                                medicineObject.GST = Convert.ToDecimal(reader["GST"]);
                            }
                            else
                            {
                                medicineObject.GST = 0.0m;
                            }

                            if (quantityType == "QTY(F)")
                            {
                                decimal priceAfterDiscount = medicineObject.MRP - (medicineObject.MRP * medicineObject.Discount / 100);
                                decimal priceWithGST = priceAfterDiscount + (priceAfterDiscount * medicineObject.GST / 100);
                                medicineObject.QtyF = qty;
                                medicineObject.Total = qty * priceWithGST;
                            }
                            else
                            {
                                string input = reader["pack_size_label"].ToString();
                                decimal number = 0;
                                Match match = Regex.Match(input, @"\d+");
                                if (match.Success)
                                {
                                    number = Convert.ToDecimal(match.Value);
                                }
                                decimal drugPrice = medicineObject.MRP / number;
                                decimal drugPriceAfterDiscount = drugPrice - (drugPrice * medicineObject.Discount / 100);
                                decimal drugPriceWithGST = drugPriceAfterDiscount + (drugPriceAfterDiscount * medicineObject.GST / 100);
                                medicineObject.QtyL = qty;
                                medicineObject.Total = qty * drugPriceWithGST;
                            }
                            medicineBilling.Add(medicineObject);

                            decimal totalAmount = medicineBilling.Sum(m => m.Total);
                            Total_Amount.Text = "Total Amount: " + totalAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN"));
                        }
                        else
                        {
                            MessageBox.Show("Medicine not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
            }
            ProductGrid.ItemsSource = null;
            ProductGrid.ItemsSource = medicineBilling;
            formPaymentType.IsEnabled = true;
            SearchTextBox.Clear();
            qtyType.SelectedItem = null;
            quantity.Clear();
            formGSTOption.SelectedItem = null;
        }
        private void UpdateMedicineQuantity(SqlConnection conn, SqlTransaction transaction)
        {
            // Combine the updates into a single query to reduce database calls
            string updateQuery = @"
        UPDATE Pharma_Medicines
        SET
            Quantity = Quantity - @QtyToDeductFull,
            QtyInLoose = QtyInLoose - @QtyToDeductLoose
        WHERE name = @ItemName AND Batch = @BatchNumber";

            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction))
            {
                updateCmd.Parameters.Add("@QtyToDeductFull", System.Data.SqlDbType.Int);
                updateCmd.Parameters.Add("@QtyToDeductLoose", System.Data.SqlDbType.Int);
                updateCmd.Parameters.Add("@ItemName", System.Data.SqlDbType.NVarChar);
                updateCmd.Parameters.Add("@BatchNumber", System.Data.SqlDbType.NVarChar);

                foreach (var med in medicineBilling)
                {
                    updateCmd.Parameters["@QtyToDeductFull"].Value = med.QtyF;
                    updateCmd.Parameters["@QtyToDeductLoose"].Value = med.QtyL;
                    updateCmd.Parameters["@ItemName"].Value = med.ProductName;
                    updateCmd.Parameters["@BatchNumber"].Value = med.BatchNumber;

                    updateCmd.ExecuteNonQuery();
                }
            }
        }

        [Obsolete]
        private void AddTo_SaleItemDetailPharmaCustomer(Object sender, RoutedEventArgs e)
        {

            if (!ValidateAllFields())
            {
                MessageBox.Show("Please fill all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (medicineBilling == null || medicineBilling.Count == 0)
            {
                MessageBox.Show("Please add at least one medicine to the bill.", "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string inputNumber = SearchNumberBox.Text;
            decimal totalAmount = medicineBilling.Sum(m => m.Total);
            //decimal paidAmount = medicineBilling.Sum(m => m.PaidAmount);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                bool customerExists = saleDBManager.checkCustomerExist(inputNumber, conn);
                String mobile = SearchNumberBox.Text;
                String customerName = formCustomerName.Text;
                saleDBManager.updatePharmaCustomer(conn, customerName, mobile, totalAmount, totalPaidAmount, customerExists);
                string billNumber = saleDBManager.GenerateBillNumber(conn);
                String billPath = SaveButton_Click(sender, e);
                UpdateSaleItemDetails(conn, billNumber, billPath, totalAmount, totalPaidAmount);
            }

        }
        private bool ValidateAllFields()
        {
            bool isValid = true;

            // Direct check for required fields
            if (string.IsNullOrWhiteSpace(SearchNumberBox.Text)) isValid = false;
            if (string.IsNullOrWhiteSpace(formCustomerName.Text)) isValid = false;
            if (string.IsNullOrWhiteSpace(formPatientName.Text)) isValid = false;
            if (string.IsNullOrWhiteSpace(formDocName.Text)) isValid = false;
            if (!formBillDate.SelectedDate.HasValue) isValid = false;
            if (!formCreateAt.SelectedDate.HasValue) isValid = false;

            // Optional: Use your existing binding validation check for more advanced scenarios
            void ValidateBinding(DependencyObject obj, DependencyProperty property)
            {
                BindingExpression binding = BindingOperations.GetBindingExpression(obj, property);
                if (binding != null)
                {
                    binding.UpdateSource();
                    if (Validation.GetHasError(obj))
                        isValid = false;
                }
            }

            // You can keep the binding validation if your XAML supports it
            ValidateBinding(SearchNumberBox, TextBox.TextProperty);
            ValidateBinding(formCustomerName, TextBox.TextProperty);
            ValidateBinding(formPatientName, TextBox.TextProperty);
            ValidateBinding(formDocName, TextBox.TextProperty);
            ValidateBinding(formBillDate, DatePicker.SelectedDateProperty);
            ValidateBinding(formCreateAt, DatePicker.SelectedDateProperty);

            // Add a check for other required fields not covered by binding
            if (formCreatedBy.Text == null) isValid = false;
            if (formPaymentType.SelectedItem == null) isValid = false;

            return isValid;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                        yield return t;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }

        private void UpdateSaleItemDetails(SqlConnection conn, String billNumber, String billPath, decimal totalAmount, decimal paidAmount)
        {

            SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                // 1. Insert into SalesDetail
                string saleDetailsQuery = @"
                INSERT INTO SaleDetails 
               (CustomerName, DoctorName, BillNumber, BillDate,PaidAmount,TotalAmount, CreatedBy,BillPath, PaymentType,Status,PayAppName,TsNum, CreatedAt, PatientName) 
               OUTPUT INSERTED.SaleID 
               VALUES(@CustomerName, @DoctorName, @BillNumber, @BillDate, @PaidAmount,@TotalAmount, @CreatedBy,@BillPath, @PaymentType,@Status,@PayAppName,@TsNum, @CreatedAt, @PatientName)";
                SqlCommand saleCmd = new SqlCommand(saleDetailsQuery, conn, transaction);

                saleCmd.Parameters.AddWithValue("@CustomerName", formCustomerName.Text);
                saleCmd.Parameters.AddWithValue("@DoctorName", formDocName.Text);
                saleCmd.Parameters.AddWithValue("@BillNumber", billNumber);
                saleCmd.Parameters.AddWithValue("@BillPath", billPath);
                if (formBillDate.SelectedDate.HasValue)
                {
                    saleCmd.Parameters.AddWithValue("@BillDate", formBillDate.SelectedDate.Value);
                }
                else
                {
                    saleCmd.Parameters.AddWithValue("@BillDate", DBNull.Value);
                }
                string createdBy = formCreatedBy.Text;
                string paymentType = formPaymentType.SelectedItem is ComboBoxItem item2 ? item2.Content.ToString() : string.Empty;
                string gstOption = formGSTOption.SelectedItem is ComboBoxItem item3 ? item3.Content.ToString() : string.Empty;

                saleCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                saleCmd.Parameters.AddWithValue("@PaidAmount", paidAmount);
                saleCmd.Parameters.AddWithValue("@Status", paidAmount < totalAmount ? "Pending" : "Completed");
                saleCmd.Parameters.AddWithValue("@PaymentType", paymentType);
                // For PayAppName (Payment App)
                if (!string.IsNullOrWhiteSpace(dialogPaymentApp.Text))
                {
                    saleCmd.Parameters.AddWithValue("@PayAppName", dialogPaymentApp.Text.Trim());
                }
                else
                {
                    saleCmd.Parameters.AddWithValue("@PayAppName", DBNull.Value);
                }
                // For TsNum (Transaction Number / UTR No)
                if (!string.IsNullOrWhiteSpace(dialogTransactionNumber.Text))
                {
                    saleCmd.Parameters.AddWithValue("@TsNum", dialogTransactionNumber.Text.Trim());
                }
                else
                {
                    saleCmd.Parameters.AddWithValue("@TsNum", DBNull.Value);
                }
                saleCmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                saleCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                saleCmd.Parameters.AddWithValue("@PatientName", formPatientName.Text);
                // Get the newly inserted SaleID
                int saleID = (int)saleCmd.ExecuteScalar();

                // 2. Insert into SaleItem for each item
                string insertItemQuery = @"
                        INSERT INTO SaleItems (SaleID,ItemId,ItemName,Batch,Expiry,Pack,MRP,Quantity,Discount,GST,CreatedAt,Is_Loose,NetAmount)
                        VALUES (@SaleID,@ItemId,@ItemName,@Batch,@Expiry,@Pack,@MRP,@Quantity,@Discount,@GST,@CreatedAt,@Is_Loose,@NetAmount)";

                SqlCommand itemCmd = new SqlCommand(insertItemQuery, conn, transaction);
                foreach (var med in medicineBilling)
                {
                    itemCmd.Parameters.Clear();
                    itemCmd.Parameters.AddWithValue("@SaleID", saleID);
                    itemCmd.Parameters.AddWithValue("@ItemId", med.ItemId);
                    itemCmd.Parameters.AddWithValue("@ItemName", med.ProductName);
                    itemCmd.Parameters.AddWithValue("@Batch", med.BatchNumber);
                    itemCmd.Parameters.AddWithValue("@Expiry", med.Expiry);
                    itemCmd.Parameters.AddWithValue("@Pack", med.StripInfo);
                    itemCmd.Parameters.AddWithValue("@MRP", med.MRP);
                    itemCmd.Parameters.AddWithValue("@Quantity", med.QtyF + med.QtyL);
                    itemCmd.Parameters.AddWithValue("@Discount", med.Discount);
                    itemCmd.Parameters.AddWithValue("@GST", med.GST);
                    itemCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    itemCmd.Parameters.AddWithValue("@Is_Loose", med.QtyL > 0);
                    itemCmd.Parameters.AddWithValue("@NetAmount", med.Total);
                    itemCmd.ExecuteNonQuery();
                }
                UpdateMedicineQuantity(conn, transaction);

                transaction.Commit();
                MessageBox.Show("Sale successfully saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        //Customer Number List
        private async void SearchTextBox_NumberChanged(object sender, EventArgs e)
        {
            String input = SearchNumberBox.Text;
            if (input.Length < 1)
            {
                NumberPopup.IsOpen = false;
                return;
            }
            List<CustomerDetail> customerDetails = await Task.Run(() => GetCustomerDetails(input));
            if (customerDetails.Count > 0)
            {
                NumberList.ItemsSource = customerDetails;
                NumberPopup.IsOpen = true;
            }
            else
            {
                NumberPopup.IsOpen = false;
            }
        }

        private List<CustomerDetail> GetCustomerDetails(String input)
        {
            String query = @"select CustomerName,Mobile from PharmaCustomers where Mobile Like @search + '%'";
            List<CustomerDetail> newCustomerList = new List<CustomerDetail>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@search", input);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CustomerDetail newCustomer = new CustomerDetail
                            {
                                CustomerName = reader["CustomerName"].ToString(),
                                CustomerNumber = reader["Mobile"].ToString()
                            };
                            newCustomerList.Add(newCustomer);
                        }
                    }
                }
            }
            return newCustomerList;
        }
        private void NumberList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NumberList.SelectedItem is CustomerDetail selectedItem)
            {
                SearchNumberBox.Text = selectedItem.CustomerNumber;
                formCustomerName.Text = selectedItem.CustomerName;
                NumberPopup.IsOpen = false;
                NumberList.SelectedItem = null;
            }
        }
        //Medicine Search List Feature 
        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = SearchTextBox.Text;
            if (input.Length < 3)
            {
                SuggestionPopup.IsOpen = false;
                return;
            }

            List<Medicine> medicines = await Task.Run(() => GetMedicines(input));
            if (medicines.Count > 0)
            {
                SuggestionList.ItemsSource = medicines;
                SuggestionPopup.IsOpen = true;
            }
            else
            {
                SuggestionPopup.IsOpen = false;
            }
        }
        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuggestionList.SelectedItem is Medicine selected)
            {
                SearchTextBox.Text = selected.ProductName;
                SuggestionPopup.IsOpen = false;
                SuggestionList.SelectedItem = null;
            }
        }
        private List<Medicine> GetMedicines(string input)
        {
            var results = new List<Medicine>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"sp_getPharmaData '" + input + "'";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandTimeout = 120;
                    cmd.Parameters.AddWithValue("@search", input);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Medicine
                            {
                                ProductName = reader["name"].ToString(),
                                CompanyName = reader["manufacturer_name"].ToString(),
                                StripInfo = reader["pack_size_label"].ToString(),
                                MRP = Convert.ToDecimal(reader["price"]),
                                Stock = Convert.ToInt32(reader["Quantity"])
                            });
                        }
                    }
                }
            }
            return results;
        }

        //Choose cash or online payment mode
        private void FormPaymentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (formPaymentType.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedPaymentType = selectedItem.Content.ToString();

                dialogTitle.Text = selectedPaymentType == "Cash" ? "Offline Payment Details" : "Online Payment Details";
                dialogPaymentMode.Text = selectedPaymentType;
                OnlinePaymentFields.Visibility = selectedPaymentType == "Online"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                RootDialogHost.IsOpen = true;
            }
        }
        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (ProductGrid.SelectedItem != null)
            {
                Medicine selectedMedicine = ProductGrid.SelectedItem as Medicine;
                medicineBilling.Remove(selectedMedicine);
                decimal totalAmount = medicineBilling.Sum(m => m.Total);
                Total_Amount.Text = "Total Amount: " + totalAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN"));
                ProductGrid.ItemsSource = null;
                ProductGrid.ItemsSource = medicineBilling;

                if (medicineBilling.Count == 0)
                {
                    formPaymentType.IsEnabled = false;
                }

                MessageBox.Show("Item successfully removed from the bill.", "Item Removed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select an item to delete.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private decimal totalPaidAmount;
        private void DialogSubmit_Click(object sender, RoutedEventArgs e)
        {
            string mode = dialogPaymentMode.Text;
            string amountText = dialogPaidAmount.Text.Trim();

            // Remove the '₹' symbol before trying to parse
            if (amountText.StartsWith("₹"))
            {
                amountText = amountText.Substring(1);
            }
            if (!decimal.TryParse(amountText, out decimal amount))
            {
                MessageBox.Show("Enter a valid paid amount.");
                return;
            }

            if (mode == "Online")
            {
                string app = dialogPaymentApp.Text;
                string utr = dialogTransactionNumber.Text;

                if (string.IsNullOrWhiteSpace(app) || string.IsNullOrWhiteSpace(utr))
                {
                    MessageBox.Show("Please fill Payment App and UTR Number.");
                    return;
                }
            }

            totalPaidAmount = amount;
            ProductGrid.Items.Refresh();
            dialogPaidAmount.Text = string.Empty;
            RootDialogHost.IsOpen = false;
        }

        private void DialogCancel_Click(object sender, RoutedEventArgs e)
        {
            RootDialogHost.IsOpen = false;
        }
        private void ClearForm()
        {
            SearchNumberBox.Clear();
            formCustomerName.Clear();
            formDocName.Clear();
            formPatientName.Clear();
            formPaymentType.SelectedItem = null;
            formCreatedBy.Clear();
            formGSTOption.SelectedItem = null;
            formBillDate.SelectedDate = DateTime.Now;
            formCreateAt.SelectedDate = DateTime.Now;
            medicineBilling.Clear();
            ProductGrid.ItemsSource = null;
            Total_Amount.Text = "Total Amount: ";
        }

        [Obsolete]
        public String SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Populate the Sale object with actual data from your form fields
            var sale = new SalePdfInvoice
            {
                CustomerName = formCustomerName.Text,
                Mobile = SearchNumberBox.Text,
                BillNo = saleDBManager.GenerateBillNumber(new SqlConnection(connectionString)),
                Date = formBillDate.SelectedDate ?? DateTime.Now,

                PaymentType = formPaymentType.SelectedItem is ComboBoxItem item2 ? item2.Content.ToString() : "Cash",
            };

            // Pass both the Sale object and the list of billing items to the generator
            return PdfInvoiceGenerator.GenerateInvoice(sale, medicineBilling);
        }

    }
    public class RequiredFieldValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(false, "This field is required.");
            }
            return ValidationResult.ValidResult;
        }
    }
    public class PositiveNumberValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (decimal.TryParse(value?.ToString(), out decimal result) && result > 0)
                return ValidationResult.ValidResult;

            return new ValidationResult(false, "Amount must be a positive number.");
        }
    }
}
