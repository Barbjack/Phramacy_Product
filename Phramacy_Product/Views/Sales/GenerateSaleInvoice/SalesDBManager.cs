using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phramacy_Product.DataModel;
using System.Data.SqlClient;

namespace Phramacy_Product.Views.Sales.GenerateSaleInvoice
{
    public class SalesDBManager
    {
        public bool checkCustomerExist(string inputNumber, SqlConnection conn)
        {
            string query = "SELECT Mobile FROM PharmaCustomers WHERE Mobile = @Mobile";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@Mobile", inputNumber);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }

        public string GenerateBillNumber(SqlConnection conn)
        {
            string today = DateTime.Now.ToString("ddMMyyyy");
            string billNumber = "";

            string query = @"
            SELECT COUNT(*) 
            FROM SaleDetails 
            WHERE CAST(BillDate AS DATE) = CAST(GETDATE() AS DATE)";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                int countToday = (int)cmd.ExecuteScalar();
                billNumber = $"{today}-{(countToday + 1).ToString("D3")}";
            }

            return billNumber;
        }
        public void updatePharmaCustomer(SqlConnection conn, string customerName, string mobile, decimal totalAmount, decimal totalPaidAmount, bool customerExists)
        {
            decimal pendingAmount = 0.0m;
            pendingAmount = totalAmount - totalPaidAmount;

            string query = customerExists ?
                "UPDATE PharmaCustomers SET PendingAmount = PendingAmount + @PendingAmount, UpdatedAt = @UpdatedAt WHERE Mobile = @Mobile" :
                "INSERT INTO PharmaCustomers (CustomerName, Mobile, PendingAmount, CreatedAt) VALUES (@CustomerName, @Mobile, @PendingAmount, @CreatedAt)";

            using (SqlCommand com = new SqlCommand(query, conn))
            {
                com.Parameters.AddWithValue("@Mobile", mobile);
                com.Parameters.AddWithValue("@PendingAmount", pendingAmount);
                if (customerExists)
                {
                    com.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                }
                else
                {
                    com.Parameters.AddWithValue("@CustomerName", customerName);
                    com.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                }
                com.ExecuteNonQuery();
            }
            conn.Close();
      } 
      
    }
}
