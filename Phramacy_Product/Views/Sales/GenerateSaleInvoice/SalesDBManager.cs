using Phramacy_Product.DataModel;
using Phramacy_Product.Views.DBMaster;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Phramacy_Product.Views.Sales.GenerateSaleInvoice
{
    public class SalesDBManager
    {
        public bool checkCustomerExist(string inputNumber)
        {
            string query = $"SELECT Mobile FROM PharmaCustomers WHERE Mobile = '{inputNumber.Replace("'", "''")}'";
            try
            {
                DataTable dt = DBMasterConnection.GD(query);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {   
                Console.WriteLine($"Error checking customer existence: {ex.Message}");
                return false;
            }
        }

        public string GenerateBillNumber()
        {
            string today = DateTime.Now.ToString("ddMMyyyy");
            string billNumber = "";
            string query = @"SELECT COUNT(*) FROM SaleDetails WHERE CAST(BillDate AS DATE) = CAST(GETDATE() AS DATE)";
            try
            {
                DataTable dt = DBMasterConnection.GD(query);
                if (dt != null && dt.Rows.Count > 0)
                {
                    int countToday = Convert.ToInt32(dt.Rows[0][0]);
                    billNumber = $"{today}-{(countToday + 1).ToString("D3")}";
                }
                else
                {
                    billNumber = $"{today}-001";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating bill number: {ex.Message}");
                 billNumber = $"{today}-ERR";
            }

            return billNumber;
        }
        public void updatePharmaCustomer(string customerName, string mobile, decimal totalAmount, decimal totalPaidAmount, bool customerExists)
        {
            decimal pendingAmount = totalAmount - totalPaidAmount;
            string query;

            if (customerExists)
            {
                query = $"UPDATE PharmaCustomers SET PendingAmount = PendingAmount + {pendingAmount}, UpdatedAt = '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' WHERE Mobile = '{mobile}'";
            }
            else
            {
                query = $"INSERT INTO PharmaCustomers (CustomerName, Mobile, PendingAmount, CreatedAt) VALUES ('{customerName.Replace("'", "''")}', '{mobile}', {pendingAmount}, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}')";
            }
            try
            {
                int rowsAffected = DBMasterConnection.IUD(query);
                if (rowsAffected == 0)
                {
                    MessageBox.Show("No rows were updated or inserted. Check if the customer exists.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}");
            }
        }

    }
}
