using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using DataGrid = System.Windows.Controls.DataGrid;
using GridView = System.Windows.Controls.GridView;
using ListBox = System.Windows.Controls.ListBox;
namespace Phramacy_Product.Views.DBMaster
{ 
    public class DBMasterConnection : System.Web.UI.Page
    {
        private static string main_connection()
        {
            return "Data Source=localhost; Initial Catalog=ReactDB; User ID=sa;Password='Ygkpa@457';";
        }

        public List<Medicine> GetMedicines(string input)
        {
            var results = new List<Medicine>();
            string query = $"sp_getPharmaData '{input}'";

            try
            {
                DataTable dt = GD(query);

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow reader in dt.Rows)
                    {
                        results.Add(new Medicine
                        {
                            ProductName = reader["name"].ToString(),
                            CompanyName = reader["manufacturer_name"].ToString(),
                            StripInfo = reader["pack_size_label"].ToString(),
                            BatchNumber = reader["Batch"].ToString(),
                            ItemId = Convert.ToInt32(reader["id"]),
                            MRP = Convert.ToDecimal(reader["price"]),
                            PTR = Convert.ToDecimal(reader["PTR"]),
                            Stock = Convert.ToInt32(reader["Quantity"]),
                            Expiry = reader["Expiry"] != DBNull.Value ? Convert.ToDateTime(reader["Expiry"]) : DateTime.MinValue,
                            medicineType = reader["type"].ToString(),
                            gST = reader["GST"] != DBNull.Value ? Convert.ToDecimal(reader["GST"]) : 0,
                            Discount = reader["discount"] != DBNull.Value ? Convert.ToDecimal(reader["discount"]) : 0,
                            saltComposition1 = reader["short_composition1"].ToString(),
                            saltComposition2 = reader["short_composition2"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return results;
        }
        public static string Encrypt(string clearText)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public static string Decrypt(string cipherText)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
        public static int IUD(string my_qry)
        {
            int status = 0;
            SqlConnection con = new SqlConnection(main_connection());
            try
            {
                if ((con.State == ConnectionState.Open))
                {
                    con.Close();
                }
                con.Open();
                SqlCommand cmd = new SqlCommand(my_qry, con);
                cmd.CommandType = CommandType.Text;
                status = cmd.ExecuteNonQuery();
                return status;
            }
            catch (Exception ex)
            {
                return status;
            }
            finally
            {
                con.Close();
                con.Dispose();
            }
        }
        public static DataTable GD(string qry)
        {
            SqlConnection my_conn = new SqlConnection(main_connection());
            if (qry == null)
            {
                throw (new ArgumentNullException("text"));
            }
            try
            {
                if (my_conn.State == ConnectionState.Open)
                {
                    my_conn.Close();
                }
                SqlCommand my_cmd = new SqlCommand(qry, my_conn);
                my_cmd.CommandTimeout = 120;
                SqlDataAdapter my_da = new SqlDataAdapter(my_cmd);
                DataTable dt = new DataTable();
                my_da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static void FGV(string qry, DataGrid my_dg)
        {
            try
            {
                DataTable my_dt = GD(qry);
                if (my_dt != null && my_dt.Rows.Count > 0)
                {
                    my_dg.ItemsSource = my_dt.DefaultView;
                }
                else
                {
                    my_dg.ItemsSource = null; 
                }
            }
            catch (Exception ex)
            {
                        throw;
            }
        }

        public static void FLB(string qry, ListBox my_lb)
        {
            try
            {
                DataTable my_dt = GD(qry);
                if (my_dt != null && my_dt.Rows.Count > 0)
                {
                    my_lb.ItemsSource = my_dt.DefaultView;
                }
                else
                {
                    my_lb.ItemsSource = null; 
                }
            }
            catch (Exception ex)
            {
                
                throw;
            }
        }
        public static void FDDL(string qry, DropDownList my_ddl)
        {
            try
            {
                DataTable my_dt = GD(qry);
                if ((my_dt.Rows.Count > 0))
                {
                    my_ddl.DataSource = my_dt;
                    my_ddl.DataBind();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void FCL(string qry, ListBox lst)
        {
            SqlDataReader rdr;
            SqlConnection my_conn = new SqlConnection(main_connection());
            try
            {
                if (my_conn.State == ConnectionState.Open)
                {
                    my_conn.Close();
                }
                my_conn.Open();
                SqlCommand my_cmd = new SqlCommand(qry, my_conn);
                rdr = my_cmd.ExecuteReader();
                if (rdr.HasRows)
                {
                    int i = 0;
                    while (rdr.Read())
                    {
                        lst.Items.Insert(i, i + 1 + ". " + rdr[0].ToString());
                        i = i + 1;
                    }
                }
            }
            catch (Exception ee)
            {
                throw ee;
            }
            finally
            {
                my_conn.Close();
                my_conn.Dispose();
            }
        }
    }
}