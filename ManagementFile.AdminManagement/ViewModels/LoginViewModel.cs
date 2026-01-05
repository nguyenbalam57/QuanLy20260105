using ManagementFile.AdminManagement.Commands;
using ManagementFile.AdminManagement.Services;
using ManagementFile.AdminManagement.ViewModels;
using ManagementFile.AdminManagement.Views;
using ManagementFile.Contracts.Requests.UserManagement;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.AdminManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho Login View
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private string _usernameOrEmail = "";
        private string _password = "";
        private bool _rememberMe = false;
        private bool _isLoading = false;
        private string _errorMessage = "";
        private string _statusMessage = "";

        public LoginViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            // Initialize commands
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
            ClearErrorCommand = new RelayCommand(ExecuteClearError);
            ExitCommand = new RelayCommand(ExecuteExit);

            // Set default values for testing
            UsernameOrEmail = "admin";
            StatusMessage = "Nhập thông tin đăng nhập để tiếp tục";
        }

        #region Properties

        /// <summary>
        /// Tên đăng nhập hoặc email
        /// </summary>
        public string UsernameOrEmail
        {
            get => _usernameOrEmail;
            set
            {
                if (SetProperty(ref _usernameOrEmail, value))
                {
                    ClearError();
                    OnPropertyChanged(nameof(CanLogin));
                }
            }
        }

        /// <summary>
        /// Mật khẩu
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ClearError();
                    OnPropertyChanged(nameof(CanLogin));
                }
            }
        }

        /// <summary>
        /// Ghi nhớ đăng nhập
        /// </summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        /// <summary>
        /// Đang loading
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanLogin));
                    OnPropertyChanged(nameof(IsNotLoading));
                }
            }
        }

        /// <summary>
        /// Không đang loading
        /// </summary>
        public bool IsNotLoading => !IsLoading;

        /// <summary>
        /// Thông báo lỗi
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// Thông báo trạng thái
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Có lỗi hay không
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Có thể đăng nhập hay không
        /// </summary>
        public bool CanLogin => !IsLoading && 
                               !string.IsNullOrWhiteSpace(UsernameOrEmail) && 
                               !string.IsNullOrWhiteSpace(Password);

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand ClearErrorCommand { get; }
        public ICommand ExitCommand { get; }

        #endregion

        #region Command Implementations

        private async Task ExecuteLoginAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang đăng nhập...";
                ClearError();

                var response = await _apiService.LoginAsync(UsernameOrEmail, Password, RememberMe);

                if (response.Success)
                {
                    StatusMessage = "Đăng nhập thành công!";
                    
                    // Close login window and open main admin window using DI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = App.GetRequiredService<MainWindow>();
                        mainWindow.Show();

                        // Close login window
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is LoginView)
                            {
                                window.Close();
                                break;
                            }
                        }
                    });
                }
                else
                {
                    ErrorMessage = response.Message;
                    StatusMessage = "Đăng nhập thất bại";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối: {ex.Message}";
                StatusMessage = "Không thể kết nối đến server";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteLogin() => CanLogin;

        private void ExecuteClearError()
        {
            ErrorMessage = "";
        }

        private void ExecuteExit()
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Helper Methods

        private void ClearError()
        {
            ErrorMessage = "";
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs> LoginSuccess;

        protected virtual void OnLoginSuccess()
        {
            LoginSuccess?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}