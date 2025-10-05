using Microsoft.Win32;
using Phramacy_Product.DataModel;
using Phramacy_Product.Views.DBMaster;
using Phramacy_Product.Views.Sales;
using Phramacy_Product.Views.Sales.GenerateSaleInvoice;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
namespace Phramacy_Product.Views.Purchase.PurchaseGenerate
{
    public partial class NewPurchaseGenerate : Page
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        private readonly ObservableCollection<PurchaseMedicine> medicineBilling = new ObservableCollection<PurchaseMedicine>();
        readonly SalesDBManager saleDBManager = new SalesDBManager();
        readonly PurchaseDBManager purchaseDBManager = new PurchaseDBManager();
        private string selectedMember;
        private string currentPath;
        public string SelectedMember
        {
            get { return selectedMember; }
            set
            {
                selectedMember = value;

            }
        }

        private DateTime currentDate;
        public DateTime CurrentDate
        {
            get { return currentDate; }
            set
            {
                currentDate = value;
            }
        }
        public NewPurchaseGenerate()
        {
            InitializeComponent();
            this.selectedMember = GlobalData.LoggedInUser;
            this.DataContext = this;
            CurrentDate = DateTime.Now;
            // pdfViewerControl = this.FindName("pdfViewControl") as MoonPdfLib.MoonPdfPanel;
            ProductGrid.ItemsSource = medicineBilling;
            medicineBilling.CollectionChanged += OnMedicineBillingChanged;
        }

