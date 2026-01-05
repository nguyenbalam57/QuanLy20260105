using ManagementFile.App.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels
{
    /// <summary>
    /// ViewModel chính cho MainWindow - điều phối tất cả các phases
    /// Quản lý navigation, user session, và integration giữa các components
    /// </summary>
    public class MainWindowViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly ServiceManager _serviceManager;
        private readonly NavigationService _navigationService;
        private readonly DataCache _dataCache;
        private readonly EventBus _eventBus;
        private readonly UserManagementService _userService;
        
        private string _currentUserName = "";
        private string _currentUserRole = "";
        private bool _isLoading = false;
        private string _loadingMessage = "";
        private string _statusMessage = "";
        private int _selectedTabIndex = 0;
        private string _currentTime = "";
        private bool _isAdminMode = false;
        private string _avatar = "";
        
        private System.Threading.Timer _timeUpdateTimer;
        private System.Threading.Timer _dataRefreshTimer;
        #endregion

        #region Constructor
        public MainWindowViewModel(
            ServiceManager serviceManager,
            NavigationService navigationService,
            DataCache dataCache,
            EventBus eventBus,
            UserManagementService userManagementService)
        {
            // Khởi tạo services
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _userService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));

            // Initialize properties
            InitializeProperties();
            
            // Initialize commands
            InitializeCommands();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Start initialization
            InitializeAsync();
        }
        #endregion

        #region Public Properties

        public string Avatar
        {
            get
            {
                if (string.IsNullOrEmpty(CurrentUserName))
                    return "?";

                var names = CurrentUserName.Split(' ');
                if (names.Length >= 2)
                    return $"{names[0][0]}{names[names.Length - 1][0]}";

                return names[0].Length > 0 ? names[0][0].ToString() : "?";
            }
        }

        /// <summary>
        /// Tên user hiện tại
        /// </summary>
        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        /// <summary>
        /// Role của user hiện tại
        /// </summary>
        public string CurrentUserRole
        {
            get => _currentUserRole;
            set => SetProperty(ref _currentUserRole, value);
        }

        /// <summary>
        /// Trạng thái loading
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Message hiển thị khi loading
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// Status message trong status bar
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Index của tab được chọn
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    OnTabChanged();
                }
            }
        }

        /// <summary>
        /// Thời gian hiện tại
        /// </summary>
        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        /// <summary>
        /// Chế độ Admin có được bật không
        /// </summary>
        public bool IsAdminMode
        {
            get => _isAdminMode;
            set => SetProperty(ref _isAdminMode, value);
        }

        /// <summary>
        /// Hiển thị thông tin user đầy đủ
        /// </summary>
        public string UserDisplayText => !string.IsNullOrEmpty(CurrentUserName) 
            ? $"{CurrentUserName} ({CurrentUserRole})" 
            : "Chưa đăng nhập";

        /// <summary>
        /// Có user đăng nhập không
        /// </summary>
        public bool HasCurrentUser => !string.IsNullOrEmpty(CurrentUserName);

        /// <summary>
        /// Window title động
        /// </summary>
        public string WindowTitle => HasCurrentUser 
            ? $"ManagementFile - {CurrentUserName}" 
            : "ManagementFile - Enterprise Platform";

        #endregion

        #region Commands

        public ICommand NavigateToDashboardCommand { get; private set; }
        public ICommand NavigateToFilesCommand { get; private set; }
        public ICommand NavigateToUsersCommand { get; private set; }
        public ICommand NavigateToProjectsCommand { get; private set; }
        public ICommand NavigateToClientCommand { get; private set; }
        public ICommand NavigateToMyWorkspaceCommand { get; private set; }
        public ICommand NavigateToCollaborationCommand { get; private set; }
        public ICommand NavigateToNotificationsCommand { get; private set; }
        public ICommand NavigateToReportsCommand { get; private set; }
        public ICommand NavigateToAdminCommand { get; private set; }
        public ICommand NavigateToProductionCommand { get; private set; }
        public ICommand NavigateBackCommand { get; private set; }
        public ICommand RefreshDataCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
        public ICommand SwitchModeCommand { get; private set; }

        #endregion

        #region Command Implementations

        private void InitializeCommands()
        {
            NavigateToDashboardCommand = new RelayCommand(ExecuteNavigateToDashboard);
            NavigateToFilesCommand = new RelayCommand(ExecuteNavigateToFiles);
            NavigateToUsersCommand = new RelayCommand(ExecuteNavigateToUsers);
            NavigateToProjectsCommand = new RelayCommand(ExecuteNavigateToProjects);
            NavigateToClientCommand = new RelayCommand(ExecuteNavigateToClient);
            NavigateToMyWorkspaceCommand = new RelayCommand(ExecuteNavigateToMyWorkspace);
            NavigateToCollaborationCommand = new RelayCommand(ExecuteNavigateToCollaboration);
            NavigateToNotificationsCommand = new RelayCommand(ExecuteNavigateToNotifications);
            NavigateToReportsCommand = new RelayCommand(ExecuteNavigateToReports);
            
            // Use parameterless constructor và direct function reference
            NavigateToAdminCommand = new RelayCommand(
                execute: ExecuteNavigateToAdmin,
                canExecute: CanExecuteAdminCommands
            );
            NavigateToProductionCommand = new RelayCommand(
                execute: ExecuteNavigateToProduction,
                canExecute: CanExecuteAdminCommands
            );
            NavigateBackCommand = new RelayCommand(
                execute: ExecuteNavigateBack,
                canExecute: CanExecuteNavigateBack
            );
            
            RefreshDataCommand = new RelayCommand(ExecuteRefreshData);
            LogoutCommand = new RelayCommand(ExecuteLogout);
            SwitchModeCommand = new RelayCommand(ExecuteSwitchMode);
        }

        private void ExecuteNavigateToDashboard()
        {
            _navigationService.NavigateToDashboard();
            StatusMessage = "Dashboard được chọn";
        }

        private void ExecuteNavigateToFiles()
        {
            _navigationService.NavigateToFiles();
            StatusMessage = "File Management được chọn";
        }

        private void ExecuteNavigateToUsers()
        {
            _navigationService.NavigateToAdmin("Users");
            StatusMessage = "User Management được chọn";
        }

        private void ExecuteNavigateToProjects()
        {
            _navigationService.NavigateToProjects();
            StatusMessage = "Project Management được chọn";
        }

        private void ExecuteNavigateToClient()
        {
            _navigationService.NavigateToClient();
            StatusMessage = "Client Dashboard được chọn";
        }

        private void ExecuteNavigateToMyWorkspace()
        {
            _navigationService.NavigateToMyWorkspace();
            StatusMessage = "My Workspace được chọn";
        }

        private void ExecuteNavigateToCollaboration()
        {
            _navigationService.NavigateToCollaboration();
            StatusMessage = "Collaboration được chọn";
        }

        private void ExecuteNavigateToNotifications()
        {
            _navigationService.NavigateToNotifications();
            StatusMessage = "Notification Center được chọn";
        }

        private void ExecuteNavigateToReports()
        {
            _navigationService.NavigateToReports();
            StatusMessage = "Reports & Analytics được chọn";
        }

        private void ExecuteNavigateToAdmin()
        {
            _navigationService.NavigateToAdmin();
            StatusMessage = "Admin Panel được chọn";
        }

        private void ExecuteNavigateToProduction()
        {
            _navigationService.NavigateToProduction();
            StatusMessage = "Production Tools được chọn";
        }

        private void ExecuteNavigateBack()
        {
            if (_navigationService.NavigateBack())
            {
                StatusMessage = "Đã quay lại trang trước";
            }
        }

        private bool CanExecuteAdminCommands()
        {
            return IsAdminMode || (HasCurrentUser && CurrentUserRole == "Admin");
        }

        private bool CanExecuteNavigateBack()
        {
            return _navigationService.CanNavigateBack();
        }

        private async void ExecuteRefreshData()
        {
            await RefreshAllDataAsync();
        }

        private void ExecuteLogout()
        {
            try
            {
                //// Confirm logout
                //var result = System.Windows.MessageBox.Show(
                //    "Bạn có chắc chắn muốn đăng xuất?",
                //    "Xác nhận đăng xuất",
                //    System.Windows.MessageBoxButton.YesNo,
                //    System.Windows.MessageBoxImage.Question);

                if (true)
                {
                    // Clear user session
                    _userService.Logout();
                    _dataCache.Clear();
                    
                    // Publish logout event
                    _eventBus.PublishNotification("Đăng xuất", "Đã đăng xuất thành công", "Info");

                    // Lấy window hiện tại từ Application.Current.Windows
                    //Window currentWindow = null;
                    //foreach (Window window in System.Windows.Application.Current.Windows)
                    //{
                    //    if (window.DataContext == this) // Tìm window có DataContext là ViewModel này
                    //    {
                    //        currentWindow = window;
                    //        break;
                    //    }
                    //}

                    // Hoặc nếu đây là MainWindow
                    var currentWindow = System.Windows.Application.Current.MainWindow;

                    // Tạm thời tắt auto-shutdown
                    System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    // Show login window
                    var loginWindow = App.GetRequiredService<Views.LogInOut.LoginView>();
                    loginWindow.Show();

                    // Set LoginView làm MainWindow mới
                    System.Windows.Application.Current.MainWindow = loginWindow;

                    // Đặt lại ShutdownMode
                    System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

                    // Đóng window cũ - QUAN TRỌNG
                    currentWindow?.Close();


                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khi đăng xuất: {ex.Message}";
                // Khôi phục ShutdownMode nếu có lỗi
                System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
        }


        private void ExecuteSwitchMode()
        {
            IsAdminMode = !IsAdminMode;
            StatusMessage = IsAdminMode ? "Đã chuyển sang Admin mode" : "Đã chuyển sang Client mode";
            
            // Publish mode switch event
            _eventBus.PublishNotification("Chuyển chế độ", 
                IsAdminMode ? "Admin mode" : "Client mode", "Info");
        }

        #endregion

        #region Initialization

        private void InitializeProperties()
        {
            LoadingMessage = "Đang khởi tạo...";
            StatusMessage = "Sẵn sàng";
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            
            // Load user info if available
            var currentUser = _userService.CurrentUser;
            if (currentUser != null)
            {
                CurrentUserName = currentUser.FullName;
                CurrentUserRole = currentUser.Role.ToString();
                IsAdminMode = currentUser.Role.ToString() == "Admin";
            }
        }

        private async void InitializeAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Khởi tạo services...";

                // Initialize ServiceManager
                await _serviceManager.InitializeAllServicesAsync();
                
                LoadingMessage = "Khởi tạo navigation...";
                
                // Navigation sẽ được initialize từ MainWindow
                
                LoadingMessage = "Đang tải dữ liệu...";
                
                // Load initial data
                await LoadInitialDataAsync();
                
                LoadingMessage = "Khởi tạo timers...";
                
                // Start timers
                StartTimers();
                
                // Start cache cleanup
                _dataCache.StartBackgroundCleanup();
                
                LoadingMessage = "Hoàn thành khởi tạo";
                
                await Task.Delay(500); // Show completion message
                
                IsLoading = false;
                StatusMessage = "Sẵn sàng - Tất cả services đã được khởi tạo";
                
                // Publish initialization complete event
                _eventBus.PublishNotification("Khởi tạo", "Application đã sẵn sàng", "Success");
            }
            catch (Exception ex)
            {
                IsLoading = false;
                StatusMessage = $"Lỗi khởi tạo: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khởi tạo MainWindowViewModel: {ex.Message}");
            }
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                // Load user preferences
                if (HasCurrentUser)
                {
                    // Cache current user
                    _dataCache.SetCurrentUser(_userService.CurrentUser);
                    
                    // Load user preferences (mock)
                    var preferences = new
                    {
                        Theme = "Light",
                        Language = "vi-VN",
                        AutoRefresh = true
                    };
                    _dataCache.SetUserPreferences(preferences);
                }

                // Load system info
                var systemInfo = new
                {
                    Version = "1.0.0",
                    BuildDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Environment = "Development"
                };
                _dataCache.Set("SystemInfo", systemInfo, TimeSpan.FromDays(1));

                await Task.Delay(100); // Simulate loading time
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load initial data: {ex.Message}");
            }
        }

        #endregion

        #region Timer Management

        private void StartTimers()
        {
            // Timer cập nhật thời gian mỗi giây
            _timeUpdateTimer = new System.Threading.Timer(
                callback: (_) => UpdateCurrentTime(),
                state: null,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromSeconds(1)
            );

            // Timer refresh data mỗi 5 phút
            _dataRefreshTimer = new System.Threading.Timer(
                callback: async (_) => await RefreshDataPeriodically(),
                state: null,
                dueTime: TimeSpan.FromMinutes(5),
                period: TimeSpan.FromMinutes(5)
            );
        }

        private void UpdateCurrentTime()
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            }));
        }

        private async Task RefreshDataPeriodically()
        {
            try
            {
                // Refresh trong background, không hiển thị loading
                await RefreshAllDataAsync(showLoading: false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi refresh data: {ex.Message}");
            }
        }

        #endregion

        #region Data Management

        /// <summary>
        /// Refresh tất cả data từ các services
        /// </summary>
        public async Task RefreshAllDataAsync(bool showLoading = true)
        {
            try
            {
                if (showLoading)
                {
                    IsLoading = true;
                    LoadingMessage = "Đang cập nhật dữ liệu...";
                }

                // Refresh service health
                var healthReport = _serviceManager.GetServicesHealthReport();
                _dataCache.Set("ServiceHealth", healthReport, TimeSpan.FromMinutes(1));

                // Publish data refresh event
                _eventBus.PublishDataUpdated("All", "Refreshed");

                if (showLoading)
                {
                    await Task.Delay(500); // Show loading briefly
                    IsLoading = false;
                }

                StatusMessage = $"Dữ liệu đã được cập nhật - {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                if (showLoading)
                {
                    IsLoading = false;
                }
                StatusMessage = $"Lỗi cập nhật dữ liệu: {ex.Message}";
            }
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            // Subscribe to navigation actions
            _navigationService.NavigationChanged = OnNavigationChanged;
            
            // Subscribe to EventBus events
            _eventBus.Subscribe<NotificationEvent>(OnNotificationReceived);
            _eventBus.Subscribe<DataUpdateEvent>(OnDataUpdated);
            _eventBus.Subscribe<PerformanceAlertEvent>(OnPerformanceAlert);
        }

        private void OnNavigationChanged(object sender, NavigationChangedEventArgs e)
        {
            StatusMessage = $"Đã chuyển tới: {e.To?.TabName ?? "Unknown"}";
            
            // Update selected tab index if needed
            var tabName = e.To?.TabName;
            var newIndex = GetTabIndexByName(tabName);
            if (newIndex >= 0 && newIndex != SelectedTabIndex)
            {
                _selectedTabIndex = newIndex; // Direct assignment to avoid recursion
                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }

        private void OnNotificationReceived(NotificationEvent notification)
        {
            StatusMessage = $"{notification.Type}: {notification.Message}";
        }

        private void OnDataUpdated(DataUpdateEvent dataUpdate)
        {
            StatusMessage = $"Dữ liệu {dataUpdate.DataType} đã được {dataUpdate.Action}";
        }

        private void OnPerformanceAlert(PerformanceAlertEvent alert)
        {
            StatusMessage = $"⚠️ {alert.AlertType}: {alert.Message}";
        }

        private void OnTabChanged()
        {
            var tabName = GetTabNameByIndex(SelectedTabIndex);
            if (!string.IsNullOrEmpty(tabName))
            {
                StatusMessage = $"Tab được chọn: {tabName}";
            }
        }

        #endregion

        #region Helper Methods

        private int GetTabIndexByName(string tabName)
        {
            switch (tabName)
            {
                case "Dashboard": return 0;
                case "Files": return 1;
                case "Users": return 2;
                case "Projects": return 3;
                case "Client": return 4;
                case "MyWorkspace": return 5;
                case "Collaboration": return 6;
                case "Notifications": return 7;
                case "Reports": return 8;
                case "Admin": return 9;
                case "Production": return 10;
                default: return -1;
            }
        }

        private string GetTabNameByIndex(int index)
        {
            switch (index)
            {
                case 0: return "Dashboard";
                case 1: return "Files";
                case 2: return "Users";
                case 3: return "Projects";
                case 4: return "Client";
                case 5: return "MyWorkspace";
                case 6: return "Collaboration";
                case 7: return "Notifications";
                case 8: return "Reports";
                case 9: return "Admin";
                case 10: return "Production";
                default: return "";
            }
        }

        #endregion

        #region IDisposable Implementation

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Stop timers
                    _timeUpdateTimer?.Dispose();
                    _dataRefreshTimer?.Dispose();
                    
                    // Unsubscribe from events
                    if (_navigationService != null)
                    {
                        _navigationService.NavigationChanged = null;
                    }
                    
                    // Cleanup services
                    _serviceManager?.Cleanup();
                    _navigationService?.Cleanup();
                    _dataCache?.Cleanup();
                    _eventBus?.Cleanup();
                    
                    System.Diagnostics.Debug.WriteLine("🧹 MainWindowViewModel đã được cleanup");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Lỗi cleanup MainWindowViewModel: {ex.Message}");
                }
            }
            
            base.Dispose(disposing);
        }

        #endregion
    }
}