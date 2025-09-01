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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Phramacy_Product.Views.Purchase.PurchaseOrders
{
    public partial class PurchaseOrder : Page
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        public PurchaseOrder()
        {
            InitializeComponent();
            LoadPurchaseOrders();
        }

        private void LoadPurchaseOrders()
        {
            try
            {
                string sqlQuery = @"
                    SELECT
                        po.PurchaseOrderID,
                        po.CreatedAt,
                        SUM(poi.RequestedQuantity) AS ItemsCount
                    FROM
                        [dbo].[PurchaseOrders] po
                    LEFT JOIN
                        [dbo].[PurchaseOrderItems] poi ON po.PurchaseOrderID = poi.PurchaseOrderID
                    WHERE
                        po.IsDeleted = 0
                    GROUP BY
                        po.PurchaseOrderID, po.CreatedAt
                    ORDER BY
                        po.CreatedAt DESC;";

                var purchaseOrders = new List<OrderQuantity>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    int totalOrderCount = 0;
                    while (reader.Read())
                    {
                        var order = new OrderQuantity
                        {
                            PurchaseOrderID = reader.GetInt32(0),
                            CreatedAt = reader.GetDateTime(1),
                            ItemsCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
                        };
                        purchaseOrders.Add(order);
                        totalOrderCount++;
                    }
                    reader.Close();
                    HeaderTextBlock.Text = $"Purchase Orders ({totalOrderCount})";
                    PurchaseOrderDataGrid.ItemsSource = purchaseOrders;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
    }
}
