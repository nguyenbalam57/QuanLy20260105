using ManagementFile.AdminManagement.Services;
using ManagementFile.AdminManagement.ViewModels;
using System.Windows.Controls;

namespace ManagementFile.AdminManagement.Views
{
    /// <summary>
    /// Interaction logic for UserManagementView.xaml
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        public UserManagementView(UserManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
        }
    }
}