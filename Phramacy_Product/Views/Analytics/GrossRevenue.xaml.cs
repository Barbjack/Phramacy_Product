using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Phramacy_Product.Views.Analytics
{
    public class SalesData
    {
        public string Date { get; set; }
        public double Value { get; set; }
    }

    public partial class GrossRevenue : UserControl, INotifyPropertyChanged
    {
        public GrossRevenue()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public double GrossRevenueValue { get; } = 165.50; 
        public double PointOfSaleRevenue { get; } = 791.64;
        public double OnlineStoreRevenue { get; } = 113.86;
        public double OtherOnlineStoreRevenue { get; } = 0.00;
        public List<SalesData> JanuarySales { get; } = new List<SalesData>
        {
            new SalesData { Date = "Jan 1", Value = 60 },
            new SalesData { Date = "Jan 7", Value = 65 },
            new SalesData { Date = "Jan 13", Value = 80 },
            new SalesData { Date = "Jan 19", Value = 125 },
            new SalesData { Date = "Jan 25", Value = 30 },
            new SalesData { Date = "Jan 31", Value = 40 }
        };

        public List<SalesData> DecemberSales { get; } = new List<SalesData>
        {
            new SalesData { Date = "Jan 1", Value = 40 },
            new SalesData { Date = "Jan 7", Value = 20 },
            new SalesData { Date = "Jan 13", Value = 60 },
            new SalesData { Date = "Jan 19", Value = 120 },
            new SalesData { Date = "Jan 25", Value = 60 },
            new SalesData { Date = "Jan 31", Value = 30 }
        };
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}