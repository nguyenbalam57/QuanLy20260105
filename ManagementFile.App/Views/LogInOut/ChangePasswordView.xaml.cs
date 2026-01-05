using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.LogInOut;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ManagementFile.App.Views.LogInOut
{
    /// <summary>
    /// Interaction logic for ChangePasswordView.xaml
    /// </summary>
    public partial class ChangePasswordView : Window
    {
        private readonly ChangePasswordViewModel _viewModel;

        public ChangePasswordView(ChangePasswordViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            // Subscribe to close event
            _viewModel.CloseRequested += OnCloseRequested;
        }

        private void OnCloseRequested(bool dialogResult)
        {
            this.DialogResult = dialogResult;
            this.Close();
        }

        // PasswordBox binding workaround
        private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.CurrentPassword = CurrentPasswordBox.Password;
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.NewPassword = NewPasswordBox.Password;
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
            base.OnClosed(e);
        }
    }
}
