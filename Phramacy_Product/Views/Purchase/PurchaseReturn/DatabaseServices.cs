using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Phramacy_Product.Views.Purchase.PurchaseReturn
{
    public class DatabaseServices
    {
            private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;

            public PurchaseDetail GetPurchaseDetailByBillNumber(string billNumber)
            {
                PurchaseDetail purchaseDetail = null;
                string query = "SELECT PurchaseID, DistributorName, BillNumber, BillDate,TotalAmount,PaidAmount,PaymentType FROM PurchaseDetails WHERE BillNumber = @billNumber;";
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@billNumber", billNumber);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            purchaseDetail = new PurchaseDetail
                            {
                                PurchaseID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                DistributorName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                BillNumber = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                BillDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                TotalAmount = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                                PaidAmount = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                                PaymentType = reader.IsDBNull(6) ? "Cash" : reader.GetString(6)
                            };
                    }
                    }
                }
                return purchaseDetail;
            }
            public List<PurchaseItemReturn> GetPurchaseItemsBySaleId(int purchaseId)
            {
                var items = new List<PurchaseItemReturn>();
                string query = "SELECT pi.ItemID, pi.PurchaseID, pi.MedId, pi.ItemName, pi.Batch,pi.Pack, pi.Expiry,pi.Quantity, pi.Is_Loose," +
                               " pi.MRP,pi.PTR, pi.Discount, pi.GST, pi.NetAmount, pi.Is_Returned " +
                               "FROM PurchaseItems pi WHERE pi.PurchaseID = @PurchaseId and pi.Is_Returned=0;";
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@PurchaseId", purchaseId);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new PurchaseItemReturn
                            {
                                // Use ternary operators with IsDBNull() to handle potential nulls
                                ItemID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                PurchaseID = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                MedId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                ItemName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Batch = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Pack = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Expiry = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                FullQty = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                Is_Loose = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                                MRP = reader.IsDBNull(9) ? 0m : reader.GetDecimal(9),
                                PTR = reader.IsDBNull(10)?0m : reader.GetDecimal(10),
                                Discount = reader.IsDBNull(11) ? 0m : reader.GetDecimal(11),
                                GST = reader.IsDBNull(12) ? 0m : reader.GetDecimal(12),
                                NetAmount = reader.IsDBNull(13) ? 0m : reader.GetDecimal(13),
                                Is_Returned = reader.IsDBNull(14) ? false : reader.GetBoolean(14),
                                ReturnQty = 0,
                                IsSelected = false
                            });
                        }
                    }
                }
                return items;
            }

        public void ProcessPurchaseReturn(List<PurchaseItemReturn> returnedItems, PurchaseDetail currentPurchase, string createdBy)
        {
            if (returnedItems == null || returnedItems.Count == 0 || currentPurchase == null)
            {
                return;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    foreach (var item in returnedItems)
                    {
                        decimal priceAfterDiscount = item.PTR - (item.PTR * item.Discount / 100);
                        decimal returnAmount = item.ReturnQty * (priceAfterDiscount + (priceAfterDiscount * item.GST / 100));
                        decimal currentTotalAmount = currentPurchase.TotalAmount;
                        decimal currentPaidAmount = currentPurchase.PaidAmount;
                        decimal actualReturnAmount = returnAmount - (currentTotalAmount - currentPaidAmount);
                        string updateMedicineQuery = "UPDATE Pharma_Medicines SET Quantity = Quantity - @returnQty, UpdatedAt = GETDATE() WHERE id = @MedId;";
                        var medicineCommand = new SqlCommand(updateMedicineQuery, connection, transaction);
                        medicineCommand.Parameters.AddWithValue("@returnQty", item.ReturnQty);
                        medicineCommand.Parameters.AddWithValue("@MedId", item.MedId);
                        medicineCommand.ExecuteNonQuery();

                        if (item.ReturnQty == item.FullQty)
                        {
                            string updatePurchaseItemQuery = "UPDATE PurchaseItems SET Is_Returned = 1, UpdatedAt = GETDATE() WHERE ItemID = @ItemId;";
                            var saleItemCommand = new SqlCommand(updatePurchaseItemQuery, connection, transaction);
                            saleItemCommand.Parameters.AddWithValue("@ItemId", item.ItemID);
                            saleItemCommand.ExecuteNonQuery();
                        }
                        else if (item.ReturnQty < item.FullQty)
                        {
                            decimal netAmountPerUnit = (item.PTR - (item.PTR * item.Discount / 100)) +
                               ((item.PTR - (item.PTR * item.Discount / 100)) * item.GST / 100);

                            decimal newNetAmount = (item.FullQty - item.ReturnQty) * netAmountPerUnit;

                            string updatePurchaseItemQuery = "UPDATE PurchaseItems SET Quantity = @remainingQty, NetAmount = @newNetAmount, UpdatedAt = GETDATE() WHERE ItemID = @ItemId;";
                            var saleItemCommand = new SqlCommand(updatePurchaseItemQuery, connection, transaction);
                            saleItemCommand.Parameters.AddWithValue("@remainingQty", item.FullQty - item.ReturnQty);
                            saleItemCommand.Parameters.AddWithValue("@newNetAmount", newNetAmount);
                            saleItemCommand.Parameters.AddWithValue("@ItemId", item.ItemID);
                            saleItemCommand.ExecuteNonQuery();
                        }

                        // 3. Insert a new record into SaleReturns
                        string insertReturnQuery = "INSERT INTO PurchaseReturns (ItemID, PurchaseID,CreatedBy,TotalReturnAmount, ReturnQuantity, CreatedAt) " +
                                                   "VALUES (@ItemId, @PurchaseId,@CreatedBy, @totalReturnAmount, @returnQuantity, GETDATE());";
                        var returnCommand = new SqlCommand(insertReturnQuery, connection, transaction);
                        returnCommand.Parameters.AddWithValue("@ItemId", item.ItemID);
                        returnCommand.Parameters.AddWithValue("@PurchaseId", item.PurchaseID);
                        returnCommand.Parameters.AddWithValue("@CreatedBy", createdBy);
                        returnCommand.Parameters.AddWithValue("@totalReturnAmount", actualReturnAmount);
                        returnCommand.Parameters.AddWithValue("@returnQuantity", item.ReturnQty);
                        returnCommand.ExecuteNonQuery();

                        string updatePurchaseDetailsQuery = @"
UPDATE PurchaseDetails 
SET 
    TotalAmount = TotalAmount - @returnAmount, 
    PaidAmount = PaidAmount - @actualReturnAmount,
    PendingAmount = (TotalAmount - @returnAmount) - (PaidAmount - @actualReturnAmount),
    ReturnAmount = ReturnAmount + @returnAmount,
    PaymentStatus = CASE 
                        WHEN (TotalAmount - @returnAmount) = (PaidAmount - @actualReturnAmount) THEN 'Completed'
                        WHEN (PaidAmount - @actualReturnAmount) > 0 AND (TotalAmount - @returnAmount) > (PaidAmount - @actualReturnAmount) THEN 'Pending'
                        WHEN (PaidAmount - @actualReturnAmount) = 0 THEN 'Pending'
                        ELSE 'Completed'
                    END,
    UpdatedAt = GETDATE()
WHERE PurchaseID = @PurchaseId;";

                        var purchaseDetailsCommand = new SqlCommand(updatePurchaseDetailsQuery, connection, transaction);
                        // @returnAmount is the full value of the returned item (TotalAmount reduction)
                        purchaseDetailsCommand.Parameters.AddWithValue("@returnAmount", returnAmount);
                        // @actualReturnAmount is the amount adjusted/refunded from PaidAmount (PaidAmount reduction)
                        purchaseDetailsCommand.Parameters.AddWithValue("@actualReturnAmount", actualReturnAmount);
                        purchaseDetailsCommand.Parameters.AddWithValue("@PurchaseId", currentPurchase.PurchaseID);
                        purchaseDetailsCommand.ExecuteNonQuery();

                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"An error occurred during the return process: {ex.Message}");
                    throw;
                }
            }
        }

    }
    }