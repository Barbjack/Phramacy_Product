using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace Phramacy_Product.Views.Analytics
{
    public class TrafficDistributionViewModel : INotifyPropertyChanged
    {
        private double _organicPercentage = 50.0;
        private double _directPercentage = 20.0;
        private double _paidPercentage = 30.0;
        private double _increaseValue = 10.57;
        public double OrganicPercentage
        {
            get => _organicPercentage;
            set
            {
                _organicPercentage = value;
                OnPropertyChanged();
            }
        }
        public double DirectPercentage
        {
            get => _directPercentage;
            set
            {
                _directPercentage = value;
                OnPropertyChanged();
            }
        }
        public double PaidPercentage
        {
            get => _paidPercentage;
            set
            {
                _paidPercentage = value;
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
    }
    public partial class TrafficDistribution : UserControl
    {
        public TrafficDistribution()
        {
            InitializeComponent();
            var viewModel = new TrafficDistributionViewModel();
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

            double total = viewModel.OrganicPercentage + viewModel.DirectPercentage + viewModel.PaidPercentage;
            if (total == 0) return;

            double radius = 125;
            double center = 125;
            double currentAngle = 0;  
            DrawSegment(canvas, viewModel.PaidPercentage / total * 360, currentAngle, radius, center, "#42A5F5");
            currentAngle += viewModel.PaidPercentage / total * 360;
            DrawSegment(canvas, viewModel.DirectPercentage / total * 360, currentAngle, radius, center, "#66BB6A");
            currentAngle += viewModel.DirectPercentage / total * 360;
            DrawSegment(canvas, viewModel.OrganicPercentage / total * 360, currentAngle, radius, center, "#E57373");
            var innerCircle = new Ellipse
            {
                Width = 100,
                Height = 100,
                Fill = Brushes.White
            };
            Canvas.SetLeft(innerCircle, center - 50);
            Canvas.SetTop(innerCircle, center - 50);
            canvas.Children.Add(innerCircle);

            AddLabel(canvas, "Paid", viewModel.PaidPercentage, 150, 175, "white");
            AddLabel(canvas, "Direct", viewModel.DirectPercentage, 170, 65, "white");
            AddLabel(canvas, "Organic", viewModel.OrganicPercentage, 30, 120, "white");
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

        private void AddLabel(Canvas canvas, string name, double percentage, double left, double top, string colorHex)
        {
            var textBlock = new TextBlock
            {
                Text = $"{percentage}%",
                FontWeight = FontWeights.Bold,
                FontSize = 18,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex)
            };
            Canvas.SetLeft(textBlock, left);
            Canvas.SetTop(textBlock, top);
            canvas.Children.Add(textBlock);
        }
    }
}