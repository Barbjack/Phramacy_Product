using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace Phramacy_Product.Views.Analytics
{
    public class User
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string ImageUrl { get; set; }
    }
    public partial class ActiveUsers : UserControl, INotifyPropertyChanged
    {
        public ActiveUsers()
        {
            Users = new ObservableCollection<User>
        {
           new User { Name = "Arjun Sharma", Role = "Admin", ImageUrl = "/Assets/ProfilePictures/image_5.png" },
           new User { Name = "Ravi Kumar", Role = "Moderator", ImageUrl = "/Assets/ProfilePictures/image_2.png" },
           new User { Name = "Sanjay Patel", Role = "Editor", ImageUrl = "/Assets/ProfilePictures/image_6.png" },
           new User { Name = "Vikram Singh", Role = "Admin", ImageUrl = "/Assets/ProfilePictures/image_4.png" },
           new User { Name = "Ajay Gupta", Role = "Editor", ImageUrl = "/Assets/ProfilePictures/image_5.png" },
           new User { Name = "Ganesh Iyer", Role = "Editor", ImageUrl = "/Assets/ProfilePictures/image_6.png" },
           new User { Name = "Rahul Kapoor", Role = "Editor", ImageUrl = "/Assets/ProfilePictures/image_2.png" }  };
            DataContext = this;
            InitializeComponent();
        }

        private ObservableCollection<User> _users;

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}