using Phramacy_Product.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Phramacy_Product.Views.Analytics
{
    // Assuming SaleDetail is a class in Phramacy_Product.DataModel
    public class SaleDetail
    {
        public int SaleID { get; set; }
        public DateTime? BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentType { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SalesData
    {
        public string DateLabel { get; set; }
        public double CurrentMonthValue { get; set; }
        public double PreviousMonthValue { get; set; }
    }
    public partial class GrossRevenue : UserControl, INotifyPropertyChanged
    {
        private readonly List<SaleDetail> _allSales;
        private int _selectedMonthIndex;

        public GrossRevenue()
        {
            _allSales = GetMockSalesData();
            InitializeComponent();
            this.DataContext = this;
            if (DateTime.Now.Month - 1 >= 0 && DateTime.Now.Month - 1 < AvailableMonths.Count)
            {
                SelectedMonthIndex = DateTime.Now.Month - 1;
            }
            else
            {
                SelectedMonthIndex = 0;
            }
        }
        private string _previousMonthName;
        public string PreviousMonthName
        {
            get => _previousMonthName;
            set { _previousMonthName = value; OnPropertyChanged(); }
        }
        private double _totalRevenueValue;
        public double TotalRevenueValue
        {
            get => _totalRevenueValue;
            set { _totalRevenueValue = value; OnPropertyChanged(); }
        }

        private double _cashRevenue;
        public double CashRevenue
        {
            get => _cashRevenue;
            set { _cashRevenue = value; OnPropertyChanged(); }
        }

        private double _onlineRevenue;
        public double OnlineRevenue
        {
            get => _onlineRevenue;
            set { _onlineRevenue = value; OnPropertyChanged(); }
        }

        private ObservableCollection<SalesData> _salesChartData = new ObservableCollection<SalesData>();
        public ObservableCollection<SalesData> SalesChartData
        {
            get => _salesChartData;
            set { _salesChartData = value; OnPropertyChanged(); }
        }

        private string _currentMonthName;
        public string CurrentMonthName
        {
            get => _currentMonthName;
            set { _currentMonthName = value; OnPropertyChanged(); }
        }
        public int SelectedMonthIndex
        {
            get => _selectedMonthIndex;
            set
            {
                if (_selectedMonthIndex != value)
                {
                    _selectedMonthIndex = value;
                    OnPropertyChanged();

                    // Set the new property here
                    if (value >= 0 && value < AvailableMonths.Count)
                    {
                        CurrentMonthName = AvailableMonths[value];
                    }

                    UpdateAnalyticsData(_selectedMonthIndex);
                }
            }
        }

        public List<string> AvailableMonths { get; } = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames.Take(12).ToList();

        private void CalculateRevenueMetrics(int month, int year)
        {
            DateTime today = DateTime.Now.Date;
            var monthlySales = _allSales
              .Where(s => !s.IsDeleted)
              .Where(s => s.BillDate.HasValue)
              .Where(s => s.BillDate.Value.Month == month &&
                    s.BillDate.Value.Year == year)
              .Where(s => s.BillDate.Value.Date <= today)
              .ToList();

            TotalRevenueValue = (double)monthlySales.Sum(s => s.TotalAmount);

            CashRevenue = (double)monthlySales
              .Where(s => s.PaymentType.Equals("Cash", StringComparison.OrdinalIgnoreCase))
              .Sum(s => s.TotalAmount);

            OnlineRevenue = (double)monthlySales
              .Where(s => s.PaymentType.Equals("Online", StringComparison.OrdinalIgnoreCase))
              .Sum(s => s.TotalAmount);
        }
        // Inside GrossRevenue.xaml.cs

        // Change the signature to accept the collection to populate
        private void PopulateSalesChart(int currentMonth, int currentYear, int previousMonth, int previousYear, ObservableCollection<SalesData> targetCollection)
        {
            int daysInCurrentMonth = DateTime.DaysInMonth(currentYear, currentMonth);
            int daysInPreviousMonth = DateTime.DaysInMonth(previousYear, previousMonth);
            var chartPoints = new List<SalesData>();
            DateTime today = DateTime.Now.Date;
            bool isCurrentMonthBeingViewed = (currentMonth == today.Month && currentYear == today.Year);

            for (int day = 1; day <= daysInCurrentMonth; day++)
            {
                DateTime currentDayDate = new DateTime(currentYear, currentMonth, day);
                if (isCurrentMonthBeingViewed && currentDayDate.Date > today)
                {
                    break;
                }
                double currentMonthSales = (double)_allSales
                  .Where(s => !s.IsDeleted && s.BillDate.HasValue)
                  .Where(s => s.BillDate.Value.Date == currentDayDate.Date)
                  .Sum(s => s.TotalAmount);
                double previousMonthSales = 0;
                if (day <= daysInPreviousMonth)
                {
                    DateTime previousDayDate = new DateTime(previousYear, previousMonth, day);
                    previousMonthSales = (double)_allSales
                      .Where(s => !s.IsDeleted && s.BillDate.HasValue)
                      .Where(s => s.BillDate.Value.Date == previousDayDate.Date) // Filter by specific day
                                  .Sum(s => s.TotalAmount);
                }
                string dateLabel = day.ToString();
                chartPoints.Add(new SalesData
                {
                    DateLabel = dateLabel,
                    CurrentMonthValue = currentMonthSales,
                    PreviousMonthValue = previousMonthSales
                });
            }

            targetCollection.Clear();
            foreach (var point in chartPoints)
            {
                targetCollection.Add(point);
            }
        }

        private List<SaleDetail> GetMockSalesData()
        {
            var sales = new List<SaleDetail>();
            var rnd = new Random();
            DateTime today = DateTime.Now.Date;

            for (int i = 0; i < 1000; i++)
            {
                DateTime billDate = today.AddDays(-rnd.Next(1, 365));
                sales.Add(new SaleDetail
                {
                    SaleID = i + 1,
                    BillDate = billDate,
                    TotalAmount = (decimal)Math.Round(rnd.Next(10, 500) * rnd.NextDouble(), 2),
                    PaymentType = rnd.Next(0, 2) == 0 ? "Cash" : "Online",
                    IsDeleted = false
                });
            }
            return sales;
        }
        private void UpdateAnalyticsData(int monthIndex)
        {
            // The month to analyze (1-12)
            int currentMonth = monthIndex + 1;
            int currentYear = DateTime.Now.Year;

            // Previous month calculation
            DateTime previousDate = new DateTime(currentYear, currentMonth, 1).AddMonths(-1);
            int previousMonth = previousDate.Month;
            int previousYear = previousDate.Year;

            // Set the property for the dynamic legend
            PreviousMonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(previousMonth);

            CalculateRevenueMetrics(currentMonth, currentYear);
            var newSalesChartData = new ObservableCollection<SalesData>();

            PopulateSalesChart(currentMonth, currentYear, previousMonth, previousYear, newSalesChartData);
            SalesChartData = newSalesChartData;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // This converter seems unused in the final XAML, but keep for completeness
    public class PointToXConverter : IValueConverter
    {
        private const double XStart = 30;
        private const double XRange = 350; // Updated to match other converters (380 - 30)
        private const int TotalPoints = 31; // Adjusted to a typical max days in a month
        private const double Interval = XRange / (TotalPoints - 1);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                double xPos = XStart + (index * Interval);
                return xPos - 10;
            }
            return XStart;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // UPDATED PointCollectionConverter for Tooltip support
    public class PointCollectionConverter : IValueConverter
    {
        private const double YMin = 20;
        private const double YMax = 180;
        private const double YRange = YMax - YMin;
        private const double XStart = 30;
        private const double XEnd = 380;
        private const double XRange = XEnd - XStart;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string target = parameter as string;

            // Case 1: Polyline PointCollection binding (Value is ObservableCollection<SalesData>)
            if (value is ObservableCollection<SalesData> salesDataCollection && salesDataCollection.Any())
            {
                double maxValue = salesDataCollection.Max(d => Math.Max(d.CurrentMonthValue, d.PreviousMonthValue));

                if (maxValue > 0)
                {
                    maxValue *= 1.1;
                }
                else
                {
                    maxValue = 1000;
                }

                int totalPoints = salesDataCollection.Count;
                double xInterval = totalPoints > 1 ? XRange / (totalPoints - 1) : 0;

                if (target == "CurrentMonth" || target == "PreviousMonth")
                {
                    var points = new PointCollection();
                    for (int i = 0; i < totalPoints; i++)
                    {
                        var data = salesDataCollection[i];
                        double pointValue = target == "CurrentMonth" ? data.CurrentMonthValue : data.PreviousMonthValue;

                        double x = XStart + (i * xInterval);
                        double normalizedValue = pointValue / maxValue;
                        double y = YMax - (normalizedValue * YRange);
                        points.Add(new System.Windows.Point(x, y));
                    }
                    return points;
                }
            }

            // Case 2: Individual Point binding for Tooltip (Value is SalesData object)
            if (value is SalesData dataPoint)
            {
                // Need access to the full collection and its index.
                // We rely on the ItemsControl.ItemContainerStyle trick for the index/collection from XAML binding.
                // For the Y-position, we need the max value. Accessing the DataContext is complex here,
                // but since the YAxisLabelConverter already computed it, we'll try to get it.
                // This approach assumes the max value is readily available in a simple way, which it is NOT here.
                // We must fetch the collection from the DataContext if possible, or recalculate the max value.

                // Re-calculate Max Value (inefficient, but necessary for simple binding)
                var collection = (Application.Current.MainWindow.DataContext as GrossRevenue)?.SalesChartData;
                if (collection == null || !collection.Contains(dataPoint)) return Double.NaN;

                double maxValue = collection.Max(d => Math.Max(d.CurrentMonthValue, d.PreviousMonthValue));
                if (maxValue <= 0) maxValue = 1000;
                maxValue *= 1.1; // Pad it 

                int totalPoints = collection.Count;
                int index = collection.IndexOf(dataPoint);
                double xInterval = totalPoints > 1 ? XRange / (totalPoints - 1) : 0;
                double x = XStart + (index * xInterval);

                // X Position (Centered)
                if (target == "XPositionOnly")
                {
                    return x - 5; // -5 to center the 10x10 Ellipse
                }

                // Y Position Current Month (Centered)
                if (target == "YCurrentPositionOnly")
                {
                    double normalizedValue = dataPoint.CurrentMonthValue / maxValue;
                    double y = YMax - (normalizedValue * YRange);
                    return y - 5; // -5 to center the 10x10 Ellipse
                }

                // Y Position Previous Month (Centered)
                if (target == "YPreviousPositionOnly")
                {
                    double normalizedValue = dataPoint.PreviousMonthValue / maxValue;
                    double y = YMax - (normalizedValue * YRange);
                    return y - 5; // -5 to center the 10x10 Ellipse
                }
            }


            return new PointCollection { new System.Windows.Point(XStart, YMax), new System.Windows.Point(XEnd, YMax) };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class YAxisLabelConverter : IValueConverter
    {
        private const double YMin = 20;
        private const double YMax = 180;
        private const double YRange = YMax - YMin;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<SalesData> salesDataCollection && salesDataCollection.Any())
            {
                double maxCurrent = salesDataCollection.Max(d => d.CurrentMonthValue);
                double maxPrevious = salesDataCollection.Max(d => d.PreviousMonthValue);
                double maxValue = Math.Max(maxCurrent, maxPrevious);

                if (maxValue <= 0) maxValue = 1000;
                double paddedMax = maxValue * 1.1;
                double step = 10;
                if (paddedMax > 50) step = 10;
                if (paddedMax > 100) step = 25;
                if (paddedMax > 250) step = 50;
                if (paddedMax > 500) step = 100;
                if (paddedMax > 2000) step = 500;
                if (paddedMax > 5000) step = 1000;

                double roundedMax = Math.Ceiling(paddedMax / step) * step;
                var labels = new List<(string Text, double YPos)>();
                int numLabels = 6;

                for (int i = 0; i < numLabels; i++)
                {
                    double labelValue = roundedMax * (i / (double)(numLabels - 1));
                    double normalizedValue = labelValue / roundedMax;
                    double y = YMax - (normalizedValue * YRange);
                    //string text = labelValue.ToString("N0", culture);
                    string text = "₹" + labelValue.ToString("N0", culture);

                    labels.Add((text, y));
                }

                labels.Reverse();
                return labels;
            }
            return new List<(string Text, double YPos)>
    {
      ("₹0", YMax),
      ("₹1000", YMin)
    };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class AxisPositionConverter : IValueConverter
    {
        private const double XStart = 30;
        private const double XEnd = 380;
        private const double XRange = XEnd - XStart;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = parameter as string;

            if (value is double yPos)
            {
                if (param == "YLabelOffset")
                {
                    return yPos - 5;
                }

                if (param == "YLineOffset")
                {
                    return yPos;
                }
                return yPos;
            }

            return XStart;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // UPDATED IndexToXConverter to accept SalesChartData as a parameter
    public class IndexToXConverter : IValueConverter
    {
        private const double XStart = 30;
        private const double XEnd = 380;
        private const double XRange = XEnd - XStart;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index && parameter is ObservableCollection<SalesData> salesDataCollection)
            {
                int totalPoints = salesDataCollection.Count;
                if (totalPoints == 0) return Double.NaN; // Handle empty collection

                string dateLabel = salesDataCollection[index]?.DateLabel;
                if (string.IsNullOrEmpty(dateLabel) || !int.TryParse(dateLabel, out int dayNumber))
                {
                    return Double.NaN;
                }

                int lastDay = int.Parse(salesDataCollection.LastOrDefault()?.DateLabel ?? "0");

                // Filter logic: Show only labels for specific days
                if (!(dayNumber == 1 || dayNumber == 5 || dayNumber == 10 || dayNumber == 15 || dayNumber == 20 || dayNumber == 25 || dayNumber == lastDay))
                {
                    return Double.NaN; // Hide labels that don't meet the criteria
                }

                // Calculate X position
                double xInterval = totalPoints > 1 ? XRange / (totalPoints - 1) : 0;
                double xPos = XStart + (index * xInterval);

                // Return X position (minus 10 for center alignment)
                return xPos - 10;
            }
            return Double.NaN;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                if (text == "0" || text.StartsWith("₹0"))
                {
                    return Visibility.Hidden;
                }
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class NanToHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return double.IsNaN(d) ? Visibility.Hidden : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}