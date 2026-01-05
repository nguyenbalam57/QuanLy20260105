using ManagementFile.AdminManagement.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ManagementFile.AdminManagement.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            
            // Set DataContext from DI
            DataContext = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            
            // Set focus to username textbox when loaded
            Loaded += (s, e) => UsernameTextBox.Focus();

            // Handle password box binding
            PasswordBox.PasswordChanged += OnPasswordChanged;
            
            // Handle enter key press
            KeyDown += OnKeyDown;
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is LoginViewModel viewModel && viewModel.CanLogin)
                {
                    viewModel.LoginCommand.Execute(null);
                }
            }
        }
    }
}