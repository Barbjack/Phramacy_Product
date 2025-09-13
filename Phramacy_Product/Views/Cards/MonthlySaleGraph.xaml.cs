using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows.Controls;

namespace Phramacy_Product.Views.Cards
{
    public partial class MonthlySaleGraph : UserControl
    {
        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public MonthlySaleGraph()
        {
            InitializeComponent();
            LoadMonthlySalesData();
            this.DataContext = this;
        }

        private void LoadMonthlySalesData()
        {
            var monthlySales = new Dictionary<int, double>();
            for (int i = 1; i <= 12; i++)
            {
                monthlySales.Add(i, 0);
            }

            string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;
            string query = @"
                SELECT 
                    MONTH(CreatedAt) as SalesMonth,
                    SUM(NetAmount) as TotalSales 
                FROM 
                    [ReactDB].[dbo].[SaleItems] 
                WHERE 
                    YEAR(CreatedAt) = YEAR(GETDATE())
                GROUP BY 
                    MONTH(CreatedAt) 
                ORDER BY 
                    SalesMonth;
            ";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        int month = reader.GetInt32(0);
                       // double totalSales = reader.GetDouble(1);
                        double totalSales = Convert.ToDouble(reader["TotalSales"]);
                        monthlySales[month] = totalSales;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            var salesValues = new ChartValues<double>();
            for (int i = 1; i <= 12; i++)
            {
                salesValues.Add(monthlySales[i]);
            }

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Sales",
                    Values = salesValues
                }
            };

            Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var indiaCulture = new CultureInfo("en-IN");
            YFormatter = value => value.ToString("C2", indiaCulture);
        }
    }
}