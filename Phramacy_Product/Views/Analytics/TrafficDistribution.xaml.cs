using System;
using System.ComponentModel; // Keep if using for the ViewModel
using System.Runtime.CompilerServices; // Keep if using for the ViewModel
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Configuration; // Required for ConfigurationManager
using System.Data.SqlClient; // Required for database access

namespace Phramacy_Product.Views.Analytics
{
    // Ensure your TrafficDistributionViewModel class is correctly defined as provided earlier,
    // including the LoadPaymentData method.
    public class TrafficDistributionViewModel : INotifyPropertyChanged
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["databaseConnection"].ConnectionString;

        private double _cashPercentage;
        private double _onlinePercentage;
        private double _increaseValue = 10.57;

        public double CashPercentage
        {
            get => _cashPercentage;
            set
            {
                _cashPercentage = value;
                OnPropertyChanged();
            }
        }

        public double OnlinePercentage
        {
            get => _onlinePercentage;
            set
            {
                _onlinePercentage = value;
                OnPropertyChanged();
            }
        }

        public double IncreaseValue
        {
            get => _increaseValue;
            set
            {
                _increaseValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadPaymentData()
        {
            string query = @"SELECT 
                SUM(CASE WHEN PaymentType = 'Cash' THEN 1 ELSE 0 END) AS CashCount,
                SUM(CASE WHEN PaymentType = 'Online' THEN 1 ELSE 0 END) AS OnlineCount,
                COUNT(*) AS TotalSales
            FROM SaleDetails
            WHERE IsDeleted = 0 AND Status = 'Completed';";

            int cashCount = 0;
            int onlineCount = 0;
            int totalSales = 0;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            cashCount = reader.GetInt32(reader.GetOrdinal("CashCount"));
                            onlineCount = reader.GetInt32(reader.GetOrdinal("OnlineCount"));
                            totalSales = reader.GetInt32(reader.GetOrdinal("TotalSales"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database Error: {ex.Message}");
            }

            if (totalSales > 0)
            {
                this.CashPercentage = Math.Round((double)cashCount / totalSales * 100, 2);
                this.OnlinePercentage = Math.Round((double)onlineCount / totalSales * 100, 2);
            }
            else
            {
                this.CashPercentage = 0;
                this.OnlinePercentage = 0;
            }
        }
    }


    public partial class TrafficDistribution : UserControl
    {
        public TrafficDistribution()
        {
            InitializeComponent();
            var viewModel = new TrafficDistributionViewModel();
            viewModel.LoadPaymentData();
            this.DataContext = viewModel;
            Loaded += (s, e) => UpdateChart();
        }

        private void UpdateChart()
        {
            var viewModel = this.DataContext as TrafficDistributionViewModel;
            if (viewModel == null) return;

            var canvas = this.FindName("ChartCanvas") as Canvas;
            if (canvas == null) return;
            canvas.Children.Clear();

            double total = viewModel.CashPercentage + viewModel.OnlinePercentage;
            if (total == 0) return;

            double radius = 125;
            double center = 125;
            double innerRadius = 50; // Radius for the inner circle of the donut
            double labelRadius = (radius + innerRadius) / 2; // Mid-point for labels
            double currentAngle = 0;

            // Define colors
            string onlineColorHex = "#42A5F5"; // Blue for Online
            string cashColorHex = "#66BB6A";   // Green for Cash

            // 1. Draw Online Segment
            double onlineAngle = viewModel.OnlinePercentage / total * 360;
            DrawSegment(canvas, onlineAngle, currentAngle, radius, center, onlineColorHex);
            // Add label for Online
            AddLabel(canvas, "Online", viewModel.OnlinePercentage, currentAngle + (onlineAngle / 2), labelRadius, center, onlineColorHex);
            currentAngle += onlineAngle;

            // 2. Draw Cash Segment
            double cashAngle = viewModel.CashPercentage / total * 360;
            DrawSegment(canvas, cashAngle, currentAngle, radius, center, cashColorHex);
            // Add label for Cash
            AddLabel(canvas, "Cash", viewModel.CashPercentage, currentAngle + (cashAngle / 2), labelRadius, center, cashColorHex);
            // currentAngle += cashAngle; // Not strictly necessary after the last segment

            // Draw Inner Circle for Donut effect
            var innerCircle = new Ellipse
            {
                Width = innerRadius * 2, // Diameter
                Height = innerRadius * 2, // Diameter
                Fill = Brushes.White
            };
            Canvas.SetLeft(innerCircle, center - innerRadius);
            Canvas.SetTop(innerCircle, center - innerRadius);
            canvas.Children.Add(innerCircle);
        }

        private void DrawSegment(Canvas canvas, double angle, double startAngle, double radius, double center, string colorHex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex);

            double startRad = startAngle * Math.PI / 180;
            double endRad = (startAngle + angle) * Math.PI / 180;

            var startPoint = new Point(center + radius * Math.Sin(startRad), center - radius * Math.Cos(startRad));
            var endPoint = new Point(center + radius * Math.Sin(endRad), center - radius * Math.Cos(endRad));

            bool isLargeArc = angle > 180;

            var path = new Path
            {
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = 2
            };

            var pathFigure = new PathFigure
            {
                StartPoint = new Point(center, center),
                IsClosed = true
            };
            pathFigure.Segments.Add(new LineSegment(startPoint, false));
            pathFigure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), angle, isLargeArc, SweepDirection.Clockwise, false));
            pathFigure.Segments.Add(new LineSegment(new Point(center, center), false));

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            path.Data = pathGeometry;

            canvas.Children.Add(path);
        }

        private void AddLabel(Canvas canvas, string name, double percentage, double midAngle, double labelRadius, double center, string colorHex)
        {
            var textBlock = new TextBlock
            {
                Text = $"{name} {percentage:F0}%", 
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White 
            };

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size textSize = textBlock.DesiredSize;
            double angleRad = midAngle * Math.PI / 180;
            double x = center + labelRadius * Math.Sin(angleRad) - (textSize.Width / 2);
            double y = center - labelRadius * Math.Cos(angleRad) - (textSize.Height / 2);

            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            canvas.Children.Add(textBlock);
        }
    }
}