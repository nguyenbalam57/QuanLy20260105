using ManagementFile.App.Models;
using ManagementFile.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ManagementFile.App.ViewModels;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.App.Views;

namespace ManagementFile.App.ViewModels.LogInOut
{
    /// <summary>
    /// ViewModel cho Login View
    /// </summary>
    public class LoginViewModel : BaseViewModel, IDisposable
    {
        private readonly ApiService _apiService;
        private readonly UserManagementService _userService;
        private string _usernameOrEmail = "";
        private string _password = "";
        private bool _rememberMe = false;
        private bool _isLoading = false;
        private string _errorMessage = "";
        private string _statusMessage = "";

        public LoginViewModel(
            ApiService apiService,
            UserManagementService userManagementService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _userService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));

            // Initialize commands
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
            ClearErrorCommand = new RelayCommand(ExecuteClearError);
            CheckConnectionCommand = new AsyncRelayCommand(ExecuteCheckConnectionAsync);
            ExitCommand = new RelayCommand(ExecuteExit);

            // Set default values for testing
            UsernameOrEmail = "";
            StatusMessage = "Nhập thông tin đăng nhập để tiếp tục";

            // Check server connection on startup
            _ = CheckServerConnectionAsync();
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
        /// Có lỗi không
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Thông báo trạng thái
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Có thể đăng nhập không
        /// </summary>
        public bool CanLogin => !IsLoading &&
                               !string.IsNullOrWhiteSpace(UsernameOrEmail) &&
                               !string.IsNullOrWhiteSpace(Password);

        public string AppVersion => App.GetVersionApp();

        #endregion

        #region Commands

        /// <summary>
        /// Command đăng nhập
        /// </summary>
        public ICommand LoginCommand { get; }

        /// <summary>
        /// Command xóa thông báo lỗi
        /// </summary>
        public ICommand ClearErrorCommand { get; }

        /// <summary>
        /// Command kiểm tra kết nối server
        /// </summary>
        public ICommand CheckConnectionCommand { get; }

        /// <summary>
        /// Command thoát ứng dụng
        /// </summary>
        public ICommand ExitCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// Event khi đăng nhập thành công
        /// </summary>
        public event System.EventHandler<LoginSuccessEventArgs> LoginSuccess;

        #endregion

        #region Command Implementations

