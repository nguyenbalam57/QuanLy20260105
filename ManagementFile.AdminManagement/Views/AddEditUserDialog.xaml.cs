using ManagementFile.AdminManagement.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ManagementFile.AdminManagement.Views
{
    /// <summary>
    /// Interaction logic for AddEditUserDialog.xaml
    /// </summary>
    public partial class AddEditUserDialog : Window
    {
        public AddEditUserDialog(AddEditUserDialogViewModel viewModel)
        {
            InitializeComponent();
            
            // Set DataContext from DI
            DataContext = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            
            // Handle password box binding for create mode
            PasswordBox.PasswordChanged += OnPasswordChanged;
            ConfirmPasswordBox.PasswordChanged += OnConfirmPasswordChanged;

            // Handle dialog result from ViewModel
            viewModel.CloseRequested += OnCloseRequested;
        }

        private void OnCloseRequested(object sender, bool result)
        {
            DialogResult = result;
            Close();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditUserDialogViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }

        private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditUserDialogViewModel viewModel)
            {
                viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }
    }
}