using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Đại diện cho một navigation item
    /// </summary>
    public class NavigationItem
    {
        public string TabName { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public DateTime NavigationTime { get; set; }
    }

    /// <summary>
    /// Event args cho navigation changed
    /// </summary>
    public class NavigationChangedEventArgs : EventArgs
    {
        public NavigationItem From { get; set; }
        public NavigationItem To { get; set; }
        public NavigationType NavigationType { get; set; }
    }

    /// <summary>
    /// Event args cho navigation changing (có thể cancel)
    /// </summary>
    public class NavigationChangingEventArgs : EventArgs
    {
        public NavigationItem From { get; set; }
        public NavigationItem To { get; set; }
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Loại navigation
    /// </summary>
    public enum NavigationType
    {
        Forward,
        Back,
        Direct,
        Refresh
    }

    /// <summary>
    /// Service quản lý navigation thống nhất cho ManagementFile Enterprise Platform
    /// Điều phối navigation giữa tất cả các phases và views
    /// </summary>
    public sealed class NavigationService
    {
        #region DI
        
        public NavigationService()
        {
            _navigationHistory = new Stack<NavigationItem>();
            _viewCache = new Dictionary<string, UserControl>();
            _activeContexts = new Dictionary<string, object>();
        }
        #endregion

        #region Private Fields
        private readonly Stack<NavigationItem> _navigationHistory;
        private readonly Dictionary<string, UserControl> _viewCache;
        private readonly Dictionary<string, object> _activeContexts;
        private TabControl _mainTabControl;
        private NavigationItem _currentNavigation;
        #endregion

        #region Events
        /// <summary>
        /// Action được trigger khi navigation thay đổi
        /// </summary>
        public Action<object, NavigationChangedEventArgs> NavigationChanged { get; set; }

        /// <summary>
        /// Action được trigger trước khi navigation thay đổi (có thể cancel)
        /// </summary>
        public Action<object, NavigationChangingEventArgs> NavigationChanging { get; set; }
        #endregion

        #region Initialization

        /// <summary>
        /// Khởi tạo NavigationService với MainTabControl
        /// </summary>
        /// <param name="mainTabControl">TabControl chính của application</param>
        public void Initialize(TabControl mainTabControl)
        {
            _mainTabControl = mainTabControl ?? throw new ArgumentNullException(nameof(mainTabControl));
            System.Diagnostics.Debug.WriteLine("🧭 NavigationService đã được khởi tạo với MainTabControl");
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Navigate đến một tab cụ thể
        /// </summary>
        /// <param name="tabName">Tên tab cần navigate</param>
        /// <param name="parameters">Parameters truyền vào (optional)</param>
        public bool NavigateToTab(string tabName, Dictionary<string, object> parameters = null)
        {
            try
            {
                if (_mainTabControl == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ MainTabControl chưa được khởi tạo");
                    return false;
                }

                // Tạo navigation item mới
                var navigationItem = new NavigationItem
                {
                    TabName = tabName,
                    Parameters = parameters ?? new Dictionary<string, object>(),
                    NavigationTime = DateTime.Now
                };

                // Kiểm tra navigation changing event
                var changingArgs = new NavigationChangingEventArgs
                {
                    From = _currentNavigation,
                    To = navigationItem,
                    Cancel = false
                };

                // Safe event invocation
                OnNavigationChanging(changingArgs);

                if (changingArgs.Cancel)
                {
                    System.Diagnostics.Debug.WriteLine($"🚫 Navigation tới {tabName} đã bị hủy");
                    return false;
                }

                // Thực hiện navigation
                var tabIndex = GetTabIndexByName(tabName);
                if (tabIndex >= 0)
                {
                    // Lưu current navigation vào history
                    if (_currentNavigation != null)
                    {
                        _navigationHistory.Push(_currentNavigation);
                    }

                    // Set tab mới
                    _mainTabControl.SelectedIndex = tabIndex;
                    _currentNavigation = navigationItem;

                    // Lưu context nếu có
                    if (parameters != null)
                    {
                        _activeContexts[tabName] = parameters;
                    }

                    // Trigger navigation changed event
                    var changedArgs = new NavigationChangedEventArgs
                    {
                        From = changingArgs.From,
                        To = navigationItem,
                        NavigationType = NavigationType.Forward
                    };
                    
                    OnNavigationChanged(changedArgs);

                    System.Diagnostics.Debug.WriteLine($"✅ Đã navigate tới tab: {tabName}");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Không tìm thấy tab: {tabName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi navigate tới {tabName}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Navigation History

        /// <summary>
        /// Navigate trở lại tab trước đó
        /// </summary>
        public bool NavigateBack()
        {
            try
            {
                if (_navigationHistory.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("📋 Não có history để navigate back");
                    return false;
                }

                var previousNavigation = _navigationHistory.Pop();
                
                // Navigate tới tab trước đó mà không lưu vào history
                var tabIndex = GetTabIndexByName(previousNavigation.TabName);
                if (tabIndex >= 0)
                {
                    var oldNavigation = _currentNavigation;
                    
                    _mainTabControl.SelectedIndex = tabIndex;
                    _currentNavigation = previousNavigation;

                    // Restore context
                    if (_activeContexts.ContainsKey(previousNavigation.TabName))
                    {
                        // Restore parameters nếu có
                    }

                    // Trigger navigation changed event
                    var changedArgs = new NavigationChangedEventArgs
                    {
                        From = oldNavigation,
                        To = previousNavigation,
                        NavigationType = NavigationType.Back
                    };
                    
                    OnNavigationChanged(changedArgs);

                    System.Diagnostics.Debug.WriteLine($"⬅️ Navigate back tới: {previousNavigation.TabName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi navigate back: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Safe Event Invocations

        /// <summary>
        /// Safely invoke NavigationChanging action
        /// </summary>
        private void OnNavigationChanging(NavigationChangingEventArgs args)
        {
            try
            {
                NavigationChanging?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi NavigationChanging action: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely invoke NavigationChanged action
        /// </summary>
        private void OnNavigationChanged(NavigationChangedEventArgs args)
        {
            try
            {
                NavigationChanged?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi NavigationChanged action: {ex.Message}");
            }
        }

        #endregion

        #region All Navigation Methods

        /// <summary>
        /// Navigate tới Dashboard (tab mặc định)
        /// </summary>
        public bool NavigateToDashboard()
        {
            return NavigateToTab("Dashboard");
        }

        /// <summary>
        /// Navigate tới Admin panel
        /// </summary>
        /// <param name="subSection">Sub-section trong admin (optional)</param>
        public bool NavigateToAdmin(string subSection = null)
        {
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(subSection))
            {
                parameters["SubSection"] = subSection;
            }
            return NavigateToTab("Admin", parameters);
        }

        /// <summary>
        /// Navigate tới Project Management
        /// </summary>
        /// <param name="projectId">ID của project cần mở (optional)</param>
        /// <param name="tabIndex">Index của tab trong ProjectManagement (optional)</param>
        public bool NavigateToProjects(int? projectId = null, int? tabIndex = null)
        {
            var parameters = new Dictionary<string, object>();
            if (projectId.HasValue)
            {
                parameters["ProjectId"] = projectId.Value;
            }
            if (tabIndex.HasValue)
            {
                parameters["TabIndex"] = tabIndex.Value;
            }
            return NavigateToTab("Projects", parameters);
        }

        /// <summary>
        /// Navigate tới Client Dashboard
        /// </summary>
        public bool NavigateToClient()
        {
            return NavigateToTab("Client");
        }

        /// <summary>
        /// Navigate tới My Workspace
        /// </summary>
        /// <param name="tabIndex">Index của tab trong MyWorkspace (optional)</param>
        public bool NavigateToMyWorkspace(int? tabIndex = null)
        {
            var parameters = new Dictionary<string, object>();
            if (tabIndex.HasValue)
            {
                parameters["TabIndex"] = tabIndex.Value;
            }
            return NavigateToTab("MyWorkspace", parameters);
        }

        /// <summary>
        /// Navigate tới Collaboration
        /// </summary>
        /// <param name="tabIndex">Index của tab trong Collaboration (optional)</param>
        public bool NavigateToCollaboration(int? tabIndex = null)
        {
            var parameters = new Dictionary<string, object>();
            if (tabIndex.HasValue)
            {
                parameters["TabIndex"] = tabIndex.Value;
            }
            return NavigateToTab("Collaboration", parameters);
        }

        /// <summary>
        /// Navigate tới Notification Center
        /// </summary>
        public bool NavigateToNotifications()
        {
            return NavigateToTab("Notifications");
        }

        /// <summary>
        /// Navigate tới Reports
        /// </summary>
        /// <param name="reportType">Loại report cần mở (optional)</param>
        public bool NavigateToReports(string reportType = null)
        {
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(reportType))
            {
                parameters["ReportType"] = reportType;
            }
            return NavigateToTab("Reports", parameters);
        }

        /// <summary>
        /// Navigate tới File Management
        /// </summary>
        /// <param name="projectId">ID của project (optional)</param>
        public bool NavigateToFiles(int? projectId = null)
        {
            var parameters = new Dictionary<string, object>();
            if (projectId.HasValue)
            {
                parameters["ProjectId"] = projectId.Value;
            }
            return NavigateToTab("Files", parameters);
        }

        /// <summary>
        /// Navigate tới Production/Advanced tools
        /// </summary>
        public bool NavigateToProduction()
        {
            return NavigateToTab("Production");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Xóa navigation history
        /// </summary>
        public void ClearHistory()
        {
            _navigationHistory.Clear();
            System.Diagnostics.Debug.WriteLine("🗑️ Đã xóa navigation history");
        }

        /// <summary>
        /// Lấy navigation history hiện tại
        /// </summary>
        public List<NavigationItem> GetNavigationHistory()
        {
            return new List<NavigationItem>(_navigationHistory.ToArray());
        }

        /// <summary>
        /// Lấy context của tab hiện tại
        /// </summary>
        /// <param name="tabName">Tên tab</param>
        /// <returns>Context object hoặc null</returns>
        public object GetTabContext(string tabName)
        {
            return _activeContexts.TryGetValue(tabName, out var context) ? context : null;
        }

        /// <summary>
        /// Set context cho một tab
        /// </summary>
        /// <param name="tabName">Tên tab</param>
        /// <param name="context">Context object</param>
        public void SetTabContext(string tabName, object context)
        {
            _activeContexts[tabName] = context;
            System.Diagnostics.Debug.WriteLine($"💾 Đã lưu context cho tab: {tabName}");
        }

        /// <summary>
        /// Xóa context của một tab
        /// </summary>
        /// <param name="tabName">Tên tab</param>
        public void ClearTabContext(string tabName)
        {
            _activeContexts.Remove(tabName);
            System.Diagnostics.Debug.WriteLine($"🗑️ Đã xóa context của tab: {tabName}");
        }

        /// <summary>
        /// Lấy index của tab theo tên
        /// </summary>
        private int GetTabIndexByName(string tabName)
        {
            if (_mainTabControl == null) return -1;

            // Mapping tab names to indexes
            var tabMapping = new Dictionary<string, int>
            {
                {"Dashboard", 0},
                {"Files", 1},
                {"Users", 2},
                {"Projects", 3},
                {"Client", 4},
                {"MyWorkspace", 5},
                {"Collaboration", 6},
                {"Notifications", 7},
                {"Reports", 8},
                {"Admin", 9},
                {"Production", 10}
            };

            return tabMapping.TryGetValue(tabName, out var index) ? index : -1;
        }

        /// <summary>
        /// Lấy tên tab hiện tại
        /// </summary>
        public string GetCurrentTabName()
        {
            return _currentNavigation?.TabName ?? "";
        }

        /// <summary>
        /// Kiểm tra xem có thể navigate back không
        /// </summary>
        public bool CanNavigateBack()
        {
            return _navigationHistory.Count > 0;
        }

        #endregion

        #region View Cache Management

        /// <summary>
        /// Cache một view để tái sử dụng
        /// </summary>
        /// <param name="key">Key của view</param>
        /// <param name="view">View instance</param>
        public void CacheView(string key, UserControl view)
        {
            _viewCache[key] = view;
            System.Diagnostics.Debug.WriteLine($"💾 Đã cache view: {key}");
        }

        /// <summary>
        /// Lấy cached view
        /// </summary>
        /// <param name="key">Key của view</param>
        /// <returns>Cached view hoặc null</returns>
        public UserControl GetCachedView(string key)
        {
            return _viewCache.TryGetValue(key, out var view) ? view : null;
        }

        /// <summary>
        /// Xóa cached view
        /// </summary>
        /// <param name="key">Key của view</param>
        public void RemoveCachedView(string key)
        {
            _viewCache.Remove(key);
            System.Diagnostics.Debug.WriteLine($"🗑️ Đã xóa cached view: {key}");
        }

        /// <summary>
        /// Xóa tất cả cached views
        /// </summary>
        public void ClearViewCache()
        {
            _viewCache.Clear();
            System.Diagnostics.Debug.WriteLine("🗑️ Đã xóa tất cả cached views");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Dọn dẹp NavigationService
        /// </summary>
        public void Cleanup()
        {
            try
            {
                ClearHistory();
                ClearViewCache();
                _activeContexts.Clear();
                _currentNavigation = null;
                
                System.Diagnostics.Debug.WriteLine("🧹 NavigationService đã được dọn dẹp");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi dọn dẹp NavigationService: {ex.Message}");
            }
        }

        #endregion
    }
}