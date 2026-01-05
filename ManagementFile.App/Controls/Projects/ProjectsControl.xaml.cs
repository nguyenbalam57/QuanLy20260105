using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Projects.PermissionProjects;
using ManagementFile.App.ViewModels.Controls;
using ManagementFile.App.ViewModels.Controls.Projects;
using ManagementFile.App.ViewModels.Project;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace ManagementFile.App.Controls.Projects
{
    /// <summary>
    /// Interaction logic for ProjectsControl.xaml
    /// Enhanced with hierarchy support similar to TaskCommentsControl
    /// </summary>
    public partial class ProjectsControl : UserControl
    {
        #region Private Fields

        private readonly ProjectsControlViewModel _viewModel;

        // Lazy Loading Fields
        private ScrollViewer _internalScrollViewer;
        private bool _isLoadingMore = false;
        private int _currentRootPage = 1;
        private int _loadedPagesCount = 1;
        private HashSet<int> _loadedProjectIds = new HashSet<int>();
        private double _lastScrollOffset = 0;
        private double _scrollPositionBeforeRefresh = 0;

        // Reply Management Fields
        private Dictionary<int, (DateTime LastLoaded, int ChildrenCount)> _loadedChildrenCache
            = new Dictionary<int, (DateTime, int)>();
        private Dictionary<int, int> _childPagesLoaded = new Dictionary<int, int>();

        // Filter state (added for visibility)
        private string _searchKeyword = string.Empty;
        private ProjectStatus _selectedStatus = ProjectStatus.All;

        #endregion

        #region Constructor

        public ProjectsControl(ProjectsControlViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Setup event handlers
            ProjectsList.PreviewMouseLeftButtonDown += ProjectsList_PreviewMouseLeftButtonDown;
            ProjectsList.MouseRightButtonUp += ProjectsList_MouseRightButtonUp;
            ProjectsList.MouseDoubleClick += ProjectsList_MouseDoubleClick;
            ProjectsList.KeyDown += ProjectsList_KeyDown;
            ProjectsList.Sorting += ProjectsList_Sorting;

            System.Diagnostics.Debug.WriteLine("ProjectsControl initialized with hierarchy support");
        }

        #endregion

        #region DataGrid Event Handlers

        private void ProjectsList_Loaded(object sender, RoutedEventArgs e)
        {
            if (_internalScrollViewer == null)
            {
                // Subscribe to LayoutUpdated để đợi Visual Tree hoàn chỉnh
                ProjectsList.LayoutUpdated += ProjectsList_LayoutUpdated;
            }
        }

        private void ProjectsList_LayoutUpdated(object sender, EventArgs e)
        {
            if (_internalScrollViewer != null)
                return;

            _internalScrollViewer = FindVisualChild<ScrollViewer>(ProjectsList);

            if (_internalScrollViewer != null)
            {
                // Unsubscribe sau khi tìm thấy
                ProjectsList.LayoutUpdated -= ProjectsList_LayoutUpdated;

                // Attach scroll event
                _internalScrollViewer.ScrollChanged += InternalScrollViewer_ScrollChanged;
                System.Diagnostics.Debug.WriteLine("✓ Internal ScrollViewer found for ProjectsList");
            }
        }

        private void ProjectsList_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_viewModel.SelectedProject is ProjectModel selectedProject)
                {
                    var isCtrlPressed = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);
                    var isShiftPressed = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift);

                    switch (e.Key)
                    {
                        case Key.Space:
                            // Toggle expand/collapse
                            if (selectedProject.HasChildren)
                            {
                                ToggleProjectExpansion(selectedProject);
                            }
                            e.Handled = true;
                            break;

                        case Key.Enter:
                            if (isCtrlPressed)
                            {
                                // Ctrl+Enter: Add child project
                                _viewModel?.AddChilderProjectCommand?.Execute(selectedProject);
                            }
                            else
                            {
                                // Enter: View details
                                _viewModel?.ViewProjectDetailsCommand?.Execute(selectedProject);
                            }
                            e.Handled = true;
                            break;

                        case Key.E:
                            if (isCtrlPressed)
                            {
                                // Ctrl+E: Edit
                                _viewModel?.EditProjectCommand?.Execute(selectedProject);
                                e.Handled = true;
                            }
                            break;

                        case Key.Delete:
                            // Delete project
                            _viewModel?.DeleteProjectCommand?.Execute(selectedProject);
                            e.Handled = true;
                            break;

                        case Key.F5:
                            // F5: Refresh
                            _viewModel?.RefreshProjectsCommand?.Execute(null);
                            e.Handled = true;
                            break;

                        case Key.N:
                            if (isCtrlPressed)
                            {
                                // Ctrl+N: New project
                                _viewModel?.AddProjectCommand?.Execute(null);
                                e.Handled = true;
                            }
                            break;

                        case Key.T:
                            if (isCtrlPressed)
                            {
                                // Ctrl+T: View tasks
                                _viewModel?.ViewProjectTasksCommand?.Execute(selectedProject);
                                e.Handled = true;
                            }
                            break;

                        case Key.M:
                            if (isCtrlPressed)
                            {
                                // Ctrl+M: View members
                                _viewModel?.ViewProjectMemebersCommand?.Execute(selectedProject);
                                e.Handled = true;
                            }
                            break;

                        case Key.F1:
                            // F1: Show help
                            ShowKeyboardShortcutsHelp();
                            e.Handled = true;
                            break;

                        case Key.Escape:
                            // Escape: Clear selection
                            ProjectsList.SelectedItem = null;
                            e.Handled = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in keyboard navigation: {ex.Message}");
            }
        }

        

        private void ProjectsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid?.SelectedItem is ProjectModel project)
                {
                    _viewModel?.ViewProjectDetailsCommand?.Execute(project);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MouseDoubleClick: {ex.Message}");
            }
        }

        private void ProjectsList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid == null) return;

                var hitTest = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
                var row = FindVisualParent<DataGridRow>(hitTest.VisualHit);

                if (row?.Item is ProjectModel projectModel)
                {
                    // Select the row
                    dataGrid.SelectedItem = projectModel;
                    _viewModel.SelectedProject = projectModel;

                    // Show enhanced context menu
                    ShowEnhancedContextMenu(projectModel, row);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectModel] Error in ProjectsList_MouseRightButtonUp: {ex.Message}");
            }
        }

        private void ProjectsList_Sorting(object sender, DataGridSortingEventArgs e)
        {
            try
            {
                // Custom sorting logic for hierarchy
                var dataGrid = sender as DataGrid;
                if (dataGrid?.ItemsSource == null) return;

                // Prevent default sorting for hierarchy columns
                if (e.Column.SortMemberPath == "HierarchyLevel")
                {
                    e.Handled = true;
                    return;
                }

                // Maintain hierarchy structure while sorting
                System.Diagnostics.Debug.WriteLine($"Sorting by: {e.Column.SortMemberPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DataGrid sorting: {ex.Message}");
            }
        }

        private void ProjectsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);

                if (row?.Item is ProjectModel project)
                {
                    // Check if Load More row
                    if (_viewModel.IsLoadMorePlaceholder(project))
                    {
                        // Handle single click on Load More row
                        var parentProjectId = _viewModel.GetParentProjectIdFromLoadMorePlaceholder(project);
                        var parentProject = FindProjectById(_viewModel.Projects, parentProjectId);

                        if (parentProject != null && !parentProject.IsLoadingChildren)
                        {
                            int currentLoaded = parentProject.Children?.Count ?? 0;
                            int nextPage = (currentLoaded / 20) + 1;

                            _ = LoadChildrenForProjectAsync(parentProject, page: nextPage);
                        }

                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PreviewMouseLeftButtonDown: {ex.Message}");
            }
        }

        private ProjectModel FindProjectById(IEnumerable<ProjectModel> project, int projectId)
        {
            if (project == null) return null;

            foreach (var task in project)
            {
                if (task.Id == projectId)
                    return task;

                var found = FindProjectById(task.Children, projectId);
                if (found != null)
                    return found;
            }

            return null;
        }

        #endregion

        #region Filter Event Handlers

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedValue is ProjectStatus status)
            {
                _viewModel?.FilterStatusCommand?.Execute(status);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _viewModel?.SearchCommand?.Execute(textBox.Text);
            }
        }

        private void ProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                _viewModel.SelectedProject = dataGrid.SelectedItem as ProjectModel;
            }
        }

        #endregion

        #region Hierarchy Management

        /// <summary>
        /// Handle expand/collapse button click
        /// </summary>
        private async void ExpandChildrenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var project = button?.Tag as ProjectModel;

                if (project == null) return;

                // Prevent multiple clicks while loading
                if (project.IsLoadingChildren)
                {
                    System.Diagnostics.Debug.WriteLine($"Already loading children for project {project.Id}");
                    return;
                }

                // Toggle expansion state
                var wasExpanded = project.IsExpanded;

                if (wasExpanded)
                {
                    // Collapse - just hide children
                    System.Diagnostics.Debug.WriteLine($"Collapsing project {project.Id}");
                    project.IsExpanded = false;
                    _viewModel?.RefreshFlattenedProjects();
                }
                else
                {
                    // Expand - load children if not loaded yet
                    System.Diagnostics.Debug.WriteLine($"Expanding project {project.Id}");

                    project.IsExpanded = true;

                    // Check if we need to load children
                    var loadedChildren = project.Children?.Count ?? 0;
                    var totalChildren = project.ChildCount;

                    if (loadedChildren == 0 && totalChildren > 0)
                    {
                        // No children loaded yet - load first page
                        System.Diagnostics.Debug.WriteLine($"Loading first page of children for project {project.Id}...");

                        project.IsLoadingChildren = true;

                        try
                        {
                            await LoadChildrenForProjectAsync(project, page: 1);
                        }
                        finally
                        {
                            project.IsLoadingChildren = false;
                        }
                    }
                    else if (loadedChildren < totalChildren)
                    {
                        // Some children loaded but not all
                        System.Diagnostics.Debug.WriteLine($"Project {project.Id} has {loadedChildren}/{totalChildren} children loaded");
                    }
                    else
                    {
                        // All children already loaded
                        System.Diagnostics.Debug.WriteLine($"Project {project.Id} has all {loadedChildren} children loaded");
                    }

                    // Refresh display
                    _viewModel?.RefreshFlattenedProjects();
                }

                // Update button visual immediately
                button.Content = project.ExpandCollapseIcon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExpandChildrenButton_Click: {ex.Message}");
                MessageBox.Show($"Lỗi khi mở/đóng dự án con: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Toggle project expansion
        /// </summary>
        private void ToggleProjectExpansion(ProjectModel project)
        {
            if (project == null) return;

            try
            {
                project.IsExpanded = !project.IsExpanded;

                // Load children nếu chưa load và đang expand
                if (project.IsExpanded && project.Children.Count == 0 && project.HasChildren)
                {
                    _ = LoadChildrenForProjectAsync(project, page: 1);
                }
                else
                {
                    // Refresh ngay lập tức
                    _viewModel?.RefreshFlattenedProjects();
                }

                System.Diagnostics.Debug.WriteLine($"Toggled expansion for project {project.Id}: IsExpanded = {project.IsExpanded}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling expansion: {ex.Message}");
            }
        }

        /// <summary>
        /// Load children for a specific project
        /// </summary>
        private async Task LoadChildrenForProjectAsync(ProjectModel parentProject, int page = 1)
        {
            if (parentProject == null || _viewModel == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading children for project {parentProject.Id} (page {page})...");

                var children = await _viewModel.LoadChildrenForProjectAsync(
                    parentProject.Id,
                    page,
                    _viewModel.PageSize);

                if (children != null && children.Count > 0)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        // Clear old children if loading first page
                        if (page == 1)
                        {
                            parentProject.Children.Clear();
                        }

                        foreach (var child in children)
                        {
                            // Check for duplicates
                            var existing = parentProject.Children.FirstOrDefault(c => c.Id == child.Id);
                            if (existing == null)
                            {
                                // Set hierarchy level
                                child.HierarchyLevel = parentProject.HierarchyLevel + 1;
                                parentProject.Children.Add(child);

                                // Track child cache
                                if (child.HasChildren)
                                {
                                    _loadedChildrenCache[child.Id] = (DateTime.UtcNow, child.Children.Count);
                                    _childPagesLoaded[child.Id] = 1;
                                }
                            }
                        }

                        // Update cache for parent project
                        _loadedChildrenCache[parentProject.Id] = (DateTime.UtcNow, parentProject.Children.Count);
                        _childPagesLoaded[parentProject.Id] = page;

                        // Refresh flattened hierarchy
                        _viewModel.RefreshFlattenedProjects();
                    });

                    System.Diagnostics.Debug.WriteLine($"Loaded {children.Count} children for project {parentProject.Id}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No children found for project {parentProject.Id} on page {page}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading children for project {parentProject.Id}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Lazy Loading

        /// <summary>
        /// Handle scroll changed event for lazy loading
        /// ✅ ENHANCED: Better bottom detection using ScrollBar Thumb position
        /// </summary>
        private async void InternalScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (_isLoadingMore || !_viewModel.CanLoadMore || _viewModel == null)
                    return;

                var scrollViewer = sender as ScrollViewer;
                if (scrollViewer == null)
                    return;

                // Only proceed if scroll actually changed (not just extent changed)
                if (e.VerticalChange == 0)
                    return;

                // Only load if scrolling down AND near/at bottom
                if (IsBottomScroll(scrollViewer))
                {
                    System.Diagnostics.Debug.WriteLine($"   Current page: {_viewModel.CurrentPage}, Can load more: {_viewModel.CanLoadMore}");
                    System.Diagnostics.Debug.WriteLine($"   Projects loaded: {_viewModel.Projects?.Count ?? 0}");

                    await LoadMoreProjectsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in scroll changed handler: {ex.Message}");
            }
        }

        // Method kiểm tra và load thêm dữ liệu
        private bool IsBottomScroll(ScrollViewer scrollViewer)
        {
            // Sử dụng Thumb position (Chi tiết hơn)
            var scrollBar = FindVisualChild<ScrollBar>(scrollViewer);
            if (scrollBar == null) return false;

            var scrollBarHeight = scrollBar.ActualHeight;
            var thumb = FindVisualChild<Thumb>(scrollBar);

            if (thumb != null)
            {
                var thumbHeight = thumb.ActualHeight;
                var thumbPosition = thumb.TransformToAncestor(scrollBar).Transform(new Point(0, 0)).Y;
                var thumbBottomPosition = thumbPosition + thumbHeight;

                // Kiểm tra thumb đã chạm hoặc gần chạm bottom
                double thumbThreshold = 150; // pixels
                bool thumbAtBottom = thumbBottomPosition >= scrollBarHeight - thumbThreshold;

                if (thumbAtBottom)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Load more projects when scrolling
        /// Enhanced with better pagination tracking and TotalPages support
        /// ✅ FIXED: Properly sync FilteredProjects and refresh hierarchy
        /// ✅ FIXED: Use ViewModel.CurrentPage instead of local _currentRootPage
        /// </summary>
        private async Task LoadMoreProjectsAsync()
        {
            if (_isLoadingMore || !_viewModel.CanLoadMore)
            {
                System.Diagnostics.Debug.WriteLine($"⏸️ Skip loading: IsLoadingMore={_isLoadingMore}, CanLoadMore={_viewModel.CanLoadMore}");
                return;
            }

            try
            {
                _isLoadingMore = true;
                ShowLoadingMoreIndicator();

                // Use ViewModel.CurrentPage + 1
                var nextPage = _viewModel.CurrentPage + 1;
                System.Diagnostics.Debug.WriteLine($"📥 [Control] Loading page {nextPage} of projects (PageSize: {_viewModel.PageSize})...");
                System.Diagnostics.Debug.WriteLine($"📊 [Control] ViewModel state: CurrentPage={_viewModel.CurrentPage}, TotalPages={_viewModel.TotalPages}");

                // Load next page via ViewModel
                var newProjects = await _viewModel.LoadMoreProjectsAsync(nextPage, _viewModel.PageSize);

                if (newProjects != null && newProjects.Count > 0)
                {
                    var addedCount = 0;

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        foreach (var project in newProjects)
                        {
                            // Add to both Projects AND FilteredProjects
                            _viewModel.Projects.Add(project);

                            addedCount++;

                            // Track child loading state
                            if (project.HasChildren)
                            {
                                _loadedChildrenCache[project.Id] = (DateTime.UtcNow, project.Children.Count);
                                _childPagesLoaded[project.Id] = 1;
                            }
                        }

                        // Refresh flattened hierarchy để update UI
                        _viewModel.RefreshFlattenedProjects();
                    });

                    // Update local tracking based on ViewModel.CurrentPage
                    _currentRootPage = _viewModel.CurrentPage;
                    _loadedPagesCount = Math.Max(_loadedPagesCount, _currentRootPage);

                    System.Diagnostics.Debug.WriteLine($"✅ [Control] Loaded {addedCount} new projects");
                    System.Diagnostics.Debug.WriteLine($"📊 [Control] Synced state: _currentRootPage={_currentRootPage}, ViewModel.CurrentPage={_viewModel.CurrentPage}");
                    System.Diagnostics.Debug.WriteLine($"📊 [Control] Collections: Projects={_viewModel.Projects?.Count ?? 0}, FilteredProjects={_viewModel.FilteredProjects?.Count ?? 0}, FlattenedProjects={_viewModel.FlattenedProjects?.Count ?? 0}");

                    // ✅ IMPROVED: Check if we can load more based on returned items
                    if (newProjects.Count < _viewModel.PageSize)
                    {
                        // Returned less than page size -> no more pages
                        _viewModel.CanLoadMore = false;
                        System.Diagnostics.Debug.WriteLine($"🛑 [Control] No more pages available (returned {newProjects.Count} < {_viewModel.PageSize})");
                    }
                    else
                    {
                        // Check if we've reached TotalPages from ViewModel
                        if (_viewModel.TotalPages > 0 && _viewModel.CurrentPage >= _viewModel.TotalPages)
                        {
                            _viewModel.CanLoadMore = false;
                            System.Diagnostics.Debug.WriteLine($"🛑 [Control] Reached max pages ({_viewModel.CurrentPage}/{_viewModel.TotalPages})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"✨ [Control] More pages available. Can continue loading...");
                        }
                    }

                    // Restore scroll position
                    await RestoreScrollPositionAsync();
                }
                else
                {
                    // No projects returned -> end of list
                    _viewModel.CanLoadMore = false;
                    System.Diagnostics.Debug.WriteLine($"🛑 [Control] No more projects available (empty result)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [Control] Error loading more projects: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải thêm dự án: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingMore = false;
                HideLoadingMoreIndicator();

                System.Diagnostics.Debug.WriteLine($"🏁 Loading complete. State: CanLoadMore={_viewModel.CanLoadMore}, Page={_currentRootPage}");
            }
        }

        /// <summary>
        /// ✅ NEW: Check if project passes current filters
        /// </summary>
        private bool PassesCurrentFilters(ProjectModel project)
        {
            if (project == null) return false;

            // Check search keyword
            if (!string.IsNullOrEmpty(_viewModel.SearchKeyword))
            {
                var keyword = _viewModel.SearchKeyword.ToLower();
                var matchesSearch = project.ProjectName?.ToLower().Contains(keyword) == true ||
                                   project.ProjectCode?.ToLower().Contains(keyword) == true ||
                                   project.Description?.ToLower().Contains(keyword) == true;

                if (!matchesSearch) return false;
            }

            // Check status filter
            if (_viewModel.SelectedStatus != ProjectStatus.All)
            {
                if (project.Status != _viewModel.SelectedStatus)
                    return false;
            }

            return true;
        }

        #endregion

        #region Scroll Position Management

        private void SaveScrollPosition()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_internalScrollViewer != null)
                {
                    _scrollPositionBeforeRefresh = _internalScrollViewer.VerticalOffset;
                    System.Diagnostics.Debug.WriteLine($"💾 Saved scroll position: {_scrollPositionBeforeRefresh}");
                }
            });
        }

        private async Task RestoreScrollPositionAsync()
        {
            await Task.Delay(50);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_internalScrollViewer != null && _lastScrollOffset > 0)
                {
                    _internalScrollViewer.ScrollToVerticalOffset(_lastScrollOffset);
                    System.Diagnostics.Debug.WriteLine($"📍 Restored scroll to: {_lastScrollOffset}");
                }
            });
        }

        private async Task RestoreScrollPositionAfterRefreshAsync()
        {
            await Task.Delay(100);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_internalScrollViewer != null && _scrollPositionBeforeRefresh > 0)
                {
                    _internalScrollViewer.ScrollToVerticalOffset(_scrollPositionBeforeRefresh);
                    System.Diagnostics.Debug.WriteLine($"📍 Restored scroll position to: {_scrollPositionBeforeRefresh}");
                }
            });
        }

        public void ScrollToTop()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _internalScrollViewer?.ScrollToTop();
            });
        }

        public void ScrollToBottom()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _internalScrollViewer?.ScrollToBottom();
            });
        }

        #endregion

        #region UI Indicators

        private void ShowLoadingMoreIndicator()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (LoadingMoreIndicator != null)
                {
                    LoadingMoreIndicator.Visibility = Visibility.Visible;
                }
            });
        }

        private void HideLoadingMoreIndicator()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (LoadingMoreIndicator != null)
                {
                    LoadingMoreIndicator.Visibility = Visibility.Collapsed;
                }
            });
        }

        #endregion

        #region Context Menu

        private async Task ShowEnhancedContextMenu(ProjectModel projectModel, FrameworkElement target)
        {
            try
            {
                int fontIconSize = 14;
                var contextMenu = new ContextMenu
                {
                    Style = TryFindResource("TabContextMenuStyle") as Style
                };

                // Basic actions
                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Thêm dự án mới",
                    Tag = projectModel,
                    Command = _viewModel?.AddProjectCommand,
                    ToolTip = "Thêm dự án mới (Ctrl+N)",
                    Icon = new TextBlock { Text = "➕", FontSize = fontIconSize }
                });

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Chỉnh sửa dự án",
                    Tag = projectModel,
                    Command = _viewModel?.EditProjectCommand,
                    CommandParameter = projectModel,
                    ToolTip = "Chỉnh sửa thông tin dự án (Ctrl+E)",
                    Icon = new TextBlock { Text = "✏️", FontSize = fontIconSize },
                });

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Xem chi tiết",
                    Tag = projectModel,
                    Command = _viewModel?.ViewProjectDetailsCommand,
                    CommandParameter = projectModel,
                    ToolTip = "Xem chi tiết dự án (Enter)",
                    Icon = new TextBlock { Text = "👁️", FontSize = fontIconSize }
                });

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Thêm dự án con",
                    Tag = projectModel,
                    Command = _viewModel?.AddChilderProjectCommand,
                    CommandParameter = projectModel,
                    ToolTip = "Thêm dự án con cho dự án này (Ctrl+Enter)",
                    Icon = new TextBlock { Text = "📁", FontSize = fontIconSize }
                });

                contextMenu.Items.Add(new Separator());

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Cập nhật tiến độ dự án",
                    Tag = projectModel,
                    Command = _viewModel?.ProgresProjectCommand,
                    CommandParameter = projectModel,
                    ToolTip = "Cập nhật tiến độ dự án và thời gian thực hiện thực tế",
                    Icon = new TextBlock { Text = "📊", FontSize = fontIconSize },
                });


                // Status actions với ProjectStatuss extensions
                //var statusMenu = new MenuItem
                //{
                //    Header = "Thay đổi trạng thái",
                //    Visibility = GetMenuItemVisibility(await PermissionProject.HasPermissionManagerProjectOfProject(projectModel.Id)),
                //    Icon = new TextBlock
                //    {
                //        Text = "📊",
                //        FontSize = fontIconSize,
                //        VerticalAlignment = VerticalAlignment.Center
                //    }
                //};

                //foreach (ProjectStatus status in Enum.GetValues(typeof(ProjectStatus)))
                //{
                //    if (status == ProjectStatus.All) continue;

                //    var statusMenuItem = new MenuItem
                //    {
                //        Header = $"{ProjectStatusHelper.GetDisplayIcon(status)} {ProjectStatusHelper.GetName(status)}",
                //        Tag = projectModel,
                //        Command = _viewModel?.ChangeProjectStatusCommand,
                //        CommandParameter = new { Project = projectModel, Status = status },
                //        ToolTip = $"{ProjectStatusHelper.GetTooltip(status)}",
                //        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ProjectStatusHelper.GetBackgroundColor(status) + "20"))
                //    };

                //    if (projectModel.Status == status)
                //    {
                //        statusMenuItem.FontWeight = FontWeights.Bold;
                //        statusMenuItem.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ProjectStatusHelper.GetBackgroundColor(status) + "40"));
                //    }

                //    statusMenu.Items.Add(statusMenuItem);
                //}
                //contextMenu.Items.Add(statusMenu);


                contextMenu.Items.Add(new Separator());

                //// Hierarchy actions
                //if (projectModel.ProjectParentId.HasValue || _viewModel.Projects.Any(p => !p.ProjectParentId.HasValue))
                //{
                //    var moveMenuItem = new MenuItem
                //    {
                //        Header = "📦 Di chuyển dự án",
                //        ToolTip = "Di chuyển dự án này cần liên hệ với quản lý!",
                //        Tag = projectModel
                //    };

                //    if (await CanCurrentUserEditProject(projectModel))
                //    {
                //        // Move to root
                //        if (projectModel.ProjectParentId.HasValue)
                //        {
                //            var moveToRootItem = new MenuItem
                //            {
                //                Header = "↑ Di chuyển lên gốc",
                //                Tag = projectModel,
                //                ToolTip = "Di chuyển dự án này lên cấp gốc (không có parent)"
                //            };
                //            moveToRootItem.Click += async (s, e) => await MoveToRoot_Click(projectModel);
                //            moveMenuItem.Items.Add(moveToRootItem);
                //            moveMenuItem.Items.Add(new Separator());
                //        }

                //        // Move to other parents
                //        var potentialParents = _viewModel.Projects
                //            .Where(p => p.Id != projectModel.Id &&
                //                       !IsDescendantOf(p, projectModel) &&
                //                       p.ProjectParentId != projectModel.Id)
                //            .Take(10)
                //            .ToList();

                //        if (potentialParents.Any())
                //        {
                //            foreach (var parent in potentialParents)
                //            {
                //                var moveToParentItem = new MenuItem
                //                {
                //                    Header = $"→ Di chuyển vào: {parent.ProjectName}",
                //                    Tag = projectModel,
                //                    ToolTip = $"Di chuyển dự án này thành con của '{parent.ProjectName}'"
                //                };

                //                var targetParent = parent; // Capture for closure
                //                moveToParentItem.Click += async (s, e) => await MoveToParent_Click(projectModel, targetParent);
                //                moveMenuItem.Items.Add(moveToParentItem);
                //            }
                //        }
                //    }

                //    contextMenu.Items.Add(moveMenuItem);
                //    contextMenu.Items.Add(new Separator());
                //}

                // View actions
                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Xem công việc",
                    Tag = projectModel,
                    Command = _viewModel?.ViewProjectTasksCommand,
                    CommandParameter = projectModel,
                    ToolTip = "Xem danh sách công việc (Ctrl+T)",
                    Icon = new TextBlock { Text = "📋", FontSize = fontIconSize }
                });

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Xem thành viên",
                    Tag = projectModel,
                    Command = _viewModel?.ViewProjectMemebersCommand,
                    CommandParameter = projectModel,
                    ToolTip = "Xem danh sách thành viên (Ctrl+M)",
                    Icon = new TextBlock { Text = "👥", FontSize = fontIconSize }
                });

                contextMenu.Items.Add(new Separator());

                //// Hierarchy info
                //if (projectModel.HasChildren || projectModel.ProjectParentId.HasValue)
                //{
                //    var hierarchyInfo = new MenuItem
                //    {
                //        Header = $"ℹ️ Thông tin phân cấp",
                //        IsEnabled = false
                //    };

                //    hierarchyInfo.Items.Add(new MenuItem
                //    {
                //        Header = $"📊 Cấp độ: {projectModel.HierarchyLevel}",
                //        IsEnabled = false
                //    });

                //    if (projectModel.HasChildren)
                //    {
                //        hierarchyInfo.Items.Add(new MenuItem
                //        {
                //            Header = $"📁 Số dự án con: {projectModel.ChildCount}",
                //            IsEnabled = false
                //        });
                //    }

                //    if (projectModel.ProjectParentId.HasValue)
                //    {
                //        hierarchyInfo.Items.Add(new MenuItem
                //        {
                //            Header = $"↑ Parent ID: {projectModel.ProjectParentId}",
                //            IsEnabled = false
                //        });
                //    }

                //    contextMenu.Items.Add(hierarchyInfo);
                //    contextMenu.Items.Add(new Separator());
                //}

                // Refresh action
                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Làm mới",
                    Tag = projectModel,
                    Command = _viewModel?.ReloadProjectCommand,
                    ToolTip = "Làm mới danh sách (F5)",
                    Icon = new TextBlock { Text = "🔄", FontSize = fontIconSize }
                });

                    // Danger zone
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = "Xóa dự án",
                        Tag = projectModel,
                        Command = _viewModel?.DeleteProjectCommand,
                        CommandParameter = projectModel,
                        ToolTip = "⚠️ Xóa dự án (Delete)",
                        Icon = new TextBlock { Text = "🗑️", FontSize = fontIconSize },
                        Foreground = new SolidColorBrush(Colors.Red),
                    });

                // Show context menu
                contextMenu.PlacementTarget = target;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectModel] Error showing enhanced context menu: {ex.Message}");
            }
        }

        #endregion

        #region Hierarchy Context Menu Actions

        /// <summary>
        /// ✅ NEW: Move project to root (no parent)
        /// </summary>
        private async Task MoveToRoot_Click(ProjectModel project)
        {
            try
            {
                if (project == null) return;

                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn di chuyển dự án '{project.ProjectName}' lên cấp gốc?\n\n" +
                    "Dự án sẽ không còn thuộc về dự án cha nào nữa.",
                    "Xác nhận di chuyển",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var success = await _viewModel.MoveProjectToParentAsync(project.Id, null, "Moved to root level");

                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully moved project {project.Id} to root");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error moving project to root: {ex.Message}");
                MessageBox.Show($"Lỗi di chuyển dự án: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ NEW: Move project to another parent
        /// </summary>
        private async Task MoveToParent_Click(ProjectModel project, ProjectModel newParent)
        {
            try
            {
                if (project == null || newParent == null) return;

                // Validate move
                if (project.Id == newParent.Id)
                {
                    MessageBox.Show("Không thể di chuyển dự án vào chính nó!", "Lỗi",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if would create circular reference
                if (IsDescendantOf(newParent, project))
                {
                    MessageBox.Show(
                        $"Không thể di chuyển!\n\n" +
                        $"Dự án '{newParent.ProjectName}' là con/cháu của '{project.ProjectName}'.\n" +
                        "Di chuyển sẽ tạo ra vòng lặp phân cấp (circular reference).",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn di chuyển dự án:\n\n" +
                    $"  '{project.ProjectName}'\n\n" +
                    $"vào dự án:\n\n" +
                    $"  '{newParent.ProjectName}'?\n\n" +
                    $"Dự án sẽ trở thành dự án con của '{newParent.ProjectName}'.",
                    "Xác nhận di chuyển",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var success = await _viewModel.MoveProjectToParentAsync(
                        project.Id,
                        newParent.Id,
                        $"Moved under '{newParent.ProjectName}'");

                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully moved project {project.Id} under {newParent.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error moving project to parent: {ex.Message}");
                MessageBox.Show($"Lỗi di chuyển dự án: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ NEW: Check if a project is descendant of another (để prevent circular reference)
        /// </summary>
        private bool IsDescendantOf(ProjectModel potentialDescendant, ProjectModel ancestor)
        {
            if (potentialDescendant == null || ancestor == null)
                return false;

            // Check if potentialDescendant is in the children tree of ancestor
            return CheckDescendantRecursive(potentialDescendant, ancestor);
        }

        private bool CheckDescendantRecursive(ProjectModel current, ProjectModel ancestor)
        {
            if (current == null) return false;

            // Check direct children
            if (ancestor.Children != null)
            {
                foreach (var child in ancestor.Children)
                {
                    if (child.Id == current.Id)
                        return true;

                    // Check descendants recursively
                    if (CheckDescendantRecursive(current, child))
                        return true;
                }
            }

            return false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Show keyboard shortcuts help dialog
        /// </summary>
        private void ShowKeyboardShortcutsHelp()
        {
            var message = @"⌨️ Keyboard Shortcuts - Projects

Navigation:
  ↑/↓      Navigate through projects
  Space    Expand/Collapse children
  Enter    View project details
  Escape   Clear selection

Project Actions:
  Ctrl+N   Add new project
  Ctrl+E   Edit selected project
  Ctrl+Enter   Add child project
  Delete   Delete selected project

View Actions:
  Ctrl+T   View project tasks
  Ctrl+M   View project members

System:
  F5       Refresh projects list
  F1       Show this help";

            MessageBox.Show(message, "Keyboard Shortcuts",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Find visual child of specific type
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Find visual parent of specific type
        /// </summary>
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }

        #endregion

        #region Permission Helpers Methods

        /// <summary>
        /// Helper method để convert bool thành Visibility
        /// </summary>
        private Visibility GetMenuItemVisibility(bool canShow)
        {
            return canShow ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Check if current user can edit project
        /// </summary>
        public async Task<bool> CanCurrentUserEditProject(ProjectModel projectModel)
        {
            if (projectModel == null) return false;

            return await PermissionProject.HasPermissionManagerProjectOfProject(projectModel.Id);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refresh projects list
        /// </summary>
        public async Task RefreshAsync()
        {
            try
            {
                SaveScrollPosition();
                await _viewModel.LoadProjectsAsync();
                await RestoreScrollPositionAfterRefreshAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing: {ex.Message}");
            }
        }

        /// <summary>
        /// Get loading status info
        /// </summary>
        public string GetLoadingStatus()
        {
            return $"Loaded {_loadedPagesCount} pages ({_viewModel?.Projects?.Count ?? 0} projects). " +
                   $"Can load more: {_viewModel.CanLoadMore}. Currently loading: {_isLoadingMore}";
        }

        #endregion
    }
}
