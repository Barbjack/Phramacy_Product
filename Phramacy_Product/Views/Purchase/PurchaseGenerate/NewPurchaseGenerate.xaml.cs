using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Phramacy_Product.Views.Sales.GenerateSaleInvoice;
using System.Collections.ObjectModel;
using Phramacy_Product.Views.DBMaster;
using System.Data;
using Phramacy_Product.Views.Sales;

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
                        QtyL = foundMedicine.QtyL,
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
                //PreviousPurchasesItemsControl.ItemsSource = null;
                //PreviousPurchasesPanel.Visibility = Visibility.Collapsed;
                //BillCountComboBox.SelectedItem = null;
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

            //decimal paidAmount = medicineBilling.Sum(m => m.PaidAmount);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
               //bool customerExists = saleDBManager.checkCustomerExist(inputNumber);
                String mobile = SearchNumberBox.Text;
                String distributorName = formDistributorName.Text;
                //purchaseDBManager.updatePharmaCustomer(customerName, mobile, totalAmount, totalPaidAmount, customerExists);
                string billNumber = new SalesDBManager().GenerateBillNumber();
                String billPath = SaveButton_Click(sender, e, billNumber);
                UpdateSaleItemDetails(conn, billNumber, billPath, totalAmount, totalPaidAmount);
                MessageBox.Show("Invoice is saved to the file : " + billPath, "Invoice Saved", MessageBoxButton.OK, MessageBoxImage.Information);

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

        private void UpdateSaleItemDetails(SqlConnection conn, String billNumber, String billPath, decimal totalAmount, decimal paidAmount)
        {
            conn.Open();
            SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                string saleDetailsQuery = @"
                INSERT INTO SaleDetails 
               (CustomerName, DoctorName, BillNumber, BillDate,PaidAmount,TotalAmount, CreatedBy,BillPath, PaymentType,Status,PayAppName,TsNum, CreatedAt, PatientName) 
               OUTPUT INSERTED.SaleID 
               VALUES(@CustomerName, @DoctorName, @BillNumber, @BillDate, @PaidAmount,@TotalAmount, @CreatedBy,@BillPath, @PaymentType,@Status,@PayAppName,@TsNum, @CreatedAt, @PatientName)";
                SqlCommand saleCmd = new SqlCommand(saleDetailsQuery, conn, transaction);

                saleCmd.Parameters.AddWithValue("@CustomerName", formDistributorName.Text);
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
                //saleCmd.Parameters.AddWithValue("@PatientName", formPatientName.Text);
                // Get the newly inserted SaleID
                int saleID = (int)saleCmd.ExecuteScalar();

                // 2. Insert into SaleItem for each item
                string insertItemQuery = @"
                        INSERT INTO SaleItems (SaleID,ItemId,ItemName,Batch,Expiry,Pack,MRP,Quantity,QtyLoose,Discount,GST,CreatedAt,Is_Loose,NetAmount)
                        VALUES (@SaleID,@ItemId,@ItemName,@Batch,@Expiry,@Pack,@MRP,@Quantity,@QtyLoose,@Discount,@GST,@CreatedAt,@Is_Loose,@NetAmount)";

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
                    itemCmd.Parameters.AddWithValue("@Quantity", med.QtyF);
                    itemCmd.Parameters.AddWithValue("@QtyLoose", med.QtyL);
                    itemCmd.Parameters.AddWithValue("@Discount", med.Discount);
                    itemCmd.Parameters.AddWithValue("@GST", med.GST);
                    itemCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    itemCmd.Parameters.AddWithValue("@Is_Loose", med.QtyL > 0);
                    itemCmd.Parameters.AddWithValue("@NetAmount", med.Total);
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
                MessageBox.Show("Error: " + ex.Message);
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

        [Obsolete]
        public String SaveButton_Click(object sender, RoutedEventArgs e, string billNo)
        {
            var sale = new SalePdfInvoice
            {
                CustomerName = formDistributorName.Text,
                Mobile = SearchNumberBox.Text,
                BillNo = billNo,
                Date = formBillDate.SelectedDate ?? DateTime.Now,
                PaymentType = formPaymentType.SelectedItem is ComboBoxItem item2 ? item2.Content.ToString() : "Cash",
            };
            List<PurchaseMedicine> medicineList = medicineBilling.ToList();
            return "Path is not decided yet";
            //return new PdfInvoiceGenerator.GenerateInvoice(sale, medicineList);
            //ShowPdfViewer(pdfPath);
            //pdfViewerControl.OpenFile(pdfPath);

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
