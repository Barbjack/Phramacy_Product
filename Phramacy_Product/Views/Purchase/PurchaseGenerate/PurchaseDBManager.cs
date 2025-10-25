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
namespace Phramacy_Product.Views.Purchase.PurchaseGenerate
{   
    public class PurchaseDBManager
    {
        public List<DistributorDetail> GetDistributorDetails(string connectionString,String input)
        {
            String query = @"select Name,ContactNumber from Distributors where ContactNumber Like @search + '%'";
            List<DistributorDetail> newDistributorList = new List<DistributorDetail>();
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
                            DistributorDetail newDistributor = new DistributorDetail
                            {
                                DistributorName = reader["Name"].ToString(),
                                DistributorNumber = reader["ContactNumber"].ToString()
                            };
                            newDistributorList.Add(newDistributor);
                        }
                    }
                }
            }
            return newDistributorList;
        }
        public string GenerateBillNumber()
        {
            string today = DateTime.Now.ToString("ddMMyyyy");
            string billNumber = "";
            string query = @"SELECT COUNT(*) FROM PurchaseDetails WHERE CAST(BillDate AS DATE) = CAST(GETDATE() AS DATE)";
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

        //public string GenerateBillNumber(SqlConnection conn)
        //  {
        //        string today = DateTime.Now.ToString("ddMMyyyy");
        //        string billNumber = "";

        //        string query = @"
        //    SELECT COUNT(*) 
        //    FROM PurchaseDetails
        //    WHERE CAST(BillDate AS DATE) = CAST(GETDATE() AS DATE)";
        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            conn.Open();
        //            int countToday = (int)cmd.ExecuteScalar();
        //            billNumber = $"{today}-{(countToday + 1).ToString("D3")}";
        //        }

        //        return billNumber;
        //    }
    }
}