        private void OnMedicineBillingChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (PurchaseMedicine item in e.NewItems)
                {
                    item.PropertyChanged += OnMedicinePropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (PurchaseMedicine item in e.OldItems)
                {
                    item.PropertyChanged -= OnMedicinePropertyChanged;
                }
            }

        }
        private void OnMedicinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                decimal totalAmount = medicineBilling.Sum(m => m.Total);
                Total_Amount.Text = "Grand Total: " + totalAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN"));
                dialogPaidAmount.Text = totalAmount.ToString("F2");
            }), System.Windows.Threading.DispatcherPriority.Background); 
        }
        private void DownloadSample_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog
            {
                FileName = "PurchaseSample.csv",
                DefaultExt = ".csv",
                Filter = "CSV files (*.csv)|*.csv"
            };

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    string resourcePath = "Phramacy_Product.Resources.PurchaseSample.csv";
                    Assembly assembly = Assembly.GetExecutingAssembly();

                    using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                    {
                        if (stream == null)
                        {
                            MessageBox.Show("Error: Sample file resource not found. Check the file's 'Build Action'.", "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        using (FileStream fileStream = File.Create(saveFileDialog.FileName))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }

                    MessageBox.Show($"Sample file saved successfully to:\n{saveFileDialog.FileName}", "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred during download: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ImportFile_Click(object sender, RoutedEventArgs e)
        {
            
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Select Purchase File to Import"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                {
                    try
                    {
                        medicineBilling.Clear();
                        Total_Amount.Text = "Grand Amount: ";
                        List<PurchaseMedicine> importedItems = ReadAndMapCsvFile(openFileDialog.FileName);

                        if (importedItems.Any())
                        {
                            var currentItems = ProductGrid.ItemsSource as ObservableCollection<PurchaseMedicine>;
                            if (currentItems != null)
                            {
                                foreach (var item in importedItems)
                                {
                                    
                                    item.RecalculateTotal();
                                    currentItems.Add(item);
                                }
                                decimal totalAmount = medicineBilling.Sum(m => m.Total);
                                Total_Amount.Text = "Grand Total: " + totalAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN"));
                                dialogPaidAmount.Text = totalAmount.ToString("F2");
                                formPaymentType.IsEnabled = true;
                            }
                            else
                            {
                                // Initialize new collection if it's null
                                ProductGrid.ItemsSource = new ObservableCollection<PurchaseMedicine>(importedItems.Select(item => { item.RecalculateTotal(); return item; }).ToList());
                            }

                            MessageBox.Show($"Successfully imported {importedItems.Count} items.", "Import Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("No valid purchase items were found in the file or the file is empty/invalid.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred during import: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private List<PurchaseMedicine> ReadAndMapCsvFile(string filePath)
        {
            var purchaseItems = new List<PurchaseMedicine>();
            var targetColumns = new Dictionary<string, Type>
            {
                { "ProductName", typeof(string) },
                { "StripInfo", typeof(string) },
                { "BatchNumber", typeof(string) },

                { "Expiry", typeof(DateTime) },
                { "QtyF", typeof(int) },
                { "QtyL", typeof(int) },
                { "MRP", typeof(decimal) },
                { "PTR", typeof(decimal) },
                { "SchAmt", typeof(decimal) },
                { "BaseAmt", typeof(decimal) },
                { "Discount", typeof(decimal) },
                { "GST", typeof(decimal) },
                { "Total", typeof(decimal) }
            };

            var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Item Name", "ProductName" },
                { "Unit/Pack", "StripInfo" },
                { "BATCH NUMBER", "BatchNumber" },
                { "EXPIRY", "Expiry" },
                { "QTY(F)", "QtyF" },
                { "Free", "QtyL" },
                { "M.R.P", "MRP" },
                { "P.T.R", "PTR" },
                { "Sch. Amt", "SchAmt" },
                { "Base", "BaseAmt" },
                { "DISC%", "Discount" },
                { "GST%", "GST" },
                { "Net Amount", "Total" }
            };


            using (var reader = new StreamReader(filePath))
            {
                if (reader.EndOfStream)
                    throw new InvalidDataException("The selected file is empty.");

                string headerLine = reader.ReadLine();
                var fileHeaders = headerLine.Split(',').Select(h => h.Trim()).ToList();
                var columnIndexMap = new Dictionary<string, int>();
                foreach (var header in fileHeaders)
                {
                    string cleanHeader = header.Replace(" ", "").Replace(".", "").Replace("%", "");

                    var matchingEntry = headerMap.FirstOrDefault(hm =>
                        hm.Key.Replace(" ", "").Replace(".", "").Replace("%", "").Equals(cleanHeader, StringComparison.OrdinalIgnoreCase));

                    if (!matchingEntry.Equals(default(KeyValuePair<string, string>)))
                    {
                        string modelPropertyName = matchingEntry.Value;
                        int index = fileHeaders.IndexOf(header);
                        if (targetColumns.ContainsKey(modelPropertyName) && !columnIndexMap.ContainsKey(modelPropertyName))
                        {
                            columnIndexMap.Add(modelPropertyName, index);
                        }
                    }
                }


                if (!columnIndexMap.ContainsKey("ProductName"))
                {

                    throw new InvalidDataException("The file must contain a valid column that maps to 'ProductName' (e.g., 'Item Name').");
                }

                int rowNumber = 1;
                while (!reader.EndOfStream)
                {
                    rowNumber++;
                    string dataLine = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(dataLine)) continue;

                    var fields = dataLine.Split(',').Select(f => f.Trim()).ToList();
                    if (fields.Count < columnIndexMap.Values.DefaultIfEmpty(0).Max() + 1)
                    {
                        MessageBox.Show($"Row {rowNumber} is malformed and was skipped.", "Data Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    var item = new PurchaseMedicine();
                    bool rowSkipped = false;
                    foreach (var map in columnIndexMap)
                    {
                        string propertyName = map.Key;
                        int columnIndex = map.Value;
                        string value = fields[columnIndex];

                        if (string.IsNullOrWhiteSpace(value)) continue;

                        try
                        {
                            Type propertyType = targetColumns[propertyName];
                            var propertyInfo = typeof(PurchaseMedicine).GetProperty(propertyName);

                            if (propertyType == typeof(string))
                            {
                                propertyInfo.SetValue(item, value);
                            }

                            else if (propertyType == typeof(int))
                            {
                                if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue))
                                {
                                    propertyInfo.SetValue(item, intValue);
                                }
                                else
                                {
                                    MessageBox.Show($"Row {rowNumber}: Invalid whole number format for '{propertyName}'. Value skipped.", "Data Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }
                            else if (propertyType == typeof(DateTime))
                            {
                                if (DateTime.TryParseExact(value, new[] { "MMM-yy", "MMMM-yy", "MM/yy", "MM/yyyy", "dd-MM-yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue))
                                  {
                                    propertyInfo.SetValue(item, dateValue);
                                }
                                else
                                {
                                    MessageBox.Show($"Row {rowNumber}: Invalid date format for '{propertyName}'. Expected MM/yy or MM/yyyy. Value skipped.", "Data Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }
                            else if (propertyType == typeof(decimal))
                            {
                                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalValue))
                                {
                                    propertyInfo.SetValue(item, decimalValue);
                                }
                                else
                                {
                                    MessageBox.Show($"Row {rowNumber}: Invalid currency/decimal format for '{propertyName}'. Value skipped.", "Data Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Row {rowNumber}: Failed to process value for '{propertyName}'. Error: {ex.Message}. Row skipped.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            rowSkipped = true;
                            break;
                        }
                    }

                    if (!rowSkipped)
                    {
                        if (!string.IsNullOrWhiteSpace(item.ProductName))
                        {
                            purchaseItems.Add(item);
                        }
                    }
                }
            }
            return purchaseItems;
        }
        private void AddBill_CustomerDetails(object sender, RoutedEventArgs e)
        {
            string productName = SearchTextBox.Text.Trim();
            string quantityType = qtyType.Text;
            string qtyText = quantity.Text.Trim();
            if (!int.TryParse(qtyText, out int qty) || qty <= 0)
            {
                Console.WriteLine("Please enter a valid quantity greater than 0.");
                return;
            }

            if (productName.Length < 3)
            {
                Console.WriteLine("Please enter at least 3 characters in Search Medicine.");
                return;
            }

            if (string.IsNullOrWhiteSpace(quantityType))
            {
                Console.WriteLine("Please select a Sale Type (QTY(F) or QTY(L)).");
                return;
            }

            List<Medicine> medicineList = new DBMasterConnection().GetMedicines(productName);
            Medicine foundMedicine = medicineList.FirstOrDefault(m => m.ProductName == productName);

            if (foundMedicine != null)
            {
                
                string gst = formGSTOption.Text;
                PurchaseMedicine existingMedicine = medicineBilling.FirstOrDefault(m => m.ProductName == productName);
                if (existingMedicine != null)
                {
                    existingMedicine.GST = (gst == "With GST") ? foundMedicine.GST : 0.0m;

                    if (quantityType == "QTY(F)")
                    {
                        existingMedicine.QtyF += qty;
                        decimal priceAfterDiscount = existingMedicine.PTR - (existingMedicine.PTR * existingMedicine.Discount / 100);
                        existingMedicine.baseAmt = priceAfterDiscount;
                        decimal priceWithGST = priceAfterDiscount + (priceAfterDiscount * existingMedicine.GST / 100);
                        
                        existingMedicine.qtFTotal = existingMedicine.QtyF * priceWithGST;
                        existingMedicine.Total = existingMedicine.qtLTotal + existingMedicine.qtFTotal;
                    }
                    else
                    {
                        string input = existingMedicine.StripInfo;
                        decimal number = 0;
                        Match match = Regex.Match(input, @"\d+");
                        if (match.Success)
                        {
                            number = Convert.ToDecimal(match.Value);
                        }
                        decimal drugPrice = existingMedicine.PTR / number;
                        decimal drugPriceAfterDiscount = drugPrice - (drugPrice * existingMedicine.Discount / 100);
                        
                        decimal drugPriceWithGST = drugPriceAfterDiscount + (drugPriceAfterDiscount * existingMedicine.GST / 100);
                        existingMedicine.QtyL += qty;
                        existingMedicine.SchAmt = existingMedicine.QtyL * drugPriceAfterDiscount;
                        existingMedicine.qtLTotal = existingMedicine.QtyL * drugPriceWithGST;
                        existingMedicine.Total = existingMedicine.qtFTotal + existingMedicine.qtLTotal;
                    }
                }
                else
                {
                    PurchaseMedicine medicineObject = new PurchaseMedicine
                    {
                        ProductName = foundMedicine.ProductName,
                        BatchNumber = foundMedicine.BatchNumber,
                        Expiry = foundMedicine.Expiry,
                        StripInfo = foundMedicine.StripInfo,
                        Discount = foundMedicine.Discount,
                        MRP = foundMedicine.MRP,
                        PTR = foundMedicine.PTR,
                        ItemId = foundMedicine.ItemId,
                        CompanyName = foundMedicine.CompanyName,
                        medicineType = foundMedicine.medicineType,
                        saltComposition1 = foundMedicine.saltComposition1,
                        saltComposition2 = foundMedicine.saltComposition2
                    };

                    medicineObject.GST = (gst == "With GST") ? foundMedicine.GST : 0.0m;

                    if (quantityType == "QTY(F)")
                    {
                        decimal priceAfterDiscount = medicineObject.PTR - (medicineObject.PTR * medicineObject.Discount / 100);
                        decimal priceWithGST = priceAfterDiscount + (priceAfterDiscount * medicineObject.GST / 100);
                        medicineObject.QtyF = qty;
                        medicineObject.baseAmt = priceAfterDiscount;
                        medicineObject.qtFTotal = qty * priceWithGST;
                        medicineObject.Total = qty * priceWithGST;
                    }
                    else
                    {
                        medicineObject.IsLoose = true;
                        string input = medicineObject.StripInfo;
                        decimal number = 0;
                        Match match = Regex.Match(input, @"\d+");
                        if (match.Success)
                        {
                            number = Convert.ToDecimal(match.Value);
                        }
                        
                        decimal drugPrice = medicineObject.PTR / number;
                        decimal drugPriceAfterDiscount = drugPrice - (drugPrice * medicineObject.Discount / 100);
                        decimal drugPriceWithGST = drugPriceAfterDiscount + (drugPriceAfterDiscount * medicineObject.GST / 100);
                        
                        medicineObject.SchAmt = qty * drugPriceAfterDiscount;
                        medicineObject.QtyL = qty;
                        medicineObject.qtLTotal = qty * drugPriceWithGST;
                        medicineObject.Total = qty * drugPriceWithGST;
                    }
                    medicineBilling.Add(medicineObject);
                }
                decimal totalAmount = medicineBilling.Sum(m => m.Total);
                Total_Amount.Text = "Grand Total: " + totalAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN"));
                dialogPaidAmount.Text = totalAmount.ToString("F2");
            }
            else
            {
                Console.WriteLine("Medicine not found in the list.");
            }

            ProductGrid.ItemsSource = null;
            ProductGrid.ItemsSource = medicineBilling;
            formPaymentType.IsEnabled = true;
            SearchTextBox.Clear();
            qtyType.SelectedItem = null;
            quantity.Clear();
        }
        private async void SearchTextBox_NumberChanged(object sender, EventArgs e)
        {
            String input = SearchNumberBox.Text;
            if (input.Length < 1)
            {
                NumberPopup.IsOpen = false;
                return;
            }
            List<DistributorDetail> distributorDetails = await Task.Run(() => purchaseDBManager.GetDistributorDetails(connectionString, input));
            if (distributorDetails.Count > 0)
            {
                NumberList.ItemsSource = distributorDetails;
                NumberPopup.IsOpen = true;
            }
            else
            {
                NumberPopup.IsOpen = false;
                formDistributorName.Clear();
                PreviousPurchasesItemsControl.ItemsSource = null;
                PreviousPurchasesPanel.Visibility = Visibility.Collapsed;
                BillCountComboBox.SelectedItem = null;
            }
        }
       
        private void NumberList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NumberList.SelectedItem is DistributorDetail selectedItem)
            {
                SearchNumberBox.Text = selectedItem.DistributorNumber;
                formDistributorName.Text = selectedItem.DistributorName;
                NumberPopup.IsOpen = false;
                NumberList.SelectedItem = null;
                if (BillCountComboBox.SelectedItem is ComboBoxItem selectedItemBox)
                {
                    int billsToLoad = Convert.ToInt32(selectedItemBox.Tag);

                    if (!string.IsNullOrWhiteSpace(selectedItem.DistributorName))
                    {
                        LoadPreviousPurchases(selectedItem.DistributorName, billsToLoad);
                    }
                }
                else
                {
                    LoadPreviousPurchases(selectedItem.DistributorName, 5);
                }
            }
        }
        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = SearchTextBox.Text;
            if (input.Length < 3)
            {
                SuggestionPopup.IsOpen = false;
                return;
            }

            List<Medicine> medicines = await Task.Run(() => new DBMasterConnection().GetMedicines(input));
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
        private void UpdateMedicineQuantity(SqlConnection conn, SqlTransaction transaction)
        {
            string updateQuery = @"
        UPDATE Pharma_Medicines
        SET
            Quantity = Quantity + @QtyToAddFull,
            QtyInLoose = QtyInLoose + @QtyToAddLoose
        WHERE name = @ItemName AND Batch = @BatchNumber";

            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction))
            {
                updateCmd.Parameters.Add("@QtyToAddFull", System.Data.SqlDbType.Int);
                updateCmd.Parameters.Add("@QtyToAddLoose", System.Data.SqlDbType.Int);
                updateCmd.Parameters.Add("@ItemName", System.Data.SqlDbType.NVarChar);
                updateCmd.Parameters.Add("@BatchNumber", System.Data.SqlDbType.NVarChar);

                foreach (var med in medicineBilling)
                {
                    updateCmd.Parameters["@QtyToAddFull"].Value = med.QtyF;
                    updateCmd.Parameters["@QtyToAddLoose"].Value = med.QtyL;
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
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                String mobile = SearchNumberBox.Text;
                String distributorName = formDistributorName.Text;
                //purchaseDBManager.updatePharmaCustomer(customerName, mobile, totalAmount, totalPaidAmount, customerExists);
                string billNumber = new SalesDBManager().GenerateBillNumber();
                UpdatePurchaseItemDetails(conn, billNumber, totalAmount, totalPaidAmount);
                //MessageBox.Show("Purchase Detail Saved", MessageBoxButton.OK, MessageBoxImage.Information);

            }
        }
        private bool ValidateAllFields()
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(SearchNumberBox.Text)) isValid = false;
            if (string.IsNullOrWhiteSpace(formDistributorName.Text)) isValid = false;
           
            if (!formBillDate.SelectedDate.HasValue) isValid = false;
            if (!formCreateAt.SelectedDate.HasValue) isValid = false;
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
            ValidateBinding(SearchNumberBox, TextBox.TextProperty);
            ValidateBinding(formDistributorName, TextBox.TextProperty);
            ValidateBinding(formBillDate, DatePicker.SelectedDateProperty);
            ValidateBinding(formCreateAt, DatePicker.SelectedDateProperty);

            if (formCreatedBy.Text == null) isValid = false;
            if (formPaymentType.SelectedItem == null)
            {
                MessageBox.Show("Please fill the bill amount!");
                return false;
            }
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
        private void UpdatePurchaseItemDetails(SqlConnection conn, String billNumber, decimal totalAmount, decimal paidAmount)
        {
            decimal pendingAmount = totalAmount - paidAmount;
            string paymentStatus = paidAmount < totalAmount ? "Pending" : "Completed";

            conn.Open();
            SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                string purchaseDetailsQuery = @"
        INSERT INTO PurchaseDetails 
       (DistributorName, BillNumber, BillDate, PaidAmount, PendingAmount, TotalAmount, CreatedBy, PaymentType, PaymentStatus, PayName, TsNum, CreatedAt) 
       OUTPUT INSERTED.PurchaseID 
       VALUES(@DistributorName, @BillNumber, @BillDate, @PaidAmount, @PendingAmount, @TotalAmount, @CreatedBy, @PaymentType, @PaymentStatus, @PayName, @TsNum, @CreatedAt)";

                SqlCommand purchaseCmd = new SqlCommand(purchaseDetailsQuery, conn, transaction);

                purchaseCmd.Parameters.AddWithValue("@DistributorName", formDistributorName.Text); // Maps to CustomerName/DistributorName
                purchaseCmd.Parameters.AddWithValue("@BillNumber", billNumber);
                if (formBillDate.SelectedDate.HasValue)
                {
                    purchaseCmd.Parameters.AddWithValue("@BillDate", formBillDate.SelectedDate.Value);
                }
                else
                {
                    purchaseCmd.Parameters.AddWithValue("@BillDate", DBNull.Value);
                }

                string createdBy = formCreatedBy.Text;
                string paymentType = formPaymentType.SelectedItem is ComboBoxItem item2 ? item2.Content.ToString() : string.Empty;

                purchaseCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                purchaseCmd.Parameters.AddWithValue("@PaidAmount", paidAmount);
                purchaseCmd.Parameters.AddWithValue("@PendingAmount", pendingAmount); 
                purchaseCmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus); 
                purchaseCmd.Parameters.AddWithValue("@PaymentType", paymentType);

                if (!string.IsNullOrWhiteSpace(dialogPaymentApp.Text))
                {
                    purchaseCmd.Parameters.AddWithValue("@PayName", dialogPaymentApp.Text.Trim()); 
                }
                else
                {
                    purchaseCmd.Parameters.AddWithValue("@PayName", DBNull.Value);
                }

                if (!string.IsNullOrWhiteSpace(dialogTransactionNumber.Text))
                {
                    purchaseCmd.Parameters.AddWithValue("@TsNum", dialogTransactionNumber.Text.Trim());
                }
                else
                {
                    purchaseCmd.Parameters.AddWithValue("@TsNum", DBNull.Value);
                }

                purchaseCmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                purchaseCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                int purchaseID = (int)purchaseCmd.ExecuteScalar();
                string insertItemQuery = @"
                INSERT INTO PurchaseItems (PurchaseID, MedId, ItemName, Batch, Expiry, Pack, MRP, PTR, Quantity, Free, SchAmt, Discount, GST, Base, NetAmount, CreatedAt, Is_Loose)
                VALUES (@PurchaseID, @MedId, @ItemName, @Batch, @Expiry, @Pack, @MRP, @PTR, @Quantity, @Free, @SchAmt, @Discount, @GST, @Base, @NetAmount, @CreatedAt, @Is_Loose)";

                SqlCommand itemCmd = new SqlCommand(insertItemQuery, conn, transaction);
                foreach (var med in medicineBilling)
                {
                    itemCmd.Parameters.Clear();

                    itemCmd.Parameters.AddWithValue("@PurchaseID", purchaseID); 
                    itemCmd.Parameters.AddWithValue("@MedId", med.ItemId);      
                    itemCmd.Parameters.AddWithValue("@ItemName", med.ProductName);
                    itemCmd.Parameters.AddWithValue("@Batch", med.BatchNumber);
                    //itemCmd.Parameters.AddWithValue("@Expiry", med.Expiry);
                    if (med.Expiry == DateTime.MinValue)
                    {
                        itemCmd.Parameters.AddWithValue("@Expiry", DBNull.Value);
                    }
                    else
                    {
                        itemCmd.Parameters.AddWithValue("@Expiry", med.Expiry);
                    }
                    itemCmd.Parameters.AddWithValue("@Pack", med.StripInfo);    
                    itemCmd.Parameters.AddWithValue("@MRP", med.MRP);
                    itemCmd.Parameters.AddWithValue("@PTR", med.PTR);           
                    itemCmd.Parameters.AddWithValue("@Quantity", med.QtyF);
                    itemCmd.Parameters.AddWithValue("@Free", med.QtyL);         
                    itemCmd.Parameters.AddWithValue("@SchAmt", med.SchAmt);     
                    itemCmd.Parameters.AddWithValue("@Discount", med.Discount);
                    itemCmd.Parameters.AddWithValue("@GST", med.GST);
                    itemCmd.Parameters.AddWithValue("@Base", med.BaseAmt);      
                    itemCmd.Parameters.AddWithValue("@NetAmount", med.Total);
                    itemCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    itemCmd.Parameters.AddWithValue("@Is_Loose", med.QtyL > 0);

                    itemCmd.ExecuteNonQuery();
                }
                UpdateMedicineQuantity(conn, transaction);
                transaction.Commit();
                conn.Close();
                ClearForm();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"Database Error during Purchase Save: {ex.Message}");
            }
        }
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
                PurchaseMedicine selectedMedicine = ProductGrid.SelectedItem as PurchaseMedicine;
                medicineBilling.Remove(selectedMedicine);
                decimal totalAmount = medicineBilling.Sum(m => m.Total);
                Total_Amount.Text = "Grand Amount: " + totalAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN"));
                dialogPaidAmount.Text = totalAmount.ToString("F2");
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
        private void LoadPreviousPurchases(string distributorName, int billsToLoad)
        {
            if (string.IsNullOrWhiteSpace(distributorName))
            {
                PreviousPurchasesItemsControl.ItemsSource = null;
                PreviousPurchasesPanel.Visibility = Visibility.Collapsed;
                return;
            }

            var previousPurchases = new List<PurchaseMedicine>();
            try
            {   
                string safeDistributorName = distributorName.Replace("'", "''");
                string query = $@"
    SELECT pi.MedId, pi.ItemName, pi.Batch, pi.Expiry, pi.Pack, pi.MRP, pi.PTR, pi.Quantity, pi.Free,pi.Base,
    pi.SchAmt,pi.Discount, pi.GST, pi.NetAmount, pi.Is_Loose,pd.BillNumber, pd.BillDate, 
    pd.DistributorName,mi.manufacturer_name, mi.type, mi.short_composition1, mi.short_composition2
FROM PurchaseDetails pd
JOIN PurchaseItems pi ON pd.PurchaseID = pi.PurchaseID
JOIN Pharma_Medicines mi ON pi.MedId = mi.id
WHERE pd.DistributorName LIKE '{safeDistributorName}%'
AND pd.IsDeleted = 0 
ORDER BY pd.BillDate DESC, pd.BillNumber DESC";

                DataTable dt = DBMasterConnection.GD(query);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow reader in dt.Rows)
                    {
                        PurchaseMedicine medicine = new PurchaseMedicine
                        {
                            ItemId = reader["MedId"] != DBNull.Value ? (int)Convert.ToInt32(reader["MedId"]) : 0,
                            ProductName = reader["ItemName"]?.ToString(),
                            BatchNumber = reader["Batch"]?.ToString(),
                            expiryMedicine = reader["Expiry"]?.ToString(),
                            StripInfo = reader["Pack"]?.ToString(),

                            MRP = reader["MRP"] != DBNull.Value ? Convert.ToDecimal(reader["MRP"]) : 0m,
                            PTR = reader["PTR"] != DBNull.Value ? Convert.ToDecimal(reader["PTR"]) : 0m, 
                            Discount = reader["Discount"] != DBNull.Value ? Convert.ToDecimal(reader["Discount"]) : 0m,
                            GST = reader["GST"] != DBNull.Value ? Convert.ToDecimal(reader["GST"]) : 0m,
                            Total = reader["NetAmount"] != DBNull.Value ? Convert.ToDecimal(reader["NetAmount"]) : 0m,
                            SchAmt = reader["SchAmt"] != DBNull.Value ? Convert.ToDecimal(reader["SchAmt"]) : 0m,
                            baseAmt = reader["Base"] != DBNull.Value ? Convert.ToDecimal(reader["Base"]) : 0m,
                            QtyF = reader["Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Quantity"]) : 0,
                            QtyL = reader["Free"] != DBNull.Value ? Convert.ToInt32(reader["Free"]) : 0,
                            IsLoose = Convert.ToBoolean(reader["Is_Loose"]),
                            BillNumber = reader["BillNumber"]?.ToString(),
                            BillDate = reader["BillDate"] as DateTime?,
                            DistributorName = reader["DistributorName"]?.ToString(),

                            // Medicine Info from Pharma_Medicines
                            CompanyName = reader["manufacturer_name"]?.ToString(),
                            medicineType = reader["type"]?.ToString(),
                            saltComposition1 = reader["short_composition1"]?.ToString(),
                            saltComposition2 = reader["short_composition2"]?.ToString()
                        };
                        previousPurchases.Add(medicine);
                    }
                }
                else
                {
                    MessageBox.Show("No previous purchases found for this distributor.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    PreviousPurchasesItemsControl.ItemsSource = null;
                    PreviousPurchasesPanel.Visibility = Visibility.Collapsed;
                    BillCountComboBox.SelectedItem = null;
                    return;
                }
                var groupedPurchases = previousPurchases
                    .GroupBy(p => p.BillNumber)
                    .Select(g => new PreviousBill
                    {
                        BillNumber = g.Key,
                        BillDate = g.FirstOrDefault()?.BillDate,
                        Items = new ObservableCollection<PurchaseMedicine>(g.ToList())
                    })
                    .OrderByDescending(b => b.BillDate)
                    .ToList();

                if (billsToLoad > 0)
                {
                    groupedPurchases = groupedPurchases.Take(billsToLoad).ToList();
                }

                PreviousPurchasesItemsControl.ItemsSource = groupedPurchases;
                PreviousPurchasesPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading previous purchases: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                PreviousPurchasesItemsControl.ItemsSource = null;
                PreviousPurchasesPanel.Visibility = Visibility.Collapsed;
                BillCountComboBox.SelectedItem = null;
            }
        }
        private void BillCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BillCountComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                int billsToLoad = Convert.ToInt32(selectedItem.Tag);

                string distributorName = formDistributorName.Text;

                if (!string.IsNullOrWhiteSpace(distributorName))
                {
                    LoadPreviousPurchases(distributorName, billsToLoad);
                }
            }
        }
        private void AddAllPreviousBillItems_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var selectedBill = button.DataContext as PreviousBill;
                if (selectedBill != null)
                {
                    foreach (var selectedItem in selectedBill.Items)
                    {
                        decimal basePrice = selectedItem.PTR;
                        PurchaseMedicine existingMedicine = medicineBilling.FirstOrDefault(m => m.ProductName.Trim().Equals(selectedItem.ProductName.Trim(), StringComparison.OrdinalIgnoreCase));
                        
                        if (existingMedicine != null)
                        {
                            decimal priceAfterDiscount = basePrice - (basePrice * selectedItem.Discount / 100);
                            decimal priceWithGST = priceAfterDiscount + (priceAfterDiscount * selectedItem.GST / 100);
                            if (selectedItem.QtyF > 0)
                            {
                                existingMedicine.QtyF += selectedItem.QtyF;
                                decimal currentFullTotal = selectedItem.QtyF * priceWithGST;
                            
                                existingMedicine.qtFTotal += currentFullTotal;
                            }
                            if (selectedItem.QtyL > 0)
                            { 
                                decimal number = 0;
                                Match match = Regex.Match(existingMedicine.StripInfo, @"\d+");
                                if (match.Success)
                                {
                                    number = Convert.ToDecimal(match.Value);
                                }

                                if (number > 0)
                                { 
                                    decimal ptrPerLooseUnit = basePrice / number;
                                    decimal loosePriceAfterDiscount = ptrPerLooseUnit - (ptrPerLooseUnit * selectedItem.Discount / 100);
                                    decimal loosePriceWithGST = loosePriceAfterDiscount + (loosePriceAfterDiscount * selectedItem.GST / 100);
                                   
                                    existingMedicine.QtyL += selectedItem.QtyL;
                                    existingMedicine.SchAmt = existingMedicine.QtyL * loosePriceAfterDiscount;
                                    decimal currentLooseTotal = selectedItem.QtyL * loosePriceWithGST;
                                    existingMedicine.qtLTotal += currentLooseTotal;
                                }
                            }
                            existingMedicine.Total = existingMedicine.qtFTotal + existingMedicine.qtLTotal;
                        }
                        else
                        {
                            PurchaseMedicine newItem = new PurchaseMedicine
                            {
                                ItemId = selectedItem.ItemId,
                                ProductName = selectedItem.ProductName,
                                BatchNumber = selectedItem.BatchNumber,
                                Expiry = DateTime.TryParseExact(selectedItem.expiryMedicine, new[] { "MM/yy", "M/yy", "MM/yyyy", "M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "MMM dd yyyy hh:mmt", "MMM dd yyyy hh:mmtt" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiryDate) ? expiryDate : DateTime.MinValue,
                                StripInfo = selectedItem.StripInfo,
                                MRP = selectedItem.MRP, 
                                PTR = selectedItem.PTR, 
                                Discount = selectedItem.Discount,
                                GST = selectedItem.GST,
                                SchAmt = selectedItem.SchAmt,
                                baseAmt = selectedItem.baseAmt,
                                QtyL = selectedItem.QtyL,
                                QtyF = selectedItem.QtyF,
                                Total = selectedItem.Total, 
                                CompanyName = selectedItem.CompanyName,
                                medicineType = selectedItem.medicineType,
                                saltComposition1 = selectedItem.saltComposition1,
                                saltComposition2 = selectedItem.saltComposition2
                            };
                            medicineBilling.Add(newItem);
                        }
                    }
                    decimal totalAmount = medicineBilling.Sum(m => m.Total);
                    Total_Amount.Text = "Grand Total: " + totalAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN"));
                    dialogPaidAmount.Text = totalAmount.ToString("F2");

                    ProductGrid.ItemsSource = null;
                    ProductGrid.ItemsSource = medicineBilling;
                    formPaymentType.IsEnabled = true;

                    MessageBox.Show($"All items from Bill No. {selectedBill.BillNumber} have been added to the current bill.", "Bill Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ProductName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock == null) return;

            var row = FindVisualParent<DataGridRow>(textBlock);
            if (row == null) return;

            if (row.DetailsVisibility == Visibility.Visible)
            {
                row.DetailsVisibility = Visibility.Collapsed;
            }
            else
            {
                row.DetailsVisibility = Visibility.Visible;
            }

            e.Handled = true;
        }

        public static T FindVisualParent<T>(UIElement child) where T : UIElement
        {
            var parent = VisualTreeHelper.GetParent(child) as UIElement;
            while (parent != null)
            {
                if (parent is T typedParent)
                {
                    return typedParent;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
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
            formDistributorName.Clear();
  
            formPaymentType.SelectedItem = null;
            PreviousPurchasesPanel.Visibility = Visibility.Collapsed;
            //PreviousPurchasesGrid.ItemsSource = null;
            PreviousPurchasesItemsControl.ItemsSource = null;

            // formCreatedBy.Clear();
            formGSTOption.SelectedItem = null;
            formBillDate.SelectedDate = DateTime.Now;
            formCreateAt.SelectedDate = DateTime.Now;

            medicineBilling.Clear();
            ProductGrid.ItemsSource = null;
            Total_Amount.Text = "Grand Amount: ";
            InitializeComponent();
        }

        

    }
    public class PreviousBill
    {
        public string BillNumber { get; set; }
        public DateTime? BillDate { get; set; }
        public ObservableCollection<PurchaseMedicine> Items { get; set; }
        public string BillNumberWithDate => $"{BillNumber} (Date: {BillDate?.ToShortDateString()})";
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
