using ManagementFile.AdminManagement.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ManagementFile.AdminManagement.Views
{
    /// <summary>
    /// Interaction logic for ChangePasswordDialog.xaml
    /// </summary>
    public partial class ChangePasswordDialog : Window
    {
        public ChangePasswordDialog(ChangePasswordDialogViewModel viewModel)
        {
            InitializeComponent();
            
            // Set DataContext from DI
            DataContext = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            
            // Handle password box binding
            CurrentPasswordBox.PasswordChanged += OnCurrentPasswordChanged;
            NewPasswordBox.PasswordChanged += OnNewPasswordChanged;
            ConfirmPasswordBox.PasswordChanged += OnConfirmPasswordChanged;

            // Handle dialog result from ViewModel
            viewModel.CloseRequested += OnCloseRequested;
        }

        private void OnCloseRequested(object sender, bool result)
        {
            DialogResult = result;
            Close();
        }

        private void OnCurrentPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordDialogViewModel viewModel)
            {
                viewModel.CurrentPassword = CurrentPasswordBox.Password;
            }
        }

        private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordDialogViewModel viewModel)
            {
                viewModel.NewPassword = NewPasswordBox.Password;
            }
        }

        private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordDialogViewModel viewModel)
            {
                viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }
    }
}