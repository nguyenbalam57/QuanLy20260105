using Dragablz;
using ManagementFile.App.Controls.Projects;
using ManagementFile.App.DragablzUser;
using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Controls.Projects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Project
{
    public class ProjectManagentsDragablzViewViewModel : BaseViewModel
    {
        private readonly IControlFactory _controlFactory;
        private TabItemViewModel _selectedTab;
        private int _tabCounter = 1;

        public ProjectManagentsDragablzViewViewModel(IControlFactory controlFactory)
        {
            _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));

            // Khởi tạo collection tabs
            Tabs = new ObservableCollection<TabItemViewModel>();

            // Khởi tạo InterTabClient
            InterTabClient = new CustomInterTabClient();

            // Khởi tạo commands
            InitializeCommands();

            // Tạo tab chính
            
        }

        #region Properties

        public ObservableCollection<TabItemViewModel> Tabs { get; set; }
        public CustomInterTabClient InterTabClient { get; set; }

        public TabItemViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    System.Diagnostics.Debug.WriteLine($"🔵 Selected tab changed: {value?.Title ?? "null"}");
                    OnPropertyChanged(nameof(TabCountInfo));
                    UpdateCommandStates();
                }
            }
        }

        public string TabCountInfo => $"Tổng: {Tabs.Count} tabs";

        #endregion

        #region Commands

        public ICommand AddTabCommand { get; private set; }
        public ICommand CloseAllTabsCommand { get; private set; }
        public ICommand CloseOtherTabsCommand { get; private set; }
        public ICommand CloseTabsToRightCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            AddTabCommand = new RelayCommand(ExecuteAddTab);
            CloseAllTabsCommand = new RelayCommand(ExecuteCloseAllTabs, CanCloseAllTabs);
            CloseOtherTabsCommand = new RelayCommand(ExecuteCloseOtherTabs, HasSelectedTab);
            CloseTabsToRightCommand = new RelayCommand(ExecuteCloseTabsToRight, HasSelectedTab);
        }

        public void Initialize()
        {
            // Method để gọi từ View nếu cần

            CreateMainTab();
        }

        private void CreateMainTab()
        {
            Tabs.Clear();

            var mainTab = new TabItemViewModel
            {
                Title = "Quản lý dự án",
                Content = App.GetRequiredService<ProjectsControl>(),
                CreatedTime = DateTime.Now,
                IconGlyph = "📋"
            };

            mainTab.SetAsMainTab();
            mainTab.CloseRequested += OnTabCloseRequested;

            if(Tabs.Any(t => t.Title == mainTab.Title))
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ Main tab already exists");
                return;
            }

            Tabs.Add(mainTab);
            SelectedTab = mainTab;

            System.Diagnostics.Debug.WriteLine("✅ Main tab created");
        }

        #endregion

        #region Tab Management

        public void AddTab(string title, object content, string iconGlyph = null, bool isCloseable = true)
        {
            var newTab = new TabItemViewModel
            {
                Title = title,
                Content = content,
                CreatedTime = DateTime.Now,
                IconGlyph = iconGlyph,
                IsCloseable = isCloseable,
                IsDraggable = true,
            };

            // Subscribe to close event
            newTab.CloseRequested += OnTabCloseRequested;

            Tabs.Add(newTab);
            SelectedTab = newTab;

            OnPropertyChanged(nameof(TabCountInfo));
            UpdateCommandStates();

            System.Diagnostics.Debug.WriteLine($"✅ Added tab: {title}");
        }

        public void AddTabProjectTask(ProjectModel projectModel)
        {
            if (projectModel == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ ProjectModel is null");
                return;
            }

            // Kiểm tra tab đã tồn tại chưa
            var existingTab = Tabs.FirstOrDefault(t =>
                t.Title.Contains(projectModel.IdText) &&
                t.Title.Contains("Công việc Dự án: "));

            if (existingTab != null)
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️ Tab already exists: {projectModel.IdText}");
                SelectedTab = existingTab;
                return;
            }

            try
            {
                var projectTasksControl = _controlFactory.CreateProjectTask(projectModel);
                AddTab(
                    $"Công việc Dự án: {projectModel.IdText}",
                    projectTasksControl,
                    "📝",
                    true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating project task tab: {ex.Message}");
                MessageBox.Show($"Lỗi tạo tab: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddTabTaskComment(ProjectTaskModel projectTaskModel)
        {
            if (projectTaskModel == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ ProjectTaskModel is null");
                return;
            }

            // Kiểm tra tab đã tồn tại chưa
            var existingTab = Tabs.FirstOrDefault(t =>
                t.Title.Contains(projectTaskModel.IdText) &&
                t.Title.Contains("Bình luận: "));

            if (existingTab != null)
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️ Tab already exists: {projectTaskModel.IdText}");
                SelectedTab = existingTab;
                return;
            }

            try
            {
                var taskCommentsControl = _controlFactory.CreateTaskComment(projectTaskModel);
                AddTab(
                    $"Bình luận: {projectTaskModel.IdText}",
                    taskCommentsControl,
                    "💬",
                    true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating task comment tab: {ex.Message}");
                MessageBox.Show($"Lỗi tạo tab: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddTabProjectMembers(ProjectModel projectModel)
        {
            if (projectModel == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ ProjectModel is null");
                return;
            }
            // Kiểm tra tab đã tồn tại chưa
            var existingTab = Tabs.FirstOrDefault(t =>
                t.Title.Contains(projectModel.IdText) &&
                t.Title.Contains("Thành viên: "));
            if (existingTab != null)
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️ Tab already exists: {projectModel.IdText}");
                SelectedTab = existingTab;
                return;
            }
            try
            {
                var projectMembersControl = _controlFactory.CreateProjectMember(projectModel);
                AddTab(
                    $"Thành viên: {projectModel.IdText}",
                    projectMembersControl,
                    "👥",
                    true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating project members tab: {ex.Message}");
                MessageBox.Show($"Lỗi tạo tab: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// hiển thị nhập công, thời gian làm việc
        /// </summary>
        /// <param name="projectModel"></param>
        public void AddTabTimeTrackings(ProjectModel projectModel)
        {
            if (projectModel == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ ProjectModel is null");
                return;
            }
            // Kiểm tra tab đã tồn tại chưa
            var existingTab = Tabs.FirstOrDefault(t =>
                t.Title.Contains(projectModel.IdText) &&
                t.Title.Contains("Giờ làm: "));
            if (existingTab != null)
            {
                System.Diagnostics.Debug.WriteLine($"📅 Tab already exists: {projectModel.IdText}");
                SelectedTab = existingTab;
                return;
            }
            try
            {
                var projectMembersControl = _controlFactory.CreateTimeTracking(projectModel);
                AddTab(
                    $"Giờ làm: {projectModel.IdText}",
                    projectMembersControl,
                    "📅",
                    true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating project task time tracking tab: {ex.Message}");
                MessageBox.Show($"Lỗi tạo tab: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnTabCloseRequested(object sender, TabItemViewModel tab)
        {
            System.Diagnostics.Debug.WriteLine($"🔔 Close requested for tab: {tab.Title}");
            CloseTab(tab);
        }

        private void CloseTab(TabItemViewModel tab)
        {
            if (tab == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Tab is null");
                return;
            }

            if (!tab.IsCloseable || tab.IsPinned)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Tab cannot be closed: {tab.Title}");
                MessageBox.Show(
                    "Tab này không thể đóng!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Kiểm tra modified
            if (tab.IsModified)
            {
                var result = MessageBox.Show(
                    $"Tab '{tab.Title}' có thay đổi chưa lưu. Bạn có muốn đóng?",
                    "Xác nhận đóng",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    System.Diagnostics.Debug.WriteLine($"ℹ️ User cancelled close for: {tab.Title}");
                    return;
                }
            }

            // Unsubscribe event
            tab.CloseRequested -= OnTabCloseRequested;

            // Remove tab
            Tabs.Remove(tab);
            System.Diagnostics.Debug.WriteLine($"✅ Closed tab: {tab.Title}");

            OnPropertyChanged(nameof(TabCountInfo));
            UpdateCommandStates();

            // Chọn tab gần nhất
            if (!Tabs.Any())
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No tabs left, creating main tab");
                CreateMainTab();
            }
            else if (SelectedTab == null || !Tabs.Contains(SelectedTab))
            {
                SelectedTab = Tabs.Last();
                System.Diagnostics.Debug.WriteLine($"ℹ️ Selected tab: {SelectedTab.Title}");
            }
        }

        #endregion

        #region Command Implementations

        private void ExecuteAddTab(object parameter)
        {
            _tabCounter++;
            System.Diagnostics.Debug.WriteLine($"ℹ️ Adding new tab #{_tabCounter}");
            AddTab(
                $"➕ Tab mới {_tabCounter}",
                new System.Windows.Controls.TextBlock
                {
                    Text = $"Nội dung tab {_tabCounter}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16
                },
                "➕");
        }

        private bool CanCloseAllTabs(object parameter)
        {
            return Tabs.Any(t => t.IsCloseable && !t.IsPinned);
        }

        private void ExecuteCloseAllTabs(object parameter)
        {
            var closeable = Tabs.Where(t => t.IsCloseable && !t.IsPinned).ToList();

            if (!closeable.Any())
            {
                MessageBox.Show("Không có tab nào có thể đóng!", "Thông báo");
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn đóng {closeable.Count} tabs?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            foreach (var tab in closeable)
            {
                tab.CloseRequested -= OnTabCloseRequested;
                Tabs.Remove(tab);
                System.Diagnostics.Debug.WriteLine($"✅ Closed tab: {tab.Title}");
            }

            OnPropertyChanged(nameof(TabCountInfo));
            UpdateCommandStates();
        }

        private bool HasSelectedTab(object parameter)
        {
            return SelectedTab != null && Tabs.Count > 1;
        }

        private void ExecuteCloseOtherTabs(object parameter)
        {
            if (SelectedTab == null)
                return;

            var tabsToClose = Tabs.Where(t =>
                t != SelectedTab &&
                t.IsCloseable &&
                !t.IsPinned).ToList();

            if (!tabsToClose.Any())
            {
                MessageBox.Show("Không có tab nào khác có thể đóng!", "Thông báo");
                return;
            }

            foreach (var tab in tabsToClose)
            {
                tab.CloseRequested -= OnTabCloseRequested;
                Tabs.Remove(tab);
                System.Diagnostics.Debug.WriteLine($"✅ Closed tab: {tab.Title}");
            }

            OnPropertyChanged(nameof(TabCountInfo));
            UpdateCommandStates();
        }

        private void ExecuteCloseTabsToRight(object parameter)
        {
            if (SelectedTab == null)
                return;

            var selectedIndex = Tabs.IndexOf(SelectedTab);
            var tabsToClose = Tabs.Skip(selectedIndex + 1)
                                  .Where(t => t.IsCloseable && !t.IsPinned)
                                  .ToList();

            if (!tabsToClose.Any())
            {
                MessageBox.Show("Không có tab nào bên phải có thể đóng!", "Thông báo");
                return;
            }

            foreach (var tab in tabsToClose)
            {
                tab.CloseRequested -= OnTabCloseRequested;
                Tabs.Remove(tab);
                System.Diagnostics.Debug.WriteLine($"✅ Closed tab: {tab.Title}");
            }

            OnPropertyChanged(nameof(TabCountInfo));
            UpdateCommandStates();
        }

        private void UpdateCommandStates()
        {
            (CloseAllTabsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CloseOtherTabsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CloseTabsToRightCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}