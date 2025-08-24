using System.Configuration;
using System.Security;
using System.Windows;
using System.Windows.Controls;
namespace Phramacy_Product.Views.Profile
{
    public partial class UserProfile : Page
    {
        public UserProfile()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ProfileTabsListBox.Items.Count > 0)
            {
                ProfileTabsListBox.SelectedIndex = 0;
            }
        }
        private void ProfileTabsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ListBoxItem selectedItem)
            {
                string tabName = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(tabName))
                {
                    ContentHeader.Content = tabName;
                    LoadContent(tabName);
                }
            }
        }

        private void LoadContent(string tabName)
        {
            switch (tabName)
            {
                case "About":
                    TabContentControl.Content = new AboutContent();
                    break;
                case "Documents":
                    TabContentControl.Content = new DocumentContent();
                    break;
                case "Security":
                    TabContentControl.Content = new SecurityContent();
                    break;
                case "Plan":
                    TabContentControl.Content = new PlanContent();
                    break;
                case "Password":
                    TabContentControl.Content = new PasswordContent();
                    break;
                case "Staff":
                    TabContentControl.Content = new StaffContent();
                    break;
                case "Integrations":
                    TabContentControl.Content = new IntegrationsContent();
                    break;
                case "Settings":
                    TabContentControl.Content = new SettingContent();
                    break;
                default:
                    TabContentControl.Content = null;
                    break;
            }
        }
    }
}
