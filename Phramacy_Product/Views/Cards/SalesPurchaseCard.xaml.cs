using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace Phramacy_Product.Views.Cards
{
    public partial class SalesPurchaseCard : UserControl
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
        public Func<double, string> Formatter { get; set; }
        public SalesPurchaseCard()
        {
            InitializeComponent();
            Formatter = value => "₹" + value.ToString("N0");
            DataContext = this;
            LoadData(0);
        }
        private void TimeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TimeTabControl.SelectedItem is TabItem selectedTab && selectedTab.Tag != null)
            {
                int days = int.Parse(selectedTab.Tag.ToString());
                LoadData(days);
            }
        }
        private void LoadData(int days)
        {
            decimal totalSales = 0;
            int salesCount = 0;

            decimal totalPurchase = 0;
            int purchaseCount = 0;

            string query = @"
                -- Sales
                SELECT ISNULL(SUM(NetAmount), 0) AS TotalAmount, COUNT(DISTINCT SaleID) AS OrderCount
                FROM SaleItems
                WHERE CreatedAt >= DATEADD(DAY, -@Days, GETDATE()) AND IsDeleted = 0;

                -- Purchase
                SELECT ISNULL(SUM(NetAmount), 0) AS TotalAmount, COUNT(DISTINCT PurchaseID) AS OrderCount
                FROM PurchaseItems
                WHERE CreatedAt >= DATEADD(DAY, -@Days, GETDATE()) AND IsDeleted = 0;
            ";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Days", days);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        totalSales = Convert.ToDecimal(reader["TotalAmount"]);
                        salesCount = Convert.ToInt32(reader["OrderCount"]);
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        totalPurchase = Convert.ToDecimal(reader["TotalAmount"]);
                        purchaseCount = Convert.ToInt32(reader["OrderCount"]);
                    }
                }
            }

            // Update UI
            SalesAmountText.Text = "₹" + totalSales.ToString("N0");
            SalesOrderCountText.Text = $"({salesCount} Orders)";

            PurchaseAmountText.Text = "₹" + totalPurchase.ToString("N0");
            PurchaseOrderCountText.Text = $"({purchaseCount} Orders)";

            // Update Chart
            ComparisonChart.Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Sales",
                        Values = new ChartValues<ObservablePoint> { new ObservablePoint(0, (double)totalSales) },
                        DataLabels = true,
                        Fill = Brushes.Green
                    },
                    new ColumnSeries
                    {
                        Title = "Purchase",
                        Values = new ChartValues<ObservablePoint> { new ObservablePoint(1, (double)totalPurchase) },
                        DataLabels = true,
                        Fill = Brushes.Blue
                    }
                };
        }
    }
}