        /// <summary>
        /// Thực hiện đăng nhập
        /// </summary>
        private async Task ExecuteLoginAsync()
        {
            if (!CanLogin)
                return;

            try
            {
                IsLoading = true;
                ClearError();
                StatusMessage = "Đang đăng nhập...";

                // Sử dụng UserManagementService thay vì gọi trực tiếp ApiService
                var result = await _userService.LoginAsync(UsernameOrEmail, Password, RememberMe);

                if (result != null)
                {
                    StatusMessage = "Đăng nhập thành công!";

                    // UserManagementService đã tự động set current user
                    // Raise login success event với thông tin từ UserManagementService
                    LoginSuccess?.Invoke(this, new LoginSuccessEventArgs
                    {
                        User = _userService.CurrentUser,
                        SessionToken = _userService.SessionToken,
                        ExpiresAt = _userService.SessionExpiresAt
                    });
                    
                }
                else
                {
                    ErrorMessage = "Thông tin đăng nhập không chính xác";
                    StatusMessage = "Đăng nhập thất bại";
                }
            }
            catch (System.IO.FileLoadException ex) when (ex.Message.Contains("Newtonsoft.Json"))
            {
                ErrorMessage = $"🔧 Lỗi Assembly Newtonsoft.Json\n\n" +
                              $"Chi tiết: {ex.Message}\n\n" +
                              $"Giải pháp:\n" +
                              $"✅ Assembly resolve handler đang hoạt động\n" +
                              $"✅ Binding redirect đã được fix: 13.0.0.0\n" +
                              $"⚠️ Nếu vẫn lỗi, restart ứng dụng\n\n" +
                              $"Debug info sẽ xuất hiện trong Output window";
                StatusMessage = "Lỗi Assembly - Đang tự động khắc phục";
            }
            catch (System.IO.FileNotFoundException ex) when (ex.Message.Contains("Newtonsoft.Json"))
            {
                ErrorMessage = $"🔍 Không tìm thấy Newtonsoft.Json DLL\n\n" +
                              $"Chi tiết: {ex.Message}\n\n" +
                              $"Trạng thái:\n" +
                              $"📂 Assembly resolve handler: HOẠT ĐỘNG\n" +
                              $"🔄 Đang tìm DLL từ packages folder\n" +
                              $"📋 Sẽ tự động copy vào app directory\n\n" +
                              $"Vui lòng thử lại trong giây lát...";
                StatusMessage = "Đang tìm và copy Newtonsoft.Json DLL";
            }
            catch (System.TypeLoadException ex) when (ex.Message.Contains("Newtonsoft.Json"))
            {
                ErrorMessage = $"⚡ Lỗi Load Type Newtonsoft.Json\n\n" +
                              $"Chi tiết: {ex.Message}\n\n" +
                              $"Version hiện tại: 13.0.0.0 (Đã fix)\n" +
                              $"Binding redirect: 13.0.0.0 → 13.0.0.0 ✅\n\n" +
                              $"Restart ứng dụng để apply changes";
                StatusMessage = "Cần restart ứng dụng";
            }
            catch (System.BadImageFormatException ex) when (ex.Message.Contains("Newtonsoft.Json"))
            {
                ErrorMessage = $"🖼️ Lỗi Bad Image Format\n\n" +
                              $"Chi tiết: {ex.Message}\n\n" +
                              $"Nguyên nhân có thể:\n" +
                              $"• DLL bị corrupt\n" +
                              $"• Platform mismatch (x86/x64)\n" +
                              $"• .NET Framework version\n\n" +
                              $"Assembly resolver sẽ thử copy DLL mới";
                StatusMessage = "Lỗi DLL format - Đang khắc phục";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi đăng nhập: {ex.Message}";
                StatusMessage = "Lỗi khi đăng nhập";

                // Đảm bảo ShutdownMode được khôi phục
                System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Kiểm tra có thể đăng nhập không
        /// </summary>
        private bool CanExecuteLogin()
        {
            return CanLogin;
        }

        /// <summary>
        /// Xóa thông báo lỗi
        /// </summary>
        private void ExecuteClearError()
        {
            ClearError();
        }

        /// <summary>
        /// Kiểm tra kết nối server
        /// </summary>
        private async Task ExecuteCheckConnectionAsync()
        {
            try
            {
                StatusMessage = "Đang kiểm tra kết nối server...";
                var isConnected = await _apiService.CheckServerConnectionAsync();

                StatusMessage = isConnected
                    ? "Kết nối server thành công"
                    : "Không thể kết nối đến server";

                if (!isConnected)
                {
                    ErrorMessage = "Không thể kết nối đến server. Vui lòng kiểm tra lại.";
                }
                else
                {
                    ClearError();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi khi kiểm tra kết nối";
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Thoát ứng dụng
        /// </summary>
        private void ExecuteExit()
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Xóa thông báo lỗi
        /// </summary>
        private void ClearError()
        {
            ErrorMessage = "";
        }

        /// <summary>
        /// Kiểm tra kết nối server async
        /// </summary>
        private async Task CheckServerConnectionAsync()
        {
            try
            {
                var isConnected = await _apiService.CheckServerConnectionAsync();
                if (!isConnected)
                {
                    StatusMessage = "Cảnh báo: Không thể kết nối đến server";
                }
            }
            catch
            {
                // Ignore connection check errors on startup
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //_apiService?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// Event args cho login success
    /// </summary>
    public class LoginSuccessEventArgs : EventArgs
    {
        public UserDto User { get; set; }
        public string SessionToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}
