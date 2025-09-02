using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phramacy_Product.DataModel;
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

        public List<Medicine> GetMedicines(string connectionString,string input)
        {
            var results = new List<Medicine>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"sp_getPharmaDataForDistributor '" + input + "'";

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
        public string GenerateBillNumber(SqlConnection conn)
          {
                string today = DateTime.Now.ToString("ddMMyyyy");
                string billNumber = "";

                string query = @"
            SELECT COUNT(*) 
            FROM PurchaseDetails
            WHERE CAST(BillDate AS DATE) = CAST(GETDATE() AS DATE)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    int countToday = (int)cmd.ExecuteScalar();
                    billNumber = $"{today}-{(countToday + 1).ToString("D3")}";
                }

                return billNumber;
            }
        }
}

