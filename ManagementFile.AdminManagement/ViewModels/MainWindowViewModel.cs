using ManagementFile.AdminManagement.Commands;
using ManagementFile.AdminManagement.Services;
using ManagementFile.AdminManagement.Views;
using ManagementFile.Contracts.DTOs.UserManagement;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.AdminManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho Main Window - Dashboard chính của Admin Management
    /// </summary>
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        #region Fields
        private UserDto _currentUser;
        private string _currentTime;
        private string _statusMessage = "Sẵn sàng";
        private string _serverStatus = "🟢 Kết nối";
        private int _totalUsers = 0;
        private int _activeUsers = 0;
        private double _storageUsedGB = 0;
        private bool _isLoading = false;
        #endregion

        public MainWindowViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            
            // Get current user from API service
            _currentUser = _apiService.CurrentUser;
            
            // Initialize commands
            LogoutCommand = new AsyncRelayCommand(ExecuteLogoutAsync);
            SwitchToUsersTabCommand = new RelayCommand(ExecuteSwitchToUsersTab);
            CleanupSystemCommand = new AsyncRelayCommand(ExecuteCleanupSystemAsync);
            CleanupTempFilesCommand = new AsyncRelayCommand(ExecuteCleanupTempFilesAsync);
            BackupDatabaseCommand = new AsyncRelayCommand(ExecuteBackupDatabaseAsync);

            // Initialize current time
            UpdateCurrentTime();
        }

        #region Properties

        public UserDto CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string ServerStatus
        {
            get => _serverStatus;
            set => SetProperty(ref _serverStatus, value);
        }

        public int TotalUsers
        {
            get => _totalUsers;
            set => SetProperty(ref _totalUsers, value);
        }

        public int ActiveUsers
        {
            get => _activeUsers;
            set => SetProperty(ref _activeUsers, value);
        }

        public double StorageUsedGB
        {
            get => _storageUsedGB;
            set => SetProperty(ref _storageUsedGB, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public ICommand LogoutCommand { get; }
        public ICommand SwitchToUsersTabCommand { get; }
        public ICommand CleanupSystemCommand { get; }
        public ICommand CleanupTempFilesCommand { get; }
        public ICommand BackupDatabaseCommand { get; }

        #endregion

        #region Command Implementations

        private async Task ExecuteLogoutAsync()
        {
            try
            {
                StatusMessage = "Đang đăng xuất...";
                
                var result = MessageBox.Show(
                    "Bạn có chắc chắn muốn đăng xuất?",
                    "Xác nhận đăng xuất",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _apiService.LogoutAsync();
                    
                    // Show login window using DI
                    var loginWindow = App.GetRequiredService<LoginView>();
                    loginWindow.Show();

                    // Close current window
                    Application.Current.MainWindow?.Close();
                }
                else
                {
                    StatusMessage = "Sẵn sàng";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi đăng xuất: {ex.Message}";
                MessageBox.Show($"Lỗi khi đăng xuất: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSwitchToUsersTab()
        {
            // This would be handled by the main window's tab control
            StatusMessage = "Chuyển sang quản lý người dùng";
        }

        private async Task ExecuteCleanupSystemAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang dọn dẹp hệ thống...";

                // Simulate cleanup process
                await Task.Delay(2000);

                StatusMessage = "Dọn dẹp hệ thống hoàn thành";
                MessageBox.Show("Dọn dẹp hệ thống hoàn thành!", "Thông báo", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi dọn dẹp: {ex.Message}";
                MessageBox.Show($"Lỗi khi dọn dẹp hệ thống: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteCleanupTempFilesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang dọn dẹp file tạm thời...";

                // Simulate cleanup process
                await Task.Delay(1500);

                StatusMessage = "Dọn dẹp file tạm thời hoàn thành";
                MessageBox.Show("Đã dọn dẹp file tạm thời thành công!", "Thông báo", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi dọn dẹp: {ex.Message}";
                MessageBox.Show($"Lỗi khi dọn dẹp file tạm thời: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteBackupDatabaseAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang sao lưu cơ sở dữ liệu...";

                // Simulate backup process
                await Task.Delay(3000);

                StatusMessage = "Sao lưu cơ sở dữ liệu hoàn thành";
                MessageBox.Show("Sao lưu cơ sở dữ liệu thành công!", "Thông báo", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi sao lưu: {ex.Message}";
                MessageBox.Show($"Lỗi khi sao lưu cơ sở dữ liệu: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Public Methods

        public void UpdateCurrentTime()
        {
            CurrentTime = DateTime.Now.ToString("HH:mm:ss - dd/MM/yyyy");
        }

        public async Task LoadInitialDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang tải dữ liệu dashboard...";

                // Load user statistics (mock data for now)
                await Task.Delay(1000);
                
                TotalUsers = 25;
                ActiveUsers = 18;
                StorageUsedGB = 2.5;

                // Check server connection
                try
                {
                    var currentUser = await _apiService.GetCurrentUserAsync();
                    if (currentUser != null)
                    {
                        CurrentUser = currentUser;
                        ServerStatus = "🟢 Kết nối";
                    }
                    else
                    {
                        ServerStatus = "🟡 Cảnh báo";
                    }
                }
                catch
                {
                    ServerStatus = "🔴 Mất kết nối";
                }

                StatusMessage = "Dashboard đã sẵn sàng";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}