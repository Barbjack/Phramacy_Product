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

namespace Phramacy_Product.Views.Analytics
{
    public class User : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string ImageUrl { get; set; }
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
                    Name = customer.CustomerName,
                    Role = $"Total Bills: {customer.BillCount}",
                    ImageUrl = null
                });
            }
        }
        private Task<List<ActiveUserData>> GetActiveCustomerDataAsync()
        {   
            return Task.Run(() =>
            {
                string sqlQuery = @"
                    DECLARE @CurrentMonthStart DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);

                    SELECT TOP 7
                        T.CustomerName,
                        T.BillCount,
                        T.LatestBillDate
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
                    WHERE
                        T.BillCount >= 5 
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
                    BillCount = row.Field<int>("BillCount"),
                    LatestBillDate = row.Field<DateTime>("LatestBillDate")
                }).ToList();

                return result;
            });
        }
        private class ActiveUserData
        {
            public string CustomerName { get; set; }
            public int BillCount { get; set; }
            public DateTime LatestBillDate { get; set; }
        }
        private string GetGenericPlaceholderImageUrl()
            {
                // TODO: Return the path to a generic default user image 
                // that you have included in your project resources, e.g.:
                // return "/Assets/default_user.png";

                // For now, returning null/empty string to remove hardcoded images
                return null;
            }
            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }