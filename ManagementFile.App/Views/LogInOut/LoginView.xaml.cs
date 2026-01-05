using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.LogInOut;
using ManagementFile.App.ViewModels.Project;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ManagementFile.App.Views.LogInOut
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;
        private readonly UserManagementService _userService;
        private bool _isPasswordVisible = false;

        public LoginView(
            LoginViewModel loginViewModel,
            UserManagementService userManagementService)
        {
            InitializeComponent();
            
            _viewModel = loginViewModel ?? throw new ArgumentNullException(nameof(loginViewModel));
            _userService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));

            DataContext = _viewModel;

            // Subscribe to login success event
            _viewModel.LoginSuccess += OnLoginSuccess;

            // Focus on username field
            Loaded += (s, e) => UsernameTextBox.Focus();
        }

        #region Event Handlers

        /// <summary>
        /// Xử lý khi login thành công
        /// </summary>
        private void OnLoginSuccess(object sender, LoginSuccessEventArgs e)
        {
            try
            {
                {
                    // Lấy LoginWindow hiện tại
                    Window loginWindow = null;
                    foreach (Window window in System.Windows.Application.Current.Windows)
                    {
                        if (window is Views.LogInOut.LoginView)
                        {
                            loginWindow = window;
                            break;
                        }
                    }

                    // QUAN TRỌNG: Set ShutdownMode trước khi đóng/mở window
                    System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    // Tạo MainWindow mới
                    var mainWindow = App.GetRequiredService<MainWindow>();
                    mainWindow.Show();

                    // Set MainWindow làm window chính
                    System.Windows.Application.Current.MainWindow = mainWindow;

                    // Đặt lại ShutdownMode SAU KHI đã set MainWindow mới
                    System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }

                // Close login window
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening application: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);

                // Đảm bảo ShutdownMode được khôi phục
                System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
        }

        /// <summary>
        /// Toggle password visibility
        /// </summary>
        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            TogglePasswordVisibility();
        }

        /// <summary>
        /// Legacy MouseDown support
        /// </summary>
        private void TogglePasswordVisibility_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TogglePasswordVisibility();
        }

        private void TogglePasswordVisibility()
        {
            _isPasswordVisible = !_isPasswordVisible;

            // Create fade animation
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(150)
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(150)
            };

            // Create rotation animation for icon
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            if (_isPasswordVisible)
            {
                // Animate icon rotation
                IconRotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

                // Fade out PasswordBox
                fadeOut.Completed += (s, e) =>
                {
                    VisiblePasswordTextBox.Text = PasswordBox.Password;
                    PasswordBox.Visibility = Visibility.Collapsed;
                    VisiblePasswordTextBox.Visibility = Visibility.Visible;
                    VisiblePasswordTextBox.BeginAnimation(OpacityProperty, fadeIn);
                    VisiblePasswordTextBox.Focus();
                    VisiblePasswordTextBox.SelectAll();
                };
                PasswordBox.BeginAnimation(OpacityProperty, fadeOut);

                // Update icon
                TogglePasswordIcon.Text = "🙈";
                TogglePasswordIcon.Foreground = (SolidColorBrush)FindResource("PrimaryBlue");
                TogglePasswordButton.ToolTip = "Ẩn mật khẩu";
            }
            else
            {
                // Animate icon rotation
                IconRotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

                // Fade out TextBox
                fadeOut.Completed += (s, e) =>
                {
                    PasswordBox.Password = VisiblePasswordTextBox.Text;
                    VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
                    PasswordBox.Visibility = Visibility.Visible;
                    PasswordBox.BeginAnimation(OpacityProperty, fadeIn);
                    PasswordBox.Focus();
                };
                VisiblePasswordTextBox.BeginAnimation(OpacityProperty, fadeOut);

                // Update icon
                TogglePasswordIcon.Text = "👁️";
                TogglePasswordIcon.Foreground = (SolidColorBrush)FindResource("SoftGray");
                TogglePasswordButton.ToolTip = "Hiển thị mật khẩu";
            }
        }

        /// <summary>
        /// Sync password when PasswordBox changes
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordVisible)
            {
                // Update ViewModel binding if needed
                var viewModel = DataContext as LoginViewModel;
                if (viewModel != null)
                {
                    viewModel.Password = PasswordBox.Password;
                }
            }
        }

        /// <summary>
        /// Sync password when visible TextBox changes
        /// </summary>
        private void VisiblePasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                // Update ViewModel binding
                var viewModel = DataContext as LoginViewModel;
                if (viewModel != null)
                {
                    viewModel.Password = VisiblePasswordTextBox.Text;
                }
            }
        }

        /// <summary>
        /// Handle Enter key to login
        /// </summary>
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var viewModel = DataContext as LoginViewModel;
                if (viewModel?.LoginCommand?.CanExecute(null) == true)
                {
                    viewModel.LoginCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Xử lý khi nhấn nút đăng nhập
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.LoginCommand.CanExecute(null))
            {
                _viewModel.LoginCommand.Execute(null);
            }
        }

        /// <summary>
        /// Xử lý khi nhấn nút thoát
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Xử lý khi nhấn nút kiểm tra kết nối
        /// </summary>
        private void CheckConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CheckConnectionCommand.CanExecute(null))
            {
                _viewModel.CheckConnectionCommand.Execute(null);
            }
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            _viewModel.LoginSuccess -= OnLoginSuccess;
            
            // Dispose viewmodel
            _viewModel?.Dispose();
            
            base.OnClosed(e);
        }

        #endregion

    }
}
