using ManagementFile.App.Controls;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using ManagementFile.App.ViewModels.Controls;
using ManagementFile.App.ViewModels.Project;
using ManagementFile.App.Views.Project;
using ManagementFile.Contracts.DTOs.ProjectManagement;
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

namespace ManagementFile.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Enterprise Hub cho ManagementFile Platform - tích hợp tất cả 5 phases
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields
        private readonly MainWindowViewModel _viewModel;
        private readonly ServiceManager _serviceManager;
        private readonly NavigationService _navigationService;
        private readonly DataCache _dataCache;
        private readonly EventBus _eventBus;
        private readonly UserMenuViewModel _userMenuViewModel;
        private readonly UserManagementService _userService;

        #endregion

        public MainWindow(
            UserMenuViewModel userMenuViewModel,
            MainWindowViewModel viewModel,
            ServiceManager serviceManager,
            NavigationService navigationService,
            DataCache dataCache,
            EventBus eventBus,
            UserManagementService userService
            )
        {
            InitializeComponent();

            // Khởi tạo services
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _userMenuViewModel = userMenuViewModel ?? throw new ArgumentNullException(nameof(userMenuViewModel));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            // Lấy ViewModel
            DataContext = _viewModel;

            // Set data context cho UserMenuPopup
            UserMenuPopup.DataContext = _userMenuViewModel;

            // Subscribe to ClosePopupRequested event
            _userMenuViewModel.ClosePopupRequested += OnClosePopupRequested;

            // Khởi tạo navigation
            InitializeNavigation();

            // Subscribe to events
            SubscribeToEvents();

            // Load tab contents
            LoadTabContents();

            System.Diagnostics.Debug.WriteLine("🏢 MainWindow Enterprise Hub đã được khởi tạo");
        }

        #region Initialization

        /// <summary>
        /// Khởi tạo navigation service với MainTabControl
        /// </summary>
        private void InitializeNavigation()
        {
            try
            {
                var proj = App.GetRequiredService<ProjectManagentsDragablzView>();
                ProjectDis.Content = proj;
                //_navigationService.Initialize(MainTabControl);
                System.Diagnostics.Debug.WriteLine("🧭 Navigation đã được khởi tạo với MainTabControl");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khởi tạo navigation: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe to application events
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                // Subscribe to tab selection changed
                //MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;

                // Subscribe to EventBus events
                _eventBus.Subscribe<NotificationEvent>(OnNotificationReceived);

                System.Diagnostics.Debug.WriteLine("📡 Đã subscribe to application events");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi subscribe events: {ex.Message}");
            }
        }

        /// <summary>
        /// Load nội dung cho các tabs động
        /// </summary>
        private async void LoadTabContents()
        {
            try
            {
                // Load tab contents theo yêu cầu (lazy loading)
                await Task.Delay(100); // Delay để UI render xong

                System.Diagnostics.Debug.WriteLine("📋 Tab contents sẽ được load khi cần thiết (lazy loading)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load tab contents: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Xử lý khi tab selection thay đổi
        /// </summary>
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var tabControl = sender as TabControl;
                if (tabControl == null) return;

                var selectedTab = tabControl.SelectedItem as TabItem;
                if (selectedTab == null) return;

                // Load content cho tab được chọn
                LoadContentForTab(selectedTab);

                // Update navigation context
                var tabName = GetTabNameFromTabItem(selectedTab);
                if (!string.IsNullOrEmpty(tabName))
                {
                    _dataCache.Set($"CurrentTab", tabName, TimeSpan.FromHours(1));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi xử lý tab selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Load content động cho tab được chọn
        /// </summary>
        private void LoadContentForTab(TabItem selectedTab)
        {
            try
            {
                if (selectedTab == null) return;

                // Tìm ContentControl trong tab
                var contentControl = FindContentControl(selectedTab);
                if (contentControl == null) return;

                // Load content dựa trên tab name
                switch (selectedTab.Name)
                {
                    case "SmartDashboardContent":
                        LoadSmartDashboardContent(contentControl);
                        break;
                    case "GlobalSearchContent":
                        LoadGlobalSearchContent(contentControl);
                        break;
                    case "FileManagementTab":
                        LoadFileManagementContent(contentControl);
                        break;
                    case "UserManagementTab":
                        LoadUserManagementContent(contentControl);
                        break;
                    case "ClientDashboardTab":
                        LoadClientDashboardContent(contentControl);
                        break;
                    case "MyWorkspaceTab":
                        LoadMyWorkspaceContent(contentControl);
                        break;
                    case "CollaborationTab":
                        LoadCollaborationContent(contentControl);
                        break;
                    case "NotificationsTab":
                        LoadNotificationsContent(contentControl);
                        break;
                    case "ReportsTab":
                        LoadReportsContent(contentControl);
                        break;
                    case "AdminTab":
                        LoadAdminContent(contentControl);
                        break;
                    case "ProductionTab":
                        LoadProductionContent(contentControl);
                        break;
                    case "ProjectAdTab":
                        LoadProjectAd(contentControl);
                        break;
                }

                // Check if content control has x:Name attribute for Smart Dashboard
                if (contentControl != null && contentControl.Name == "SmartDashboardContent")
                {
                    LoadSmartDashboardContent(contentControl);
                }
                else if (contentControl != null && contentControl.Name == "GlobalSearchContent")
                {
                    LoadGlobalSearchContent(contentControl);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load content cho tab: {ex.Message}");
            }
        }

        /// <summary>
        /// Tìm ContentControl trong TabItem
        /// </summary>
        private ContentControl FindContentControl(TabItem tabItem)
        {
            try
            {
                // Tìm ContentControl đầu tiên trong tab content
                if (tabItem.Content is ContentControl contentControl)
                {
                    return contentControl;
                }

                // Nếu content là Grid hoặc container khác, tìm ContentControl bên trong
                if (tabItem.Content is Panel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is ContentControl cc)
                        {
                            return cc;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi tìm ContentControl: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Content Loading Methods

        /// <summary>
        /// Load File Management content
        /// </summary>
        private void LoadFileManagementContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return; // Đã load rồi

                // Kiểm tra xem có FileManagementMainView không
                var cachedView = _navigationService.GetCachedView("FileManagement");
                if (cachedView != null)
                {
                    contentControl.Content = cachedView;
                    return;
                }

                // Tạo mới FileManagementMainView nếu có
                try
                {
                    var fileManagementType = Type.GetType("ManagementFile.App.FileManagement.Views.FileManagementMainView");
                    if (fileManagementType != null)
                    {
                        var fileManagementView = Activator.CreateInstance(fileManagementType) as UserControl;
                        if (fileManagementView != null)
                        {
                            contentControl.Content = fileManagementView;
                            _navigationService.CacheView("FileManagement", fileManagementView);
                            System.Diagnostics.Debug.WriteLine("📁 FileManagementMainView đã được load");
                            return;
                        }
                    }
                }
                catch
                {
                    // Fallback nếu không tìm thấy FileManagementMainView
                }

                // Fallback: hiển thị message
                contentControl.Content = new TextBlock
                {
                    Text = "📁 File Management\n\nModule sẵn sàng để tích hợp\nFileManagementMainView sẽ được load tại đây",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load FileManagement content: {ex.Message}");
            }
        }

        

        private void LoadProjectAd(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                var cachedView = _navigationService.GetCachedView("ProjectAd");
                if (cachedView != null)
                {
                    contentControl.Content = cachedView;
                    return;
                }

                // Tạo mới ProjectManagementMainView từ đúng namespace
                try
                {
                    var projectManagementView = App.GetRequiredService<ProjectManagentsDragablzView>();
                    if (projectManagementView != null)
                    {
                        contentControl.Content = projectManagementView;
                        _navigationService.CacheView("ProjectAd", projectManagementView);
                        System.Diagnostics.Debug.WriteLine("📋 ProjectManagementMainView đã được load");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Lỗi tạo ProjectManagementMainView: {ex.Message}");
                }

                // Fallback: hiển thị message khi không thể load view
                contentControl.Content = new TextBlock
                {
                    Text = "📋 Project Management\n\nPhase 2 - Đã triển khai hoàn chỉnh ✅\n\n• Quản lý Projects với filtering & search\n• Task Management với time tracking\n• Team Member Management\n• Real-time Dashboard\n• Mock API Integration\n\nProjectManagementMainView sẽ được load tại đây",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Orange
                };

                System.Diagnostics.Debug.WriteLine("📋 ProjectManagement placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load ProjectManagement content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Client Dashboard content
        /// </summary>
        private void LoadClientDashboardContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                contentControl.Content = new TextBlock
                {
                    Text = "👤 Client Dashboard\n\nPhase 3 - Hoàn thành 100%\nClientDashboardView sẽ được tích hợp",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Blue
                };

                System.Diagnostics.Debug.WriteLine("👤 ClientDashboard placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load ClientDashboard content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Reports content
        /// </summary>
        private void LoadReportsContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                contentControl.Content = new TextBlock
                {
                    Text = "📊 Reports & Analytics\n\nPhase 4 - Hoàn thành 100%\nReportsMainView sẽ được tích hợp",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Green
                };

                System.Diagnostics.Debug.WriteLine("📊 Reports placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load Reports content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load My Workspace content
        /// </summary>
        private void LoadMyWorkspaceContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                contentControl.Content = new TextBlock
                {
                    Text = "🏠 My Workspace\n\nPhase 3 - Hoàn thành 100%\nMyWorkspaceView sẽ được tích hợp",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Purple
                };

                System.Diagnostics.Debug.WriteLine("🏠 MyWorkspace placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load MyWorkspace content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Collaboration content
        /// </summary>
        private void LoadCollaborationContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                contentControl.Content = new TextBlock
                {
                    Text = "🤝 Team Collaboration\n\nPhase 3 - Hoàn thành 100%\nCollaborationView sẽ được tích hợp",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Teal
                };

                System.Diagnostics.Debug.WriteLine("🤝 Collaboration placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load Collaboration content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Notifications content
        /// </summary>
        private void LoadNotificationsContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                contentControl.Content = new TextBlock
                {
                    Text = "🔔 Notification Center\n\nPhase 3 - Hoàn thành 100%\nNotificationCenterView sẽ được tích hợp",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Red
                };

                System.Diagnostics.Debug.WriteLine("🔔 Notifications placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load Notifications content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load User Management content
        /// </summary>
        private void LoadUserManagementContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                contentControl.Content = new TextBlock
                {
                    Text = "👥 User Management\n\nPhase 1 - Hoàn thành 100%\nUserManagementView sẽ được tích hợp",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Orange
                };

                System.Diagnostics.Debug.WriteLine("👥 UserManagement placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load UserManagement content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Admin content
        /// </summary>
        private void LoadAdminContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                contentControl.Content = new TextBlock
                {
                    Text = "⚙️ Admin Control Panel\n\nPhase 1 - Hoàn thành 100%\nAdminMainWindow content sẽ được tích hợp",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Red
                };

                System.Diagnostics.Debug.WriteLine("⚙️ Admin placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load Admin content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Production Tools content
        /// </summary>
        private void LoadProductionContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return;

                contentControl.Content = new TextBlock
                {
                    Text = "🚀 Production Tools\n\nPhase 5 - Hoàn thành 100%\nProductionReadinessView và các tools khác sẽ được tích hợp",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Purple
                };

                System.Diagnostics.Debug.WriteLine("🚀 Production placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load Production content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Smart Dashboard content (Phase 7)
        /// </summary>
        private void LoadSmartDashboardContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return; // Đã load rồi

                // Kiểm tra xem có SmartDashboardView trong cache không
                var cachedView = _navigationService.GetCachedView("SmartDashboard");
                if (cachedView != null)
                {
                    contentControl.Content = cachedView;
                    System.Diagnostics.Debug.WriteLine("🏠 SmartDashboard từ cache đã được load");
                    return;
                }

                // Tạo mới SmartDashboardView
                try
                {
                    var smartDashboardType = Type.GetType("ManagementFile.App.Views.Dashboard.SmartDashboardView");
                    if (smartDashboardType != null)
                    {
                        var smartDashboardView = Activator.CreateInstance(smartDashboardType) as UserControl;
                        if (smartDashboardView != null)
                        {
                            contentControl.Content = smartDashboardView;
                            _navigationService.CacheView("SmartDashboard", smartDashboardView);
                            System.Diagnostics.Debug.WriteLine("🏠 SmartDashboardView đã được load và cached");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Lỗi tạo SmartDashboardView: {ex.Message}");
                }

                // Fallback: hiển thị loading message cho Smart Dashboard
                contentControl.Content = new TextBlock
                {
                    Text = "🏠 Smart Dashboard\n\nPhase 7 - Advanced Integration Features\n⚡ Loading enterprise intelligence dashboard...\n\n📊 Real-time metrics from all 5 phases\n🔄 Cross-phase event integration\n📈 Performance monitoring\n🚨 System health alerts",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.DarkBlue
                };

                System.Diagnostics.Debug.WriteLine("🏠 SmartDashboard placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load SmartDashboard content: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Global Search content (Phase 7)
        /// </summary>
        private void LoadGlobalSearchContent(ContentControl contentControl)
        {
            try
            {
                if (contentControl.Content != null) return; // Đã load rồi

                // Kiểm tra xem có GlobalSearchView trong cache không
                var cachedView = _navigationService.GetCachedView("GlobalSearch");
                if (cachedView != null)
                {
                    contentControl.Content = cachedView;
                    System.Diagnostics.Debug.WriteLine("🔍 GlobalSearch từ cache đã được load");
                    return;
                }

                // Tạo mới GlobalSearchView
                try
                {
                    var globalSearchType = Type.GetType("ManagementFile.App.Views.Search.GlobalSearchView");
                    if (globalSearchType != null)
                    {
                        var globalSearchView = Activator.CreateInstance(globalSearchType) as UserControl;
                        if (globalSearchView != null)
                        {
                            contentControl.Content = globalSearchView;
                            _navigationService.CacheView("GlobalSearch", globalSearchView);
                            System.Diagnostics.Debug.WriteLine("🔍 GlobalSearchView đã được load và cached");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Lỗi tạo GlobalSearchView: {ex.Message}");
                }

                // Fallback: hiển thị loading message cho Global Search
                contentControl.Content = new TextBlock
                {
                    Text = "🔍 Global Search\n\nPhase 7 - Advanced Integration Features\n⚡ Loading universal search engine...\n\n🌐 Cross-phase search capabilities\n🧠 Intelligent ranking algorithms\n💡 Smart suggestions\n📊 Advanced analytics",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.DarkGreen
                };

                System.Diagnostics.Debug.WriteLine("🔍 GlobalSearch placeholder loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi load GlobalSearch content: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Lấy tab name từ TabItem
        /// </summary>
        private string GetTabNameFromTabItem(TabItem tabItem)
        {
            if (tabItem?.Name == null) return "";

            switch (tabItem.Name)
            {
                case "FileManagementTab": return "Files";
                case "UserManagementTab": return "Users";
                case "ProjectManagementTab": return "Projects";
                case "ClientDashboardTab": return "Client";
                case "MyWorkspaceTab": return "MyWorkspace";
                case "CollaborationTab": return "Collaboration";
                case "NotificationsTab": return "Notifications";
                case "ReportsTab": return "Reports";
                case "AdminTab": return "Admin";
                case "ProductionTab": return "Production";
                default: return "Dashboard";
            }
        }

        #endregion

        #region Event Bus Handlers

        /// <summary>
        /// Xử lý notification events
        /// </summary>
        private void OnNotificationReceived(NotificationEvent notification)
        {
            try
            {
                // Update status bar với notification
                if (_viewModel != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // StatusMessage sẽ được cập nhật thông qua ViewModel
                    }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi xử lý notification: {ex.Message}");
            }
        }

        #endregion

        #region Window Events

        /// <summary>
        /// Xử lý khi window đóng
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Unsubscribe events
                //if (MainTabControl != null)
                //{
                //    MainTabControl.SelectionChanged -= MainTabControl_SelectionChanged;
                //}

                // Clear user session
                _userService.Logout();
                _dataCache.Clear();

                _userMenuViewModel.ClosePopupRequested -= OnClosePopupRequested;

                // Cleanup services thông qua ViewModel
                if (_viewModel != null)
                {
                    _viewModel.Dispose();
                }

                System.Diagnostics.Debug.WriteLine("🧹 MainWindow đã được cleanup");

                base.OnClosed(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi cleanup MainWindow: {ex.Message}");
                base.OnClosed(e);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Navigate đến một tab cụ thể từ external calls
        /// </summary>
        /// <param name="tabName">Tên tab</param>
        public void NavigateToTab(string tabName)
        {
            try
            {
                _navigationService.NavigateToTab(tabName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi navigate to tab {tabName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh tất cả data
        /// </summary>
        public async Task RefreshAllDataAsync()
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAllDataAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi refresh data: {ex.Message}");
            }
        }

        #endregion

        private void UserAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle popup
            UserMenuPopup.IsOpen = !UserMenuPopup.IsOpen;
        }

        private void OnClosePopupRequested()
        {
            UserMenuPopup.IsOpen = false;
        }
    }
}
