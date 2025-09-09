using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Phramacy_Product.Views.Sales.SaleReturn
{
    public class DatabaseServices
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;

        public SaleDetail GetSaleDetailByBillNumber(string billNumber)
        {
            SaleDetail saleDetail = null;
            string query = "SELECT SaleID, CustomerName, BillNumber, BillDate, TotalAmount,PaidAmount,PaymentType FROM SaleDetails WHERE BillNumber = @billNumber;";
            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@billNumber", billNumber);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        saleDetail = new SaleDetail
                        {
                            SaleID = reader.GetInt32(0),
                            CustomerName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            BillNumber = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            BillDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                            TotalAmount = reader.GetDecimal(4),
                            PaidAmount = reader.GetDecimal(5),
                            PaymentStatus = reader.IsDBNull(6) ? "Cash" : reader.GetString(6)
                        };
                    }
                }
            }
            return saleDetail;
        }

        public List<SaleItemReturn> GetSaleItemsBySaleId(int saleId)
        {
            var items = new List<SaleItemReturn>();
            string query = "SELECT si.SaleItemID, si.SaleID, si.ItemId, si.ItemName, si.Batch, si.Pack, si.Expiry, si.Quantity," +
                           " si.MRP, si.Discount, si.GST, si.NetAmount, si.Is_Returned,si.QtyLoose " +
                           "FROM SaleItems si WHERE si.SaleID = @saleId and si.Is_Returned=0 and si.Quantity >0 ;";
            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@saleId", saleId);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new SaleItemReturn
                        {
                            // Use ternary operators with IsDBNull() to handle potential nulls
                            SaleItemID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            SaleID = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                            ItemId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                            ItemName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Batch = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Pack = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Expiry = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            FullQty = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                            //Is_Loose = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                            MRP = reader.IsDBNull(8) ? 0m : reader.GetDecimal(8),
                            Discount = reader.IsDBNull(9) ? 0m : reader.GetDecimal(9),
                            GST = reader.IsDBNull(10) ? 0m : reader.GetDecimal(10),
                            NetAmount = reader.IsDBNull(11) ? 0m : reader.GetDecimal(11),
                            Is_Returned = reader.IsDBNull(12) ? false : reader.GetBoolean(12),
                            ReturnQty = 0,
                            LooseQty = reader.IsDBNull(13)?0:reader.GetInt32(13),
                            IsSelected = false
                        });
                    }
                }
            }
            return items;
        }

        public void ProcessSaleReturn(List<SaleItemReturn> returnedItems, SaleDetail currentSale, string createdBy)
        {
            if (returnedItems == null || returnedItems.Count == 0 || currentSale == null)
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
                        // Calculation of the return amount, including discount and GST
                        decimal priceAfterDiscount = item.MRP - (item.MRP * item.Discount / 100);
                        decimal returnAmount = item.ReturnQty * (priceAfterDiscount + (priceAfterDiscount * item.GST / 100));
                        decimal currentTotalAmount = currentSale.TotalAmount;
                        decimal currentPaidAmount = currentSale.PaidAmount;
                        decimal actualReturnAmount = returnAmount - (currentTotalAmount - currentPaidAmount);
                        // 1. Update Pharma_Medicines: Increment stock quantity
                        string updateMedicineQuery = "UPDATE Pharma_Medicines SET Quantity = Quantity + @returnQty, UpdatedAt = GETDATE() WHERE id = @itemId;";
                        var medicineCommand = new SqlCommand(updateMedicineQuery, connection, transaction);
                        medicineCommand.Parameters.AddWithValue("@returnQty", item.ReturnQty);
                        medicineCommand.Parameters.AddWithValue("@itemId", item.ItemId);
                        medicineCommand.ExecuteNonQuery();

                        // 2. Update SaleItems: Adjust quantity and mark as fully returned if applicable
                        if (item.ReturnQty == item.FullQty)
                        {
                            string updateSaleItemQuery = "UPDATE SaleItems SET Quantity = Quantity-@returnQty,NetAmount =NetAmount-@returnAmount Is_Returned = 1, ModifiedAt = GETDATE() WHERE SaleItemID = @saleItemId;";
                            var saleItemCommand = new SqlCommand(updateSaleItemQuery, connection, transaction);
                            saleItemCommand.Parameters.AddWithValue("@saleItemId", item.SaleItemID);
                            saleItemCommand.Parameters.AddWithValue("@returnQty", item.ReturnQty);
                            saleItemCommand.Parameters.AddWithValue("@returnAmount", returnAmount);
                            saleItemCommand.ExecuteNonQuery();
                        }
                        else if (item.ReturnQty < item.FullQty)
                        {
                            string updateSaleItemQuery = "UPDATE SaleItems SET Quantity = @remainingQty, NetAmount = NetAmount - @newReturnAmount, ModifiedAt = GETDATE() WHERE SaleItemID = @saleItemId;";
                            var saleItemCommand = new SqlCommand(updateSaleItemQuery, connection, transaction);
                            saleItemCommand.Parameters.AddWithValue("@remainingQty", item.FullQty - item.ReturnQty);
                            saleItemCommand.Parameters.AddWithValue("@newReturnAmount", returnAmount);
                            saleItemCommand.Parameters.AddWithValue("@saleItemId", item.SaleItemID);
                            saleItemCommand.ExecuteNonQuery();
                        }

                        // 3. Insert a new record into SaleReturns
                        string insertReturnQuery = "INSERT INTO SaleReturns (SaleItemID, SaleID, ReturnDate,CreatedBy,TotalReturnAmount, ReturnQuantity, CreatedAt) " +
                                                   "VALUES (@saleItemId, @saleId, GETDATE(),@CreatedBy, @totalReturnAmount, @returnQuantity, GETDATE());";
                        var returnCommand = new SqlCommand(insertReturnQuery, connection, transaction);
                        returnCommand.Parameters.AddWithValue("@saleItemId", item.SaleItemID);
                        returnCommand.Parameters.AddWithValue("@saleId", item.SaleID);
                        returnCommand.Parameters.AddWithValue("@CreatedBy", createdBy);
                        returnCommand.Parameters.AddWithValue("@totalReturnAmount", returnAmount);
                        returnCommand.Parameters.AddWithValue("@returnQuantity", item.ReturnQty);
                        returnCommand.ExecuteNonQuery();

                        // 4. Update SaleDetails: Adjust total and paid amounts


                        string updateSaleDetailsQuery = "UPDATE SaleDetails SET TotalAmount = TotalAmount - @returnAmount, PaidAmount = PaidAmount - @actualReturnAmount WHERE SaleID = @saleId;";
                        var saleDetailsCommand = new SqlCommand(updateSaleDetailsQuery, connection, transaction);
                        saleDetailsCommand.Parameters.AddWithValue("@returnAmount", returnAmount);
                        saleDetailsCommand.Parameters.AddWithValue("@actualReturnAmount", actualReturnAmount);
                        saleDetailsCommand.Parameters.AddWithValue("@saleId", currentSale.SaleID);
                        saleDetailsCommand.ExecuteNonQuery();

                        // 5. Update PharmaCustomers: Recalculate and update pending amount
                        if (!string.IsNullOrEmpty(currentSale.CustomerName))
                        {
                            // Fetch the updated TotalAmount and PaidAmount from SaleDetails
                            string fetchUpdatedAmountsQuery = "SELECT TotalAmount, PaidAmount FROM SaleDetails WHERE SaleID = @saleId;";
                            var fetchCommand = new SqlCommand(fetchUpdatedAmountsQuery, connection, transaction);
                            fetchCommand.Parameters.AddWithValue("@saleId", currentSale.SaleID);

                            decimal newTotalAmount = 0;
                            decimal newPaidAmount = 0;

                            using (var reader = fetchCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    newTotalAmount = reader.IsDBNull(0) ? 0m : reader.GetDecimal(0);
                                    newPaidAmount = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                                }
                            }

                            // Calculate the new pending amount
                            decimal newPendingAmount = newTotalAmount - newPaidAmount;

                            // Update the PharmaCustomers table
                            string updateCustomerQuery = "UPDATE PharmaCustomers SET PendingAmount = PendingAmount-@newPendingAmount, UpdatedAt = GETDATE() WHERE CustomerName = @customerName;";
                            var customerCommand = new SqlCommand(updateCustomerQuery, connection, transaction);
                            customerCommand.Parameters.AddWithValue("@newPendingAmount", newPendingAmount);
                            customerCommand.Parameters.AddWithValue("@customerName", currentSale.CustomerName);
                            customerCommand.ExecuteNonQuery();
                        }

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
        public void updateInvoiceBill(string billPath, string billNo)
        {
            string query = @"update SaleDetails set BillPath=@BillPath where BillNumber = @BillNumber;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BillPath", billPath);
                    cmd.Parameters.AddWithValue("@BillNumber", billNo);
                    cmd.ExecuteNonQuery();
                }
            }

        }

    }
}