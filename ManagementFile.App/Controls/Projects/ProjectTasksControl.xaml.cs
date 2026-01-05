using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Projects.PermissionProjects;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Controls.Projects;
using ManagementFile.App.ViewModels.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ManagementFile.App.Controls.Projects
{
    /// <summary>
    /// Interaction logic for ProjectTasksControl.xaml
    /// ✅ Enhanced with lazy loading and hierarchy support
    /// </summary>
    public partial class ProjectTasksControl : UserControl
    {
        #region Private Fields

        private readonly ProjectTasksControlViewModel _viewModel;

        // Lazy Loading Fields (similar to ProjectsControl)
        private ScrollViewer _internalScrollViewer;
        private bool _isLoadingMore = false;
        private double _lastScrollOffset = 0;
        private double _scrollPositionBeforeRefresh = 0;

        // Hierarchy Loading Cache
        private Dictionary<int, (DateTime LastLoaded, int ChildrenCount)> _loadedChildrenCache
            = new Dictionary<int, (DateTime, int)>();
        private Dictionary<int, int> _childPagesLoaded = new Dictionary<int, int>();

        #endregion

        #region Constructor

        public ProjectTasksControl(ProjectTasksControlViewModel viewModel)
        {
            InitializeComponent();

            this._viewModel = viewModel;
            DataContext = _viewModel;

            // Setup event handlers
            TasksList.PreviewMouseLeftButtonDown += TasksList_PreviewMouseLeftButtonDown;
            TasksList.MouseRightButtonUp += TasksList_MouseRightButtonUp;
            TasksList.MouseDoubleClick += TasksList_MouseDoubleClick;
            TasksList.KeyDown += TasksList_KeyDown;
            TasksList.Loaded += TasksList_Loaded;

            System.Diagnostics.Debug.WriteLine("ProjectTasksControl initialized with lazy loading support");
        }

        #endregion

        #region DataGrid Event Handlers

        /// <summary>
        /// Handle DataGrid loaded to find ScrollViewer
        /// </summary>
        private void TasksList_Loaded(object sender, RoutedEventArgs e)
        {
            if (_internalScrollViewer == null)
            {
                // Subscribe to LayoutUpdated để đợi Visual Tree hoàn chỉnh
                TasksList.LayoutUpdated += TasksList_LayoutUpdated;
            }
        }

        /// <summary>
        /// Find ScrollViewer in Visual Tree
        /// </summary>
        private void TasksList_LayoutUpdated(object sender, EventArgs e)
        {
            if (_internalScrollViewer != null)
                return;

            _internalScrollViewer = FindVisualChild<ScrollViewer>(TasksList);

            if (_internalScrollViewer != null)
            {
                // Unsubscribe sau khi tìm thấy
                TasksList.LayoutUpdated -= TasksList_LayoutUpdated;

                // Attach scroll event
                _internalScrollViewer.ScrollChanged += InternalScrollViewer_ScrollChanged;
                System.Diagnostics.Debug.WriteLine("✓ Internal ScrollViewer found for TasksList");
            }
        }

        public void SetProject(ProjectModel project)
        {
            _viewModel.SelectedProject = project;
        }

        private void TasksList_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_viewModel.SelectedTask is ProjectTaskModel selectedTask)
                {
                    var isCtrlPressed = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);

                    switch (e.Key)
                    {
                        case Key.Space:
                            // Toggle expand/collapse
                            if (selectedTask.HasSubTasks)
                            {
                                ToggleTaskExpansion(selectedTask);
                            }
                            e.Handled = true;
                            break;

                        case Key.Enter:
                            if (isCtrlPressed)
                            {
                                // Ctrl+Enter: Add subtask
                                _viewModel?.AddSubTaskCommand?.Execute(selectedTask);
                            }
                            else
                            {
                                // Enter: View details
                                _viewModel?.ViewTaskDetailsCommand?.Execute(selectedTask);
                            }
                            e.Handled = true;
                            break;

                        case Key.N:
                            if (isCtrlPressed)
                            {
                                // Ctrl+N: New task
                                _viewModel?.AddTaskCommand?.Execute(null);
                                e.Handled = true;
                            }
                            break;

                        case Key.E:
                            if (isCtrlPressed)
                            {
                                // Ctrl+E: Edit
                                _viewModel?.EditTaskCommand?.Execute(selectedTask);
                                e.Handled = true;
                            }
                            break;

                        case Key.Delete:
                            // Delete task
                            _viewModel?.DeleteTaskCommand?.Execute(selectedTask);
                            e.Handled = true;
                            break;

                        case Key.F5:
                            // F5: Refresh
                            _viewModel?.RefreshTasksCommand?.Execute(null);
                            e.Handled = true;
                            break;

                        case Key.Escape:
                            // Escape: Clear selection
                            TasksList.SelectedItem = null;
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

        private void TasksList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid?.SelectedItem is ProjectTaskModel task)
                {
                    _viewModel?.ViewTaskDetailsCommand?.Execute(task);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MouseDoubleClick: {ex.Message}");
            }
        }

        private void TasksList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid == null) return;

                var hitTest = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
                var row = FindVisualParent<DataGridRow>(hitTest.VisualHit);

                if (row?.Item is ProjectTaskModel projectTaskModel)
                {
                    // Select the row
                    dataGrid.SelectedItem = projectTaskModel;
                    _viewModel.SelectedTask = projectTaskModel;

                    // Show enhanced context menu
                    ShowEnhancedContextMenu(projectTaskModel, row);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectTaskModel] Error in CommentsDataGrid_MouseRightButtonUp: {ex.Message}");
            }
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

        #region Filter Event Handlers


        #endregion

        #region Lazy Loading Methods (- Similar to ProjectsControl)

        /// <summary>
        /// Handle scroll changed event for lazy loading
        /// Pattern copied from ProjectsControl
        /// </summary>
        private async void InternalScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (_isLoadingMore || _viewModel == null || !_viewModel.CanLoadMoreTasks)
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
                    System.Diagnostics.Debug.WriteLine($"📜 [TasksControl] Near bottom - Loading more tasks...");
                    System.Diagnostics.Debug.WriteLine($"   Current page: {_viewModel.CurrentPage}");
                    System.Diagnostics.Debug.WriteLine($"   Tasks loaded: {_viewModel.ProjectTasks?.Count ?? 0}");

                    await LoadMoreTasksAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in scroll changed handler: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NEW: Check if scrolled to bottom
        /// Pattern copied from ProjectsControl
        /// </summary>
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
        /// Load more tasks when scrolling
        /// Pattern adapted from ProjectsControl.LoadMoreProjectsAsync
        /// </summary>
        private async Task LoadMoreTasksAsync()
        {
            if (_isLoadingMore || _viewModel?.SelectedProject == null || !_viewModel.CanLoadMoreTasks)
            {
                System.Diagnostics.Debug.WriteLine($"⏸️ Skip loading: IsLoadingMore={_isLoadingMore}, HasProject={_viewModel?.SelectedProject != null}");
                return;
            }

            try
            {
                _isLoadingMore = true;
                ShowLoadingMoreIndicator();

                var nextPage = _viewModel.CurrentPage + 1;
                System.Diagnostics.Debug.WriteLine($"📥 [TasksControl] Loading page {nextPage} of tasks (PageSize: 20)...");

                // Load next page of tasks
                var newTasks = await _viewModel.LoadMoreProjectsTaskAsync(nextPage, _viewModel.PageSize);

                if (newTasks != null && newTasks.Count > 0)
                {
                    var addedCount = 0;

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        foreach (var task in newTasks)
                        {

                            _viewModel.ProjectTasks.Add(task);
                            addedCount++;

                            // Track child loading state
                            if (task.HasSubTasks)
                            {
                                _loadedChildrenCache[task.Id] = (DateTime.UtcNow, task.SubTasks.Count);
                                _childPagesLoaded[task.Id] = 1;
                            }
                        }

                        // Apply filter to update FilteredTasks
                        _viewModel.RefreshFlattenedTasks();
                    });

                    // Update pagination state
                    if (newTasks.Count < _viewModel.PageSize)
                    {
                        _viewModel.CanLoadMoreTasks = false;

                    }
                    else
                    {
                        // Kiểm tra xem total tasks đã đạt giới hạn chưa
                        if (_viewModel.TotalPages > 0 && _viewModel.CurrentPage >= _viewModel.TotalPages)
                        {
                            _viewModel.CanLoadMoreTasks = false;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ [TasksControl] Loaded {addedCount} new tasks");
                    System.Diagnostics.Debug.WriteLine($"📊 [TasksControl] State: Page={_viewModel.CurrentPage}, Total Tasks={_viewModel.ProjectTasks?.Count ?? 0}");

                    // Restore scroll position
                    await RestoreScrollPositionAsync();
                }
                else
                {
                    // No tasks returned -> end of list
                    _viewModel.CanLoadMoreTasks = false;
                    System.Diagnostics.Debug.WriteLine($"🛑 [TasksControl] No more tasks available (empty result)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [TasksControl] Error loading more tasks: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải thêm tasks: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingMore = false;
                HideLoadingMoreIndicator();

                System.Diagnostics.Debug.WriteLine($"🏁 Loading complete. Page={_viewModel.CurrentPage}");
            }
        }

        #endregion

        #region Hierarchy Management (✅ Enhanced)

        /// <summary>
        /// Handle expand/collapse button click
        /// </summary>
        private void ExpandChildrenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var task = button?.Tag as ProjectTaskModel;

                // Check if task is null (was incorrectly checking != null)
                if (task == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ ExpandChildrenButton_Click: task is null");
                    return;
                }

                // Prevent multiple clicks while loading
                if (task.IsLoadingSubTasks)
                {
                    System.Diagnostics.Debug.WriteLine($"⏸️ Already loading subtasks for task {task.Id}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔘 Expand button clicked for task {task.Id} (IsExpanded: {task.IsExpanded})");

                // Toggle expansion
                ToggleTaskExpansion(task);

                // Update button content immediately
                button.Content = task.ExpandCollapseIcon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in ExpandChildrenButton_Click: {ex.Message}");
                MessageBox.Show($"Lỗi khi mở/đóng subtasks: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void TasksList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);

                if (row?.Item is ProjectTaskModel task)
                {
                    // Check if Load More row
                    if (_viewModel.IsLoadMorePlaceholder(task))
                    {
                        // Handle single click on Load More row
                        var parentTaskId = _viewModel.GetParentTaskIdFromPlaceholder(task);
                        var parentTask = FindTaskById(_viewModel.ProjectTasks, parentTaskId);

                        if (parentTask != null && !parentTask.IsLoadingSubTasks)
                        {
                            int currentLoaded = parentTask.SubTasks?.Count ?? 0;
                            int nextPage = (currentLoaded / 20) + 1;

                            _ = LoadSubTasksForTaskAsync(parentTask, page: nextPage, pageSize: _viewModel.PageSize);
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

        private ProjectTaskModel FindTaskById(IEnumerable<ProjectTaskModel> tasks, int taskId)
        {
            if (tasks == null) return null;

            foreach (var task in tasks)
            {
                if (task.Id == taskId)
                    return task;

                var found = FindTaskById(task.SubTasks, taskId);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Toggle task expansion
        /// </summary>
        private void ToggleTaskExpansion(ProjectTaskModel task)
        {
            if (task == null) return;

            try
            {
                var wasExpanded = task.IsExpanded;

                if (wasExpanded)
                {
                    // Collapse - hide subtasks
                    System.Diagnostics.Debug.WriteLine($"📁 Collapsing task {task.Id}");
                    task.IsExpanded = false;

                    // Refresh flattened tasks để ẩn subtasks
                    _viewModel?.RefreshFlattenedTasks();

                    System.Diagnostics.Debug.WriteLine($"✅ Collapsed task {task.Id}");
                    return;
                }

                // Expand
                System.Diagnostics.Debug.WriteLine($"📂 Expanding task {task.Id}");
                task.IsExpanded = true;

                // Load subtasks with pageSize = 20 for first page
                if (task.SubTasks.Count == 0 && task.HasSubTasks)
                {
                    System.Diagnostics.Debug.WriteLine($"📥 Loading first 20 subtasks for task {task.Id}...");
                    _ = LoadSubTasksForTaskAsync(task, page: 1, pageSize: _viewModel.PageSize);
                }
                else
                {
                    // Subtasks already loaded, just refresh display
                    System.Diagnostics.Debug.WriteLine($"♻️ Subtasks already loaded for task {task.Id}, refreshing display");
                    _viewModel?.RefreshFlattenedTasks();
                }

                System.Diagnostics.Debug.WriteLine($"✅ Expanded task {task.Id}: IsExpanded = {task.IsExpanded}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error toggling expansion: {ex.Message}");
            }
        }

        /// <summary>
        /// Load subtasks for a specific task with pagination support
        /// Pattern adapted from ProjectsControl.LoadChildrenForProjectAsync
        /// </summary>
        private async Task LoadSubTasksForTaskAsync(ProjectTaskModel parentTask, int page = 1, int pageSize = 20)
        {
            if (parentTask == null || _viewModel?.SelectedProject == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"📥 Loading subtasks for task {parentTask.Id} (page {page}, pageSize {pageSize})...");

                parentTask.IsLoadingSubTasks = true;

                var subtasks = await _viewModel.GetSubTaskModelPageAsync(
                    _viewModel.SelectedProject.Id,
                    parentTask.Id,
                    page,
                    pageSize);

                if (subtasks != null && subtasks.Count > 0)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        // Clear old subtasks if loading first page
                        if (page == 1)
                        {
                            parentTask.SubTasks.Clear();
                        }

                        System.Diagnostics.Debug.WriteLine($"📊 Paging subtasks: Total={subtasks.Count}, Page={page}, PageSize={pageSize}");

                        foreach (var subtaskModel in subtasks)
                        {
                            // Set hierarchy level
                            subtaskModel.HierarchyLevel = parentTask.HierarchyLevel + 1;
                            parentTask.SubTasks.Add(subtaskModel);

                            // Track child cache
                            if (subtaskModel.HasSubTasks)
                            {
                                _loadedChildrenCache[subtaskModel.Id] = (DateTime.UtcNow, subtaskModel.SubTasks.Count);
                                _childPagesLoaded[subtaskModel.Id] = 1;
                            }
                        }

                        // Update cache for parent task
                        _loadedChildrenCache[parentTask.Id] = (DateTime.UtcNow, parentTask.SubTasks.Count);
                        _childPagesLoaded[parentTask.Id] = page;

                        // Update "Can Load More" status
                        var totalSubTasks = subtasks.Count;
                        var loadedSubTasks = parentTask.SubTasks.Count;
                        var hasMoreSubTasks = loadedSubTasks < totalSubTasks;

                        System.Diagnostics.Debug.WriteLine($"📈 SubTasks status: Loaded={loadedSubTasks}, Total={totalSubTasks}, HasMore={hasMoreSubTasks}");

                        // Refresh flattened tasks để hiển thị subtasks và "Load More" button
                        _viewModel?.RefreshFlattenedTasks();
                    });

                    System.Diagnostics.Debug.WriteLine($"✅ Loaded {subtasks.Count} subtasks for task {parentTask.Id} (page {page})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No subtasks found for task {parentTask.Id} on page {page}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading subtasks for task {parentTask.Id}: {ex.Message}");
                MessageBox.Show($"Lỗi load subtasks: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                parentTask.IsLoadingSubTasks = false;
            }
        }

        #endregion

        #region Scroll Position Management

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
                // TODO: Show loading indicator UI (if exists in XAML)
                // LoadingMoreIndicator.Visibility = Visibility.Visible;
            });
        }

        private void HideLoadingMoreIndicator()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // TODO: Hide loading indicator UI (if exists in XAML)
                // LoadingMoreIndicator.Visibility = Visibility.Collapsed;
            });
        }

        #endregion

        #region Context Menu

        private async Task ShowEnhancedContextMenu(ProjectTaskModel projectTaskModel, FrameworkElement target)
        {
            try
            {
                int fontIconSize = 14;
                var contextMenu = new ContextMenu
                {
                    Style = TryFindResource("TabContextMenuStyle") as Style
                };

                // Basic actions
                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "Thêm mới công việc",
                        Tag = projectTaskModel,
                        Command = _viewModel?.AddTaskCommand,
                        ToolTip = "Thêm mới công việc (Ctrl+N)",
                        Icon = new TextBlock
                        {
                            Text = "➕",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });

                
                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "Chỉnh sửa công việc",
                        Tag = projectTaskModel,
                        Command = _viewModel?.EditTaskCommand,
                        ToolTip = "Chỉnh sửa thông tin công việc (Ctrl+E). Chỉ những người thực hiện mới được chỉnh sửa.",
                        Icon = new TextBlock
                        {
                            Text = "✏️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });

                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "Xem chi tiết công việc",
                        Tag = projectTaskModel,
                        Command = _viewModel?.ViewTaskDetailsCommand,
                        ToolTip = "Xem chi tiết thông tin công việc (Enter)",
                        Icon = new TextBlock
                        {
                            Text = "👁️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });

                // Add SubTask (Hierarchy)
                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "Thêm công việc con",
                        Tag = projectTaskModel,
                        Command = _viewModel?.AddSubTaskCommand,
                        ToolTip = "Thêm công việc con cho công việc này (Ctrl+Enter)",
                        IsEnabled = projectTaskModel != null,
                        Icon = new TextBlock
                        {
                            Text = "📁",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });

                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "Xóa task",
                        Tag = projectTaskModel,
                        Command = _viewModel?.DeleteTaskCommand,
                        ToolTip = "Xóa task này (Delete). Chỉ có người quản lý, thực hiện mới xóa được.",
                        Foreground = new SolidColorBrush(Colors.Red),
                        Icon = new TextBlock
                        {
                            Text = "🗑️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                    });

                // cap nhat tien do
                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "Cập nhật tiến độ",
                        Tag = projectTaskModel,
                        Command = _viewModel?.UpdateTaskProgressCommand,
                        ToolTip = "Cập nhật tiến độ hoàn thành của task",
                        Icon = new TextBlock
                        {
                            Text = "📈",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }

                    });

                contextMenu.Items.Add(new Separator());

                // View Comments
                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = $"Xem bình luận ({projectTaskModel?.CommentCount ?? 0})",
                        Tag = projectTaskModel,
                        Command = _viewModel?.ViewTaskCommentCommand,
                        CommandParameter = projectTaskModel,
                        ToolTip = "Xem danh sách bình luận của công việc",
                        Icon = new TextBlock
                        {
                            Text = "💬",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });

                contextMenu.Items.Add(new Separator());

                // Refresh Section
                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "Load lại danh sách",
                        Tag = projectTaskModel,
                        Command = _viewModel?.RefreshTasksCommand,
                        CommandParameter = projectTaskModel,
                        ToolTip = "Load lại danh sách công việc (F5)",
                        Icon = new TextBlock
                        {
                            Text = "🔄",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });

                // Show context menu 
                contextMenu.PlacementTarget = target;
                contextMenu.IsOpen = true;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectTaskModel] Error showing enhanced context menu: {ex.Message}");

                // Fallback to simple context menu
                ShowSimpleContextMenu(projectTaskModel, target);
            }
        }

        /// <summary>
        /// Simple fallback context menu
        /// </summary>
        private void ShowSimpleContextMenu(ProjectTaskModel projectTaskModel, FrameworkElement target)
        {
            try
            {
                var contextMenu = new ContextMenu();

                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "✏️ Chỉnh sửa",
                        Command = _viewModel?.EditTaskCommand,
                        CommandParameter = projectTaskModel
                    });

                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "💬 Bình luận",
                        Command = _viewModel?.ViewTaskCommentCommand,
                        CommandParameter = projectTaskModel
                    });

                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "🔄 Refresh",
                        Command = _viewModel?.RefreshTasksCommand
                    });

                contextMenu.PlacementTarget = target;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectTaskModel] Error showing simple context menu: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

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

        #endregion

        #region Public Methods

        /// <summary>
        /// ✅ NEW: Get loading status info
        /// </summary>
        public string GetLoadingStatus()
        {
            return $"Loaded {_viewModel.CurrentPage} pages ({_viewModel?.ProjectTasks?.Count ?? 0} tasks). " +
                   $"Currently loading: {_isLoadingMore}";
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
        /// Check if user có thể change priority hoặc status
        /// </summary>
        private async Task<bool> CanUserChangePriorityOrStatus(ProjectModel projectModel)
        {
            if (projectModel == null) return false;

            return await PermissionProject.HasPermissionManagerProjectOfProject(projectModel.Id);
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

    }
}
