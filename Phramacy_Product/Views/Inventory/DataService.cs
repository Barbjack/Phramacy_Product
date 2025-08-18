using Phramacy_Product.Views.Inventory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using Phramacy_Product.DataModel;
namespace Phramacy_Product.Views.Inventory
{
    public class DataService
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        public List<PharmaMedicine> GetMedicines(int page, int pageSize)
        {

            var medicines = new List<PharmaMedicine>();
            string query = "GetPaginatedMedicines '"+page+"','"+pageSize+"'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.CommandTimeout = 120;
                command.Parameters.AddWithValue("@PageNumber", page);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                try
                {
                    
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        medicines.Add(new PharmaMedicine
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            Name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name")),
                            Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? null : reader.GetString(reader.GetOrdinal("Batch")),
                            Price = reader.IsDBNull(reader.GetOrdinal("price")) ? 0 : (decimal)reader.GetDouble(reader.GetOrdinal("price")),
                            Expiry = reader.IsDBNull(reader.GetOrdinal("Expiry")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Expiry")),
                            Quantity = reader.IsDBNull(reader.GetOrdinal("Quantity")) ? 0 : reader.GetInt32(reader.GetOrdinal("Quantity")),
                            QtyInLoose = reader.IsDBNull(reader.GetOrdinal("QtyInLoose")) ? 0 : reader.GetInt32(reader.GetOrdinal("QtyInLoose")),
                            IsDiscontinued = reader.IsDBNull(reader.GetOrdinal("Is_discontinued")) ? false : reader.GetBoolean(reader.GetOrdinal("Is_discontinued")),
                            ManufacturerName = reader.IsDBNull(reader.GetOrdinal("manufacturer_name")) ? null : reader.GetString(reader.GetOrdinal("manufacturer_name")),
                            Type = reader.IsDBNull(reader.GetOrdinal("type")) ? null : reader.GetString(reader.GetOrdinal("type")),
                            PackSizeLabel = reader.IsDBNull(reader.GetOrdinal("pack_size_label")) ? null : reader.GetString(reader.GetOrdinal("pack_size_label")),
                            ShortComposition1 = reader.IsDBNull(reader.GetOrdinal("short_composition1")) ? null : reader.GetString(reader.GetOrdinal("short_composition1")),
                            ShortComposition2 = reader.IsDBNull(reader.GetOrdinal("short_composition2")) ? null : reader.GetString(reader.GetOrdinal("short_composition2")),
                            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                            IsDeleted = reader.IsDBNull(reader.GetOrdinal("IsDeleted")) ? false : reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                            Discount = reader.IsDBNull(reader.GetOrdinal("Discount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Discount")),
                            GST = reader.IsDBNull(reader.GetOrdinal("GST")) ? 0 : reader.GetDecimal(reader.GetOrdinal("GST"))
                        });
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fetching data: {ex.Message}");
                }
            }
            return medicines;
        }

        public void AddMedicine(PharmaMedicine newMedicine)
        {
            string query = @"
                INSERT INTO [ReactDB].[dbo].[Pharma_Medicines]
                ([name], [Batch], [price], [Expiry], [Quantity], [QtyInLoose], [Is_discontinued], 
                [manufacturer_name], [type], [pack_size_label], [short_composition1], 
                [short_composition2], [UpdatedAt], [IsDeleted], [Discount], [GST])
                VALUES
                (@name, @Batch, @price, @Expiry, @Quantity, @QtyInLoose, @IsDiscontinued, 
                @manufacturer_name, @type, @pack_size_label, @short_composition1, 
                @short_composition2, @UpdatedAt, @IsDeleted, @Discount, @GST)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@name", (object)newMedicine.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@Batch", (object)newMedicine.Batch ?? DBNull.Value);
                command.Parameters.AddWithValue("@price", newMedicine.Price);
                command.Parameters.AddWithValue("@Expiry", newMedicine.Expiry.HasValue ? (object)newMedicine.Expiry.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Quantity", newMedicine.Quantity);
                command.Parameters.AddWithValue("@QtyInLoose", newMedicine.QtyInLoose);
                command.Parameters.AddWithValue("@IsDiscontinued", newMedicine.IsDiscontinued);
                command.Parameters.AddWithValue("@manufacturer_name", (object)newMedicine.ManufacturerName ?? DBNull.Value);
                command.Parameters.AddWithValue("@type", (object)newMedicine.Type ?? DBNull.Value);
                command.Parameters.AddWithValue("@pack_size_label", (object)newMedicine.PackSizeLabel ?? DBNull.Value);
                command.Parameters.AddWithValue("@short_composition1", (object)newMedicine.ShortComposition1 ?? DBNull.Value);
                command.Parameters.AddWithValue("@short_composition2", (object)newMedicine.ShortComposition2 ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                command.Parameters.AddWithValue("@IsDeleted", newMedicine.IsDeleted);
                command.Parameters.AddWithValue("@Discount", newMedicine.Discount);
                command.Parameters.AddWithValue("@GST", newMedicine.GST);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    MessageBox.Show("Medicine added successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding medicine: {ex.Message}");
                }
            }
        }

        public int GetTotalMedicineCount()
        {
            string query = "GetTotalCountOfMedicines";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.CommandTimeout = 120;
                try
                {
                    connection.Open();
                    // Cast to long to handle large numbers (bigint)
                    object result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return (int)(long)result;
                    }
                    return 0; // Return 0 
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error getting count: {ex.Message}");
                    return 0;
                }
            }
        }

        public void UpdateMedicine(PharmaMedicine medicine)
        {
            string query = @"
                UPDATE [ReactDB].[dbo].[Pharma_Medicines] SET
                    [name] = @name, 
                    [Batch] = @Batch, 
                    [price] = @price, 
                    [Expiry] = @Expiry, 
                    [Quantity] = @Quantity, 
                    [QtyInLoose] = @QtyInLoose, 
                    [Is_discontinued] = @IsDiscontinued, 
                    [manufacturer_name] = @manufacturer_name, 
                    [type] = @type, 
                    [pack_size_label] = @pack_size_label, 
                    [short_composition1] = @short_composition1, 
                    [short_composition2] = @short_composition2, 
                    [UpdatedAt] = @UpdatedAt, 
                    [IsDeleted] = @IsDeleted, 
                    [Discount] = @Discount, 
                    [GST] = @GST
                WHERE id = @id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", medicine.Id);
                command.Parameters.AddWithValue("@name", (object)medicine.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@Batch", (object)medicine.Batch ?? DBNull.Value);
                command.Parameters.AddWithValue("@price", medicine.Price);
                command.Parameters.AddWithValue("@Expiry", medicine.Expiry.HasValue ? (object)medicine.Expiry.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Quantity", medicine.Quantity);
                command.Parameters.AddWithValue("@QtyInLoose", medicine.QtyInLoose);
                command.Parameters.AddWithValue("@IsDiscontinued", medicine.IsDiscontinued);
                command.Parameters.AddWithValue("@manufacturer_name", (object)medicine.ManufacturerName ?? DBNull.Value);
                command.Parameters.AddWithValue("@type", (object)medicine.Type ?? DBNull.Value);
                command.Parameters.AddWithValue("@pack_size_label", (object)medicine.PackSizeLabel ?? DBNull.Value);
                command.Parameters.AddWithValue("@short_composition1", (object)medicine.ShortComposition1 ?? DBNull.Value);
                command.Parameters.AddWithValue("@short_composition2", (object)medicine.ShortComposition2 ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                command.Parameters.AddWithValue("@IsDeleted", medicine.IsDeleted);
                command.Parameters.AddWithValue("@Discount", medicine.Discount);
                command.Parameters.AddWithValue("@GST", medicine.GST);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    MessageBox.Show("Medicine updated successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating medicine: {ex.Message}");
                }
            }
        }

        public void DeleteMedicine(int id)
        {
            string query = "DELETE FROM [ReactDB].[dbo].[Pharma_Medicines] WHERE id = @id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.CommandTimeout = 120;
                command.Parameters.AddWithValue("@id", id);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Medicine deleted successfully.");
                    }
                    else
                    {
                        MessageBox.Show("No medicine found with that ID.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting medicine: {ex.Message}");
                }
            }
        }
    }
}