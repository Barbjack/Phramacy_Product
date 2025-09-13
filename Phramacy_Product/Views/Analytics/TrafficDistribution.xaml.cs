using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Phramacy_Product.Views.Analytics
{
    // FIX: This class should be a separate ViewModel class, not inside the UserControl code-behind
    // This allows for cleaner separation of concerns (MVVM pattern).
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
           
            this.DataContext = new TrafficDistributionViewModel();
        }
    }
}