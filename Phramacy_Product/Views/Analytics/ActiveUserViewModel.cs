using Phramacy_Product.DataModel;
using Phramacy_Product.Views.DBMaster;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Windows.Media; // Correct WPF namespace is already imported

namespace Phramacy_Product.Views.Analytics
{
    public class User : INotifyPropertyChanged
    {
        public string CustomerName { get; set; }
        public string MobileNumber { get; set; }
        public int noOfbills { get; set; }

        public string Initial
        {
            get => string.IsNullOrEmpty(CustomerName) ? "" : CustomerName.Substring(0, 1).ToUpper();
        }
        private static readonly string[] AvatarColorPalette = new string[]
        {
            "#4CAF50", "#2196F3", "#FF9800", "#9C27B0", "#E91E63",
            "#00BCD4", "#8BC34A", "#FF5722", "#607D8B", "#FBC02D"
        };

        /// <summary>
        /// Fixed: Changed the property type from System.Drawing.Brush to the correct WPF type, Brush (System.Windows.Media.Brush).
        /// </summary>
        public Brush AvatarColor
        {
            get
            {
                if (string.IsNullOrEmpty(CustomerName))
                    // These classes (SolidColorBrush, Color.FromRgb) are correct for WPF
                    return new SolidColorBrush(Color.FromRgb(170, 170, 170));
                int hash = CustomerName.GetHashCode();
                int index = Math.Abs(hash) % AvatarColorPalette.Length;
                // BrushConverter is also a WPF class, and this conversion is correct.
                return (Brush)new BrushConverter().ConvertFromString(AvatarColorPalette[index]);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class ActiveUsersViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<User> Users { get; set; }
        public ActiveUsersViewModel()
        {
            Users = new ObservableCollection<User>();
            LoadActiveUsers();
        }

        private async void LoadActiveUsers()
        {
            var activeCustomersData = await GetActiveCustomerDataAsync();
            Users.Clear();
            foreach (var customer in activeCustomersData)
            {
                Users.Add(new User
                {
                    CustomerName = customer.CustomerName,
                    MobileNumber = customer.Mobile,
                    noOfbills = customer.BillCount,

                });
            }
        }
        private Task<List<ActiveUserData>> GetActiveCustomerDataAsync()
        {
            return Task.Run(() =>
            {
                // SQL query to find active customers and retrieve their mobile number from PharmaCustomers.
                string sqlQuery = @"
            DECLARE @CurrentMonthStart DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);

            SELECT TOP 7
                T.CustomerName,
                T.BillCount,
                T.LatestBillDate,
                PC.Mobile
            FROM
                (
                    SELECT
                        [CustomerName],
                        COUNT([SaleID]) AS BillCount,
                        MAX([BillDate]) AS LatestBillDate,
                        MAX(CASE
                                WHEN [BillDate] >= @CurrentMonthStart
                                THEN 1
                                ELSE 0
                            END) AS HasCurrentMonthBill
                    FROM
                        [ReactDB].[dbo].[SaleDetails]
                    WHERE
                        [CustomerName] IS NOT NULL
                    GROUP BY
                        [CustomerName]
                ) AS T
            INNER JOIN
                [ReactDB].[dbo].[PharmaCustomers] PC
                ON T.CustomerName = PC.CustomerName
            WHERE
                T.BillCount >= 5
                -- And who have made a purchase in the current month
                AND T.HasCurrentMonthBill = 1
            ORDER BY
                T.BillCount DESC,
                T.LatestBillDate DESC;
        ";

                DataTable dt = DBMasterConnection.GD(sqlQuery);

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new List<ActiveUserData>();
                }

                var result = dt.AsEnumerable().Select(row => new ActiveUserData
                {
                    CustomerName = row.Field<string>("CustomerName"),
                    Mobile = row.Field<string>("Mobile"),
                    BillCount = row.Field<int>("BillCount"),
                    LatestBillDate = row.Field<DateTime>("LatestBillDate")
                }).ToList();

                return result;
            });
        }

        private class ActiveUserData
        {
            public string CustomerName { get; set; }
            public string Mobile { get; set; }
            public int BillCount { get; set; }
            public DateTime LatestBillDate { get; set; }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

