using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Phramacy_Product.DataModel;
using System.Configuration;
namespace Phramacy_Product.Views.Sales
{
    public class DatabaseService
    {
     private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        public List<SaleItemReturn> GetAllSaleItems()
        {
            var items = new List<SaleItemReturn>();
            string query = "SELECT si.SaleItemID, si.SaleID, si.ItemName, si.Batch, si.Expiry, si.Quantity, si.MRP, si.Discount, si.GST, si.NetAmount, " +
                   "si.Is_Loose, si.Is_Returned, sd.BillNumber FROM SaleItems si JOIN SaleDetails sd ON si.SaleID = sd.SaleID;";

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(query, connection);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new SaleItemReturn
                        {
                            SaleItemID = reader.GetInt32(0),
                            SaleID = reader.GetInt32(1),
                            ItemName = reader.GetString(2),
                            Batch = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Expiry = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            FullQty = reader.GetInt32(5),
                            MRP = reader.GetDecimal(6),
                            Discount = reader.GetDecimal(7),
                            GST = reader.GetDecimal(8),
                            NetAmount = reader.GetDecimal(9),
                            Is_Loose = reader.GetBoolean(10),
                            Is_Returned = reader.GetBoolean(11),
                            // Handle potential NULL values for BillNumber
                            BillNumber = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                            ReturnQty = 0,
                            IsSelected = false
                        });
                    }
                }
            }
            return items;
        }
        // This method gets sale details using the SaleID, which is retrieved from the selected item.
        public SaleDetail GetSaleDetailBySaleId(int saleId)
        {
            SaleDetail saleDetail = null;
            string query = "SELECT SaleID, CustomerName, BillNumber, BillDate, TotalAmount FROM SaleDetails WHERE SaleID = @saleId;";
            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@saleId", saleId);
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
                            TotalAmount = reader.GetDecimal(4)
                        };
                    }
                }
            }
            return saleDetail;
        }
        public void UpdateReturnedItems(List<SaleItemReturn> returnedItems)
        {
            string query = "UPDATE SaleItems SET Is_Returned = 1 WHERE SaleItemID = @saleItemId;";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (var item in returnedItems)
                {
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@saleItemId", item.SaleItemID);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
