using ManagementFile.App.Configuration.Projects;
using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Projects.PermissionProjects;
using ManagementFile.App.ViewModels;
using ManagementFile.App.ViewModels.Controls;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ManagementFile.App.Controls
{
    /// <summary>
    /// TaskCommentsControl - UserControl độc lập với ViewModel riêng
    /// </summary>
    public partial class TaskCommentsControl : UserControl
    {

        /// <summary>
        /// Current column configurations - Lưu trạng thái hiện tại
        /// </summary>
        private List<ColumnConfig> _currentColumnConfigs;

        /// <summary>
        /// Column visibility change log - Lưu lại các thay đổi
        /// </summary>
        private List<ColumnVisibilityChange> _columnVisibilityChanges;


        #region Private Fields

        private TaskCommentsControlViewModel _viewModel;
        private bool _isInitialized = false;

        #endregion

        #region Reply Management Fields

        /// <summary>
        /// Track which comments have their replies fully loaded
        /// Key: CommentId, Value: (LastLoadedTime, TotalRepliesLoaded)
        /// </summary>
        private Dictionary<int, (DateTime LastLoaded, int RepliesCount)> _loadedRepliesCache
            = new Dictionary<int, (DateTime, int)>();

        /// <summary>
        /// Track reply pages loaded for each parent comment
        /// Key: ParentCommentId, Value: LoadedPageNumber
        /// </summary>
        private Dictionary<int, int> _replyPagesLoaded = new Dictionary<int, int>();

        #endregion

        #region Lazy Loading Fields

        private ScrollViewer _internalScrollViewer;

        /// <summary>
        /// Đang load thêm comments
        /// </summary>
        private bool _isLoadingMore = false;

        /// <summary>
        /// Có thể load thêm comments không
        /// </summary>
        private bool _canLoadMore = true;

        /// <summary>
        /// Current page for root comments
        /// </summary>
        private int _currentRootPage = 1;

        /// <summary>
        /// NEW: Track how many pages were loaded before refresh
        /// </summary>
        private int _loadedPagesCount = 1;

        /// <summary>
        /// Page size for loading
        /// </summary>
        private int PageSize;

        /// <summary>
        /// Threshold để trigger load more (pixels from bottom)
        /// </summary>
        private const double LoadMoreThreshold = 100;

        /// <summary>
        /// Cache loaded comment IDs to prevent duplicates
        /// </summary>
        private HashSet<int> _loadedCommentIds = new HashSet<int>();

        /// <summary>
        /// Last scroll position
        /// </summary>
        private double _lastScrollOffset = 0;

        /// <summary>
        /// NEW: Store scroll position before refresh
        /// </summary>
        private double _scrollPositionBeforeRefresh = 0;


        #endregion

        #region Popup Fields

        private Popup _columnVisibilityPopup;
        private TextBox _columnSearchBox;
        private ComboBox _commentTypeFilter;
        private ComboBox _statusFilter;
        private ComboBox _priorityFilter;
        private StackPanel _activeFiltersPanel;
        private TextBlock _activeFiltersText;
        private Button _clearFiltersButton;
        private StackPanel _columnsStackPanel;
        private TextBlock _columnCountInfo;

        // Filter state
        private string _columnSearchText = "";
        private string _selectedCommentTypeFilter = "All";
        private string _selectedStatusFilter = "All";
        private string _selectedPriorityFilter = "All";
        private Dictionary<string, bool> _originalCheckBoxVisibility = new Dictionary<string, bool>();

        #endregion


        #region Events

        /// <summary>
        /// Event được fire khi có comment action hoàn thành
        /// </summary>
        public event EventHandler<CommentActionEventArgs> CommentActionCompleted;

        #endregion

        #region Constructor

        public TaskCommentsControl(TaskCommentsControlViewModel viewModel)
        {
            InitializeComponent();

            // Load column preferences before setup
            LoadColumnConfigurations();

            SetupColumnVisibilityControls();
            SetupPerformanceOptimizations();

            InitializeColumnVisibilityPopup();

            // Add event handlers for enhanced functionality
            CommentsDataGrid.MouseRightButtonUp += CommentsDataGrid_MouseRightButtonUp;
            CommentsDataGrid.MouseDoubleClick += CommentsDataGrid_MouseDoubleClick;
            CommentsDataGrid.KeyDown += CommentsDataGrid_KeyDown;

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
            // Subscribe vào events
            _viewModel.CommentActionCompleted += OnCommentActionCompleted;
            PageSize = _viewModel.PageSize;

            // Đợi control loaded rồi mới initialize ViewModel
            this.Loaded += TaskCommentsControl_Loaded;

            System.Diagnostics.Debug.WriteLine("TaskCommentsControl initialized with enhanced features");

        }

        public void SetTask(ProjectTaskModel projectTaskModel)
        {
            // Initialize ViewModel
            _viewModel.InitializeAsync(
                projectTaskModel.Id,
                projectTaskModel.ProjectId,
                isViewMode: false,
                isNewTask: projectTaskModel.Id <= 0).ConfigureAwait(false);
        }

        #endregion

        #region Event Handlers

        private void TaskCommentsControl_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        /// <summary>
        /// Handle event từ ViewModel
        /// </summary>
        private void OnCommentActionCompleted(object sender, CommentActionEventArgs e)
        {
            // Forward event lên parent nếu có
            CommentActionCompleted?.Invoke(this, e);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize column configurations
        /// </summary>
        private void InitializeColumnConfigurations()
        {
            try
            {
                // Load default configurations
                _currentColumnConfigs = TaskCommentColumnConfigurationManager.GetDefaultColumnConfigurations();
                _columnVisibilityChanges = new List<ColumnVisibilityChange>();

                // Apply configurations to checkboxes
                ApplyConfigurationsToCheckBoxes();

                // Apply initial visibility to DataGrid columns
                ApplyInitialColumnVisibility();

                System.Diagnostics.Debug.WriteLine($"Initialized {_currentColumnConfigs.Count} column configurations");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing column configurations: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply configurations to checkboxes
        /// </summary>
        private void ApplyConfigurationsToCheckBoxes()
        {
            try
            {
                foreach (var config in _currentColumnConfigs)
                {
                    var checkBox = FindName(config.CheckBoxName) as CheckBox;
                    if (checkBox != null)
                    {
                        // Set checkbox state based on configuration
                        checkBox.IsChecked = config.IsVisibleByDefault;

                        // Update display text if specified
                        if (!string.IsNullOrEmpty(config.DisplayName))
                        {
                            checkBox.Content = config.DisplayName;
                        }

                        // Disable checkbox if essential
                        if (config.IsEssential)
                        {
                            checkBox.IsEnabled = false;
                            checkBox.ToolTip = "Cột này không thể ẩn";
                        }

                        System.Diagnostics.Debug.WriteLine($"Applied config for {config.CheckBoxName}: IsChecked = {checkBox.IsChecked}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying configurations to checkboxes: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply initial column visibility based on configurations
        /// </summary>
        private void ApplyInitialColumnVisibility()
        {
            try
            {
                foreach (var config in _currentColumnConfigs)
                {
                    ToggleColumnVisibility(config.ColumnName, config.IsVisibleByDefault);
                }

                System.Diagnostics.Debug.WriteLine("Applied initial column visibility");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying initial column visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current column configuration by checkbox name
        /// </summary>
        private ColumnConfig GetColumnConfigByCheckBox(string checkBoxName)
        {
            return _currentColumnConfigs?.FirstOrDefault(c => c.CheckBoxName == checkBoxName);
        }

        /// <summary>
        /// Update column configuration when checkbox changes
        /// </summary>
        private void UpdateColumnConfiguration(string checkBoxName, bool isVisible)
        {
            try
            {
                var config = GetColumnConfigByCheckBox(checkBoxName);
                if (config != null)
                {
                    // Log the change
                    var change = new ColumnVisibilityChange
                    {
                        ColumnName = config.ColumnName,
                        CheckBoxName = checkBoxName,
                        OldValue = config.IsVisibleByDefault,
                        NewValue = isVisible,
                        ChangedAt = DateTime.Now,
                        ChangedBy = App.GetCurrentUser().Username // Current user
                    };

                    _columnVisibilityChanges.Add(change);

                    // Update the configuration
                    config.IsVisibleByDefault = isVisible;

                    System.Diagnostics.Debug.WriteLine($"Column configuration updated: {config.ColumnName} = {isVisible}");

                    // Optional: Auto-save configurations
                    SaveColumnConfigurations();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating column configuration: {ex.Message}");
            }
        }

        #endregion

        #region Lazy Loading Setup

        /// <summary>
        /// QUAN TRỌNG: Tìm và attach vào internal ScrollViewer khi DataGrid loaded
        /// </summary>
        private void CommentsDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (_internalScrollViewer == null)
            {
                // Subscribe to LayoutUpdated để đợi Visual Tree hoàn chỉnh
                CommentsDataGrid.LayoutUpdated += CommentsDataGrid_LayoutUpdated;
            }
        }


        private void CommentsDataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            if (_internalScrollViewer != null)
                return;

            _internalScrollViewer = FindVisualChild<ScrollViewer>(CommentsDataGrid);

            if (_internalScrollViewer != null)
            {
                // Unsubscribe sau khi tìm thấy
                CommentsDataGrid.LayoutUpdated -= CommentsDataGrid_LayoutUpdated;

                // Attach scroll event
                _internalScrollViewer.ScrollChanged += InternalScrollViewer_ScrollChanged;
                System.Diagnostics.Debug.WriteLine("✓ Internal ScrollViewer found via LayoutUpdated");
            }
        }

        /// <summary>
        /// Tìm child control theo type trong Visual Tree
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
        /// Handle scroll changed event for lazy loading
        /// </summary>
        private async void InternalScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (_isLoadingMore || !_canLoadMore || _viewModel == null)
                    return;

                var scrollViewer = sender as ScrollViewer;
                if (scrollViewer == null)
                    return;

                // Calculate distance from bottom
                var distanceFromBottom = scrollViewer.ScrollableHeight - scrollViewer.VerticalOffset;

                // Store current scroll offset
                _lastScrollOffset = scrollViewer.VerticalOffset;

                // Debug info
                System.Diagnostics.Debug.WriteLine(
                    $"Scroll: Offset={scrollViewer.VerticalOffset:F1}, " +
                    $"Height={scrollViewer.ScrollableHeight:F1}, " +
                    $"FromBottom={distanceFromBottom:F1}");

                // Trigger load more when within threshold
                if (distanceFromBottom <= LoadMoreThreshold)
                {
                    System.Diagnostics.Debug.WriteLine($"Scroll threshold reached. Loading more comments... (Page: {_currentRootPage + 1})");
                    await LoadMoreCommentsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in scroll changed handler: {ex.Message}");
            }
        }

        /// <summary>
        /// Load more comments when scrolling
        /// </summary>
        private async Task LoadMoreCommentsAsync()
        {
            if (_isLoadingMore || !_canLoadMore)
                return;

            try
            {
                _isLoadingMore = true;
                ShowLoadingMoreIndicator();

                System.Diagnostics.Debug.WriteLine($"Loading page {_currentRootPage + 1} of comments...");

                // Load next page via ViewModel
                var newComments = await _viewModel.LoadMoreCommentsAsync(_currentRootPage + 1, PageSize);

                if (newComments != null && newComments.Count > 0)
                {
                    var addedCount = 0;

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        foreach (var comment in newComments)
                        {
                            // Check for duplicates
                            if (!_loadedCommentIds.Contains(comment.Id))
                            {
                                _loadedCommentIds.Add(comment.Id);
                                _viewModel.TaskComments.Add(comment);
                                addedCount++;

                                // NEW: Track reply loading state for this comment
                                if (comment.HasReplies)
                                {
                                    _loadedRepliesCache[comment.Id] = (DateTime.UtcNow, comment.Replies.Count);
                                    _replyPagesLoaded[comment.Id] = 1; // First page of replies loaded
                                }
                            }
                        }

                        // Refresh flattened hierarchy
                        _viewModel.RefreshFlattenedComments();
                    });

                    _currentRootPage++;

                    // Track loaded pages count
                    _loadedPagesCount = Math.Max(_loadedPagesCount, _currentRootPage);

                    System.Diagnostics.Debug.WriteLine($"Loaded {addedCount} new comments (Page {_currentRootPage}, Total pages loaded: {_loadedPagesCount})");

                    // Check if we can load more
                    if (newComments.Count < PageSize)
                    {
                        _canLoadMore = false;
                        System.Diagnostics.Debug.WriteLine("No more comments to load");
                    }

                    // Restore scroll position
                    await RestoreScrollPositionAsync();
                }
                else
                {
                    _canLoadMore = false;
                    System.Diagnostics.Debug.WriteLine("No more comments available");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading more comments: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải thêm bình luận: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingMore = false;
                HideLoadingMoreIndicator();
            }
        }

        /// <summary>
        /// NEW: Load more replies for a specific comment
        /// </summary>
        private async Task LoadMoreRepliesForCommentAsync(TaskCommentModel parentComment)
        {
            if (parentComment == null || !parentComment.HasReplies)
                return;

            try
            {
                // Get current page for this comment's replies
                if (!_replyPagesLoaded.TryGetValue(parentComment.Id, out int currentPage))
                {
                    currentPage = 1;
                }

                int nextPage = currentPage + 1;

                System.Diagnostics.Debug.WriteLine($"Loading page {nextPage} of replies for comment {parentComment.Id}...");

                // Load next page of replies
                var newReplies = await _viewModel.LoadRepliesForCommentAsync(
                    parentComment.Id,
                    nextPage,
                    PageSize);

                if (newReplies != null && newReplies.Count > 0)
                {
                    var addedCount = 0;

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        foreach (var reply in newReplies)
                        {
                            // Check if reply already exists
                            var existingReply = parentComment.Replies.FirstOrDefault(r => r.Id == reply.Id);
                            if (existingReply == null)
                            {
                                parentComment.Replies.Add(reply);
                                addedCount++;

                                // Recursively track nested replies
                                if (reply.HasReplies)
                                {
                                    _loadedRepliesCache[reply.Id] = (DateTime.UtcNow, reply.Replies.Count);
                                    _replyPagesLoaded[reply.Id] = 1;
                                }
                            }
                        }

                        // Update cache
                        _loadedRepliesCache[parentComment.Id] = (DateTime.UtcNow, parentComment.Replies.Count);
                        _replyPagesLoaded[parentComment.Id] = nextPage;

                        // Refresh flattened hierarchy to show new replies
                        _viewModel.RefreshFlattenedComments();
                    });

                    System.Diagnostics.Debug.WriteLine($"Loaded {addedCount} new replies for comment {parentComment.Id} (Page {nextPage})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No more replies for comment {parentComment.Id}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading more replies: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải thêm reply: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// NEW: Reload all previously loaded pages with updated replies
        /// </summary>
        private async Task ReloadAllLoadedPagesAsync()
        {
            if (_viewModel == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Reloading all {_loadedPagesCount} previously loaded pages with replies...");

                SaveScrollPosition();
                _isLoadingMore = true;
                ShowLoadingMoreIndicator();

                var allReloadedComments = new List<TaskCommentModel>();
                var reloadedIds = new HashSet<int>();

                // Phase 1: Load all root comment pages
                for (int page = 1; page <= _loadedPagesCount; page++)
                {
                    System.Diagnostics.Debug.WriteLine($"Reloading root comments page {page}/{_loadedPagesCount}...");

                    var pageComments = await _viewModel.LoadMoreCommentsAsync(page, PageSize);

                    if (pageComments != null && pageComments.Count > 0)
                    {
                        foreach (var comment in pageComments)
                        {
                            if (!reloadedIds.Contains(comment.Id))
                            {
                                reloadedIds.Add(comment.Id);
                                allReloadedComments.Add(comment);
                            }
                        }
                    }

                    await Task.Delay(50);
                }

                // Phase 2: Reload replies for comments that had replies loaded before
                foreach (var comment in allReloadedComments)
                {
                    if (_replyPagesLoaded.TryGetValue(comment.Id, out int previouslyLoadedPages))
                    {
                        System.Diagnostics.Debug.WriteLine($"Reloading {previouslyLoadedPages} pages of replies for comment {comment.Id}...");

                        // Reload all previously loaded reply pages
                        for (int replyPage = 1; replyPage <= previouslyLoadedPages; replyPage++)
                        {
                            var replies = await _viewModel.LoadRepliesForCommentAsync(
                                comment.Id,
                                replyPage,
                                PageSize);

                            if (replies != null && replies.Count > 0)
                            {
                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    // Clear old replies for this page range
                                    if (replyPage == 1)
                                    {
                                        comment.Replies.Clear();
                                    }

                                    foreach (var reply in replies)
                                    {
                                        var existingReply = comment.Replies.FirstOrDefault(r => r.Id == reply.Id);
                                        if (existingReply == null)
                                        {
                                            comment.Replies.Add(reply);
                                        }
                                    }
                                });
                            }

                            await Task.Delay(50);
                        }

                        // Update cache
                        _loadedRepliesCache[comment.Id] = (DateTime.UtcNow, comment.Replies.Count);
                    }
                }

                // Phase 3: Update UI with all reloaded comments
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    _viewModel.TaskComments.Clear();
                    _loadedCommentIds.Clear();

                    foreach (var comment in allReloadedComments)
                    {
                        _loadedCommentIds.Add(comment.Id);
                        _viewModel.TaskComments.Add(comment);
                    }

                    _viewModel.RefreshFlattenedComments();
                });

                _currentRootPage = _loadedPagesCount;

                var lastPageComments = await _viewModel.LoadMoreCommentsAsync(_loadedPagesCount, PageSize);
                _canLoadMore = lastPageComments?.Count >= PageSize;

                System.Diagnostics.Debug.WriteLine($"Successfully reloaded {allReloadedComments.Count} comments with their replies");

                await RestoreScrollPositionAfterRefreshAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading pages: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải lại bình luận: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingMore = false;
                HideLoadingMoreIndicator();
            }
        }

        /// <summary>
        /// Smart refresh - check for new replies on visible comments
        /// </summary>
        private async Task SmartRefreshRepliesAsync()
        {
            if (_viewModel?.TaskComments == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine("Smart refreshing replies for visible comments...");

                var visibleComments = _viewModel.FlattenedComments
                    .Where(c => c.HasReplies || c.ReplyCount > 0)
                    .ToList();

                foreach (var comment in visibleComments)
                {
                    // Check if this comment needs reply refresh
                    if (await ShouldRefreshRepliesForComment(comment))
                    {
                        System.Diagnostics.Debug.WriteLine($"Refreshing replies for comment {comment.Id}...");

                        // Get current page count for this comment
                        int loadedPages = _replyPagesLoaded.TryGetValue(comment.Id, out int pages)
                            ? pages
                            : 1;

                        // Reload all reply pages for this comment
                        comment.Replies.Clear();

                        for (int page = 1; page <= loadedPages; page++)
                        {
                            var replies = await _viewModel.LoadRepliesForCommentAsync(
                                comment.Id,
                                page,
                                PageSize);

                            if (replies != null)
                            {
                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    foreach (var reply in replies)
                                    {
                                        comment.Replies.Add(reply);
                                    }
                                });
                            }
                        }

                        // Update cache
                        _loadedRepliesCache[comment.Id] = (DateTime.UtcNow, comment.Replies.Count);
                    }
                }

                // Refresh UI
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    _viewModel.RefreshFlattenedComments();
                });

                System.Diagnostics.Debug.WriteLine("Smart refresh completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in smart refresh: {ex.Message}");
            }
        }

        /// <summary>
        /// NEW: Check if comment needs reply refresh
        /// </summary>
        private async Task<bool> ShouldRefreshRepliesForComment(TaskCommentModel comment)
        {
            try
            {
                // Always refresh if no cache
                if (!_loadedRepliesCache.TryGetValue(comment.Id, out var cacheInfo))
                    return true;

                // Refresh if older than 1 minute
                if ((DateTime.UtcNow - cacheInfo.LastLoaded).TotalMinutes > 1)
                    return true;

                // Check if reply count changed on server
                var latestComment = await _viewModel.GetCommentByIdAsync(comment.Id);
                if (latestComment != null && latestComment.ReplyCount != cacheInfo.RepliesCount)
                {
                    System.Diagnostics.Debug.WriteLine($"Reply count changed for comment {comment.Id}: {cacheInfo.RepliesCount} → {latestComment.ReplyCount}");
                    return true;
                }

                return false;
            }
            catch
            {
                // On error, refresh to be safe
                return true;
            }
        }

        #endregion

        #region Scroll Position Management

        /// <summary>
        /// Save current scroll position
        /// </summary>
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

        /// <summary>
        /// Restore scroll position after loading more data
        /// </summary>
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

        /// <summary>
        /// Restore scroll position after refresh with retry
        /// </summary>
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

            await Task.Delay(50);
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_internalScrollViewer != null && _scrollPositionBeforeRefresh > 0)
                {
                    _internalScrollViewer.UpdateLayout();
                    _internalScrollViewer.ScrollToVerticalOffset(_scrollPositionBeforeRefresh);
                }
            });
        }

        /// <summary>
        /// Scroll to top
        /// </summary>
        public void ScrollToTop()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _internalScrollViewer?.ScrollToTop();
            });
        }

        /// <summary>
        /// Scroll to bottom
        /// </summary>
        public void ScrollToBottom()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _internalScrollViewer?.ScrollToBottom();
            });
        }

        #endregion

        #region UI Indicators

        /// <summary>
        /// Show loading more indicator
        /// </summary>
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

        /// <summary>
        /// Hide loading more indicator
        /// </summary>
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

        #region State Management

        /// <summary>
        /// Reset lazy loading state
        /// </summary>
        private void ResetLazyLoadingState(bool preserveLoadedPages = false)
        {
            if (!preserveLoadedPages)
            {
                _currentRootPage = 1;
                _loadedPagesCount = 1;
                _loadedRepliesCache.Clear();
                _replyPagesLoaded.Clear();
                System.Diagnostics.Debug.WriteLine("🔄 Lazy loading state reset completely");
            }
            else
            {
                _currentRootPage = _loadedPagesCount;
                System.Diagnostics.Debug.WriteLine(
                    $"🔄 Lazy loading state reset but preserved {_loadedPagesCount} loaded pages");
            }

            _canLoadMore = true;
            _isLoadingMore = false;
            _loadedCommentIds.Clear();
            _lastScrollOffset = 0;
            _scrollPositionBeforeRefresh = 0;
        }

        /// <summary>
        /// Cleanup khi unload
        /// </summary>
        private void Cleanup()
        {
            if (_internalScrollViewer != null)
            {
                _internalScrollViewer.ScrollChanged -= InternalScrollViewer_ScrollChanged;
                _internalScrollViewer = null;
                System.Diagnostics.Debug.WriteLine("🧹 Cleaned up scroll event handler");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refresh dữ liệu comment
        /// </summary>
        public async Task RefreshAsync()
        {
            System.Diagnostics.Debug.WriteLine($"Refresh requested. Currently loaded {_loadedPagesCount} pages with {_viewModel?.TaskComments?.Count ?? 0} comments");

            try
            {
                if (_viewModel == null)
                    return;

                // Option 1: Reload ALL previously loaded pages (RECOMMENDED)
                await ReloadAllLoadedPagesAsync();

                System.Diagnostics.Debug.WriteLine($"Refresh completed. Now showing {_viewModel?.TaskComments?.Count ?? 0} comments");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during refresh: {ex.Message}");
                MessageBox.Show($"Lỗi khi làm mới: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Hard refresh - reset everything to page 1
        /// </summary>
        public async Task HardRefreshAsync()
        {
            System.Diagnostics.Debug.WriteLine("Hard refresh requested - resetting to page 1");

            ResetLazyLoadingState(preserveLoadedPages: false);

            if (_viewModel != null)
            {
                await _viewModel.RefreshAsync();
            }

        }

        /// <summary>
        /// NEW: Light refresh - only check for new replies on visible comments
        /// </summary>
        public async Task LightRefreshAsync()
        {
            System.Diagnostics.Debug.WriteLine("Light refresh requested - checking for new replies only");

            try
            {
                if (_viewModel == null)
                    return;

                await SmartRefreshRepliesAsync();

                System.Diagnostics.Debug.WriteLine("Light refresh completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during light refresh: {ex.Message}");
            }
        }

        #endregion

        #region Public Status Methods

        /// <summary>
        /// Get current loading status info
        /// </summary>
        public string GetLoadingStatus()
        {
            return $"Loaded {_loadedPagesCount} pages ({_viewModel?.TaskComments?.Count ?? 0} comments). " +
                   $"Can load more: {_canLoadMore}. Currently loading: {_isLoadingMore}";
        }

        /// <summary>
        /// Get loaded pages count
        /// </summary>
        public int GetLoadedPagesCount() => _loadedPagesCount;

        /// <summary>
        /// Get loaded comments count
        /// </summary>
        public int GetLoadedCommentsCount() => _viewModel?.TaskComments?.Count ?? 0;

        #endregion

        #region Column Visibility Management - Giữ nguyên

        private void SetupColumnVisibilityControls()
        {
            this.Loaded += (s, e) =>
            {
                try
                {
                    // Initialize configurations first
                    InitializeColumnConfigurations();

                    UpdateColumnVisibilityButtonText();
                    UpdateColumnCountInfo();

                    if (_columnVisibilityPopup != null)
                    {
                        _columnVisibilityPopup.Opened += (sender, args) =>
                        {
                            Mouse.Capture(_columnVisibilityPopup.Child, CaptureMode.SubTree);
                        };

                        _columnVisibilityPopup.Closed += (sender, args) =>
                        {
                            Mouse.Capture(null);
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error setting up column visibility controls: {ex.Message}");
                }
            };
        }

        private void ToggleColumnVisibility(string columnName, bool isVisible)
        {
            try
            {
                var column = FindName(columnName) as DataGridColumn;
                if (column != null)
                {
                    column.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling column visibility for {columnName}: {ex.Message}");
            }
        }

        #endregion

        #region DataGrid Event Handlers - Delegate to ViewModel

        private void AddReplyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                var comment = menuItem?.Tag as TaskCommentModel;

                if (comment != null && _viewModel?.AddReplyCommand?.CanExecute(comment) == true)
                {
                    _viewModel.AddReplyCommand.Execute(comment);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddReplyMenuItem_Click: {ex.Message}");
            }
        }

        private void EditCommentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                var comment = menuItem?.Tag as TaskCommentModel;

                if (comment != null && _viewModel?.EditCommentCommand?.CanExecute(comment) == true)
                {
                    _viewModel.EditCommentCommand.Execute(comment);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EditCommentMenuItem_Click: {ex.Message}");
            }
        }

        private void DeleteCommentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                var comment = menuItem?.Tag as TaskCommentModel;

                if (comment != null && _viewModel?.DeleteCommentCommand?.CanExecute(comment) == true)
                {
                    _viewModel.DeleteCommentCommand.Execute(comment);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteCommentMenuItem_Click: {ex.Message}");
            }
        }

        private void EditCommentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var comment = button?.Tag as TaskCommentModel;

                if (comment != null && _viewModel?.EditCommentCommand?.CanExecute(comment) == true)
                {
                    _viewModel.EditCommentCommand.Execute(comment);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EditCommentButton_Click: {ex.Message}");
            }
        }

        private void DeleteCommentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var comment = button?.Tag as TaskCommentModel;

                if (comment != null && _viewModel?.DeleteCommentCommand?.CanExecute(comment) == true)
                {
                    _viewModel.DeleteCommentCommand.Execute(comment);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteCommentButton_Click: {ex.Message}");
            }
        }

        private void ViewCommentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var comment = button?.Tag as TaskCommentModel;

                if (comment != null && _viewModel?.ViewCommentCommand?.CanExecute(comment) == true)
                {
                    _viewModel.ViewCommentCommand.Execute(comment);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ViewCommentButton_Click: {ex.Message}");
            }
        }

        // Trong TaskCommentsControl.xaml.cs
        private async void HardRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Bạn có muốn reset và chỉ hiển thị trang đầu tiên?\n\n" +
                    $"Hiện tại đã load: {GetLoadedPagesCount()} trang ({GetLoadedCommentsCount()} comments)\n\n" +
                    $"Sau khi reset sẽ chỉ hiển thị 20 comments đầu tiên.",
                    "Xác nhận reset",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await HardRefreshAsync();

                    MessageBox.Show("Đã reset về trang đầu tiên!", "Thành công",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi reset: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LightRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LightRefreshAsync();

                MessageBox.Show("Đã kiểm tra reply mới!", "Thành công",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Enhanced Column Management


        private void ColumnCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var checkBox = sender as CheckBox;
                if (checkBox == null) return;

                // Get column config
                var config = GetColumnConfigByCheckBox(checkBox.Name);
                if (config != null)
                {
                    // Don't allow essential columns to be unchecked
                    if (config.IsEssential && checkBox.IsChecked == false)
                    {
                        checkBox.IsChecked = true;
                        MessageBox.Show($"Cột '{config.DisplayName}' là cột bắt buộc và không thể ẩn.",
                                       "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    bool isVisible = checkBox.IsChecked == true;

                    // Update column visibility
                    ToggleColumnVisibility(config.ColumnName, isVisible);

                    // Update configuration and log change
                    UpdateColumnConfiguration(checkBox.Name, isVisible);
                }

                UpdateColumnVisibilityButtonText();
                UpdateColumnCountInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling column checkbox change: {ex.Message}");
            }
        }

        private void UpdateColumnVisibilityButtonText()
        {
            try
            {
                if (ColumnVisibilityButton == null) return;

                var allColumns = GetAllColumnCheckBoxesWithState();
                int visibleCount = allColumns.Count(c => c.Value.IsVisible);
                int totalCount = allColumns.Count;
                int essentialCount = allColumns.Count(c => c.Value.Config.IsEssential);

                ColumnVisibilityButton.Content = $"⚙️ Cột ({visibleCount}/{totalCount})";

                // Add tooltip with more info
                ColumnVisibilityButton.ToolTip = $"Hiển thị: {visibleCount}/{totalCount} cột\n" +
                                               $"Bắt buộc: {essentialCount} cột\n" +
                                               $"Thay đổi: {_columnVisibilityChanges?.Count ?? 0} lần";

                // Change button color based on visible count
                if (visibleCount <= essentialCount)
                {
                    ColumnVisibilityButton.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (visibleCount <= totalCount / 2)
                {
                    ColumnVisibilityButton.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
                }
                else
                {
                    ColumnVisibilityButton.Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating column visibility button text: {ex.Message}");
            }
        }

        #endregion

        #region Hierarchy Display Management

        /// <summary>
        /// Refresh hierarchy display when comments expand/collapse
        /// </summary>
        private void RefreshHierarchyDisplay()
        {
            if (_viewModel != null)
            {
                _viewModel.RefreshFlattenedComments();
            }
        }

        /// <summary>
        /// Handle expand/collapse với animation
        /// </summary>
        private async void ExpandRepliesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var comment = button?.Tag as TaskCommentModel;

                if (comment == null) return;

                // Prevent multiple clicks while loading
                if (comment.IsLoadingReplies)
                {
                    System.Diagnostics.Debug.WriteLine($"Already loading replies for comment {comment.Id}");
                    return;
                }

                // Toggle expansion state
                var wasExpanded = comment.IsExpanded;

                if (wasExpanded)
                {
                    // Collapse - just hide replies
                    System.Diagnostics.Debug.WriteLine($"Collapsing comment {comment.Id}");
                    comment.IsExpanded = false;
                    _viewModel?.RefreshFlattenedComments();
                }
                else
                {
                    // Expand - load replies if not loaded yet
                    System.Diagnostics.Debug.WriteLine($"Expanding comment {comment.Id}");

                    comment.IsExpanded = true;

                    // Check if we need to load replies
                    var loadedReplies = comment.Replies?.Count ?? 0;
                    var totalReplies = comment.ReplyCount;

                    if (loadedReplies == 0 && totalReplies > 0)
                    {
                        // No replies loaded yet - load first page
                        System.Diagnostics.Debug.WriteLine($"Loading first page of replies for comment {comment.Id}...");

                        comment.IsLoadingReplies = true;

                        try
                        {
                            await LoadRepliesForCommentAsync(comment, page: 1);
                        }
                        finally
                        {
                            comment.IsLoadingReplies = false;
                        }
                    }
                    else if (loadedReplies < totalReplies)
                    {
                        // Some replies loaded but not all - show option to load more
                        System.Diagnostics.Debug.WriteLine($"Comment {comment.Id} has {loadedReplies}/{totalReplies} replies loaded");

                        // Show load more button in the UI (handled by binding)
                    }
                    else
                    {
                        // All replies already loaded
                        System.Diagnostics.Debug.WriteLine($"Comment {comment.Id} has all {loadedReplies} replies loaded");
                    }

                    // Refresh display
                    _viewModel?.RefreshFlattenedComments();
                }

                // Update button visual immediately
                button.Content = comment.ExpandCollapseIcon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExpandRepliesButton_Click: {ex.Message}");
                MessageBox.Show($"Lỗi khi mở/đóng replies: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load replies for a specific comment - UPDATED
        /// </summary>
        private async Task LoadRepliesForCommentAsync(TaskCommentModel parentComment, int page = 1)
        {
            if (parentComment == null || _viewModel == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading replies for comment {parentComment.Id} (page {page})...");

                var replies = await _viewModel.LoadRepliesForCommentAsync(
                    parentComment.Id,
                    page,
                    PageSize);

                if (replies != null && replies.Count > 0)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        // Clear old replies if loading first page
                        if (page == 1)
                        {
                            parentComment.Replies.Clear();
                        }

                        foreach (var reply in replies)
                        {
                            // Check for duplicates
                            var existing = parentComment.Replies.FirstOrDefault(r => r.Id == reply.Id);
                            if (existing == null)
                            {
                                // Set hierarchy level
                                reply.HierarchyLevel = parentComment.HierarchyLevel + 1;
                                parentComment.Replies.Add(reply);

                                // Track reply cache
                                if (reply.HasReplies)
                                {
                                    _loadedRepliesCache[reply.Id] = (DateTime.UtcNow, reply.Replies.Count);
                                    _replyPagesLoaded[reply.Id] = 1;
                                }
                            }
                        }

                        // Update cache for parent comment
                        _loadedRepliesCache[parentComment.Id] = (DateTime.UtcNow, parentComment.Replies.Count);
                        _replyPagesLoaded[parentComment.Id] = page;

                        // Refresh flattened hierarchy
                        _viewModel.RefreshFlattenedComments();
                    });

                    System.Diagnostics.Debug.WriteLine($"Loaded {replies.Count} replies for comment {parentComment.Id}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No replies found for comment {parentComment.Id} on page {page}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading replies for comment {parentComment.Id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load more replies button click handler - NEW
        /// </summary>
        private async void LoadMoreRepliesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var comment = button?.Tag as TaskCommentModel;

                if (comment == null || comment.IsLoadingReplies)
                    return;

                // Get current page
                int currentPage = _replyPagesLoaded.TryGetValue(comment.Id, out int page) ? page : 1;
                int nextPage = currentPage + 1;

                System.Diagnostics.Debug.WriteLine($"Loading more replies for comment {comment.Id} (page {nextPage})...");

                comment.IsLoadingReplies = true;

                try
                {
                    await LoadRepliesForCommentAsync(comment, nextPage);
                }
                finally
                {
                    comment.IsLoadingReplies = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thêm replies: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region DataGrid Sorting Enhancement

        /// <summary>
        /// Handle DataGrid sorting events
        /// </summary>
        private void CommentsDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            try
            {
                // Custom sorting logic for hierarchy
                var dataGrid = sender as DataGrid;
                if (dataGrid?.ItemsSource is ObservableCollection<TaskCommentModel> comments)
                {
                    // Prevent default sorting for hierarchy columns
                    if (e.Column.SortMemberPath == "HierarchyLevel")
                    {
                        e.Handled = true;
                        return;
                    }

                    // For other columns, maintain hierarchy structure while sorting
                    e.Handled = true;

                    var sortDirection = e.Column.SortDirection == System.ComponentModel.ListSortDirection.Ascending
                        ? System.ComponentModel.ListSortDirection.Descending
                        : System.ComponentModel.ListSortDirection.Ascending;

                    e.Column.SortDirection = sortDirection;

                    // Custom sort logic based on column
                    switch (e.Column.SortMemberPath)
                    {
                        case "CreatedAt":
                            SortCommentsByDate(comments, sortDirection);
                            break;
                        case "Priority":
                            SortCommentsByPriority(comments, sortDirection);
                            break;
                        case "CommentType":
                            SortCommentsByType(comments, sortDirection);
                            break;
                        default:
                            // Default string sorting
                            SortCommentsByProperty(comments, e.Column.SortMemberPath, sortDirection);
                            break;
                    }

                    RefreshHierarchyDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DataGrid sorting: {ex.Message}");
            }
        }

        private void SortCommentsByDate(ObservableCollection<TaskCommentModel> comments, System.ComponentModel.ListSortDirection direction)
        {
            var sortedComments = direction == System.ComponentModel.ListSortDirection.Ascending
                ? comments.OrderBy(c => c.CreatedAt).ToList()
                : comments.OrderByDescending(c => c.CreatedAt).ToList();

            comments.Clear();
            foreach (var comment in sortedComments)
            {
                comments.Add(comment);
            }
        }

        private void SortCommentsByPriority(ObservableCollection<TaskCommentModel> comments, System.ComponentModel.ListSortDirection direction)
        {
            var sortedComments = direction == System.ComponentModel.ListSortDirection.Ascending
                ? comments.OrderBy(c => c.Priority).ToList()
                : comments.OrderByDescending(c => c.Priority).ToList();

            comments.Clear();
            foreach (var comment in sortedComments)
            {
                comments.Add(comment);
            }
        }

        private void SortCommentsByType(ObservableCollection<TaskCommentModel> comments, System.ComponentModel.ListSortDirection direction)
        {
            var sortedComments = direction == System.ComponentModel.ListSortDirection.Ascending
                ? comments.OrderBy(c => c.CommentType).ToList()
                : comments.OrderByDescending(c => c.CommentType).ToList();

            comments.Clear();
            foreach (var comment in sortedComments)
            {
                comments.Add(comment);
            }
        }

        private void SortCommentsByProperty(ObservableCollection<TaskCommentModel> comments, string propertyName, System.ComponentModel.ListSortDirection direction)
        {
            // Generic property sorting using reflection
            var property = typeof(TaskCommentModel).GetProperty(propertyName);
            if (property == null) return;

            var sortedComments = direction == System.ComponentModel.ListSortDirection.Ascending
                ? comments.OrderBy(c => property.GetValue(c)).ToList()
                : comments.OrderByDescending(c => property.GetValue(c)).ToList();

            comments.Clear();
            foreach (var comment in sortedComments)
            {
                comments.Add(comment);
            }
        }

        #endregion

        #region Search and Filter Enhancement

        private string _searchText = "";
        /// <summary>
        /// Search text for filtering comments
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                FilterComments();
            }
        }

        /// <summary>
        /// Filter comments based on search text
        /// </summary>
        private void FilterComments()
        {
            if (_viewModel?.TaskComments == null) return;

            try
            {
                var filteredComments = string.IsNullOrWhiteSpace(SearchText)
                    ? _viewModel.TaskComments.ToList()
                    : _viewModel.TaskComments.Where(c =>
                        c.Content.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        c.CreatedByName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        c.IssueTitle.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToList();

                // Rebuild hierarchy with filtered comments
                var hierarchyComments = TaskCommentModel.BuildHierarchy(filteredComments);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    _viewModel.FlattenedComments.Clear();
                    foreach (var comment in hierarchyComments)
                    {
                        _viewModel.FlattenedComments.Add(comment);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering comments: {ex.Message}");
            }
        }

        #endregion

        #region Context Menu Enhancement

        /// <summary>
        /// Enhanced context menu with more options
        /// </summary>
        private void ShowEnhancedContextMenu(TaskCommentModel comment, FrameworkElement target)
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
                    Header = "Thêm trả lời con",
                    Tag = comment,
                    Command = _viewModel?.AddReplyCommand,
                    CommandParameter = comment,
                    Icon = new TextBlock
                    {
                        Text = "💬",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Chỉnh sửa",
                    Tag = comment,
                    Command = _viewModel?.EditCommentCommand,
                    CommandParameter = comment,
                    Icon = new TextBlock
                    {
                        Text = "✏️",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                contextMenu.Items.Add(new Separator());

                // Resolution actions
                if (!comment.IsResolved)
                {
                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = "Đánh dấu đã giải quyết",
                        Tag = comment,
                        Command = _viewModel?.ResolveCommentCommand,
                        CommandParameter = comment,
                        Icon = new TextBlock
                        {
                            Text = "✅",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }

                if (comment.IsResolved && !comment.IsVerified)
                {
                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = "Xác nhận đã giải quyết",
                        Tag = comment,
                        Command = _viewModel?.VerifyCommentCommand,
                        CommandParameter = comment,
                        Icon = new TextBlock
                        {
                            Text = "🔒",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }

                contextMenu.Items.Add(new Separator());

                // Assignment actions
                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Xét người thực hiện",
                    Tag = comment,
                    Command = _viewModel?.AssignCommentCommand,
                    CommandParameter = comment,
                    Icon = new TextBlock
                    {
                        Text = "👥",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "Xét người xác nhận",
                    Tag = comment,
                    Command = _viewModel?.SetReviewerCommand,
                    CommandParameter = comment,
                    Icon = new TextBlock
                    {
                        Text = "🔍",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                contextMenu.Items.Add(new Separator());

                // Priority actions với TaskPriority extensions
                var priorityMenu = new MenuItem
                {
                    Header = "Thay đổi ưu tiên",
                    Icon = new TextBlock
                    {
                        Text = "🎯",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)))
                {
                    if (priority == TaskPriority.All) continue;

                    var priorityMenuItem = new MenuItem
                    {
                        Header = $"{priority.GetIcon()} {priority.GetDisplayName()}",
                        Tag = comment,
                        Command = _viewModel?.ChangePriorityCommand,
                        CommandParameter = new { Comment = comment, Priority = priority },
                        ToolTip = $"{priority.GetDescription()}\nSLA: {priority.GetSlaInDays()} ngày",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(priority.GetHexColor() + "20"))
                    };

                    if (comment.Priority == priority)
                    {
                        priorityMenuItem.FontWeight = FontWeights.Bold;
                        priorityMenuItem.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(priority.GetHexColor() + "40"));
                    }

                    if (priority.IsHighPriority())
                    {
                        priorityMenuItem.Foreground = new SolidColorBrush(Colors.DarkRed);
                    }
                    else if (priority.IsUrgent())
                    {
                        priorityMenuItem.Foreground = new SolidColorBrush(Colors.Red);
                    }

                    priorityMenu.Items.Add(priorityMenuItem);
                }
                contextMenu.Items.Add(priorityMenu);

                // Status actions với TaskStatuss extensions
                var statusMenu = new MenuItem
                {
                    Header = "Thay đổi trạng thái",
                    Icon = new TextBlock
                    {
                        Text = "📊",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                foreach (TaskStatuss status in Enum.GetValues(typeof(TaskStatuss)))
                {
                    if (status == TaskStatuss.All) continue;

                    var statusMenuItem = new MenuItem
                    {
                        Header = $"{status.GetIcon()} {status.GetDisplayName()}",
                        Tag = comment,
                        Command = _viewModel?.ChangeStatusCommand,
                        CommandParameter = new { Comment = comment, Status = status },
                        ToolTip = $"{status.GetDescription()}\nWorkflow: {status.GetWorkflowPhase()}",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(status.GetHexColor() + "20"))
                    };

                    if (comment.CommentStatus == status)
                    {
                        statusMenuItem.FontWeight = FontWeights.Bold;
                        statusMenuItem.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(status.GetHexColor() + "40"));
                    }

                    if (status.IsFinal())
                    {
                        statusMenuItem.Foreground = new SolidColorBrush(Colors.DarkGreen);
                    }
                    else if (status.IsBlocked())
                    {
                        statusMenuItem.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else if (status.IsInProgress())
                    {
                        statusMenuItem.Foreground = new SolidColorBrush(Colors.Blue);
                    }

                    statusMenu.Items.Add(statusMenuItem);
                }
                contextMenu.Items.Add(statusMenu);

                // Comment Type actions với CommentType extensions
                var typeMenu = new MenuItem
                {
                    Header = "Thay đổi loại comment",
                    Icon = new TextBlock
                    {
                        Text = "🏷️",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                foreach (CommentType commentType in Enum.GetValues(typeof(CommentType)))
                {
                    var typeMenuItem = new MenuItem
                    {
                        Header = $"{commentType.GetIcon()} {commentType.GetDisplayName()}",
                        Tag = comment,
                        Command = _viewModel?.ChangeTypeCommand,
                        CommandParameter = new { Comment = comment, Type = commentType },
                        ToolTip = $"{commentType.GetDescription()}\nDefault Priority: {commentType.GetDefaultPriority().GetDisplayName()}",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(commentType.GetHexColor() + "20"))
                    };

                    if (comment.CommentType == commentType)
                    {
                        typeMenuItem.FontWeight = FontWeights.Bold;
                        typeMenuItem.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(commentType.GetHexColor() + "40"));
                    }

                    if (commentType.RequiresResponse())
                    {
                        typeMenuItem.Header += " ⚡";
                    }

                    if (commentType.CanBeBlocking())
                    {
                        typeMenuItem.Header += " 🚫";
                    }

                    typeMenu.Items.Add(typeMenuItem);
                }
                contextMenu.Items.Add(typeMenu);

                contextMenu.Items.Add(new Separator());

                // Flags
                var blockingMenuItem = new MenuItem
                {
                    Header = comment.IsBlocking ? "Bỏ đánh dấu Blocking" : "Đánh dấu Blocking",
                    Tag = comment,
                    Command = _viewModel?.ToggleBlockingCommand,
                    CommandParameter = comment,
                    ToolTip = comment.IsBlocking
                        ? "Click để bỏ đánh dấu blocking. Comment sẽ không còn block task."
                        : "Click để đánh dấu blocking. Comment này sẽ block task cho đến khi được resolve.",
                    IsEnabled = comment.CommentType.CanBeBlocking(),
                    Icon = new TextBlock
                    {
                        Text = "🚫",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                if (!comment.CommentType.CanBeBlocking())
                {
                    blockingMenuItem.ToolTip = $"Loại comment '{comment.CommentType.GetDisplayName()}' không thể được đánh dấu blocking.";
                    blockingMenuItem.Foreground = new SolidColorBrush(Colors.Gray);
                }

                contextMenu.Items.Add(blockingMenuItem);

                var discussionMenuItem = new MenuItem
                {
                    Header = comment.RequiresDiscussion ? "Bỏ yêu cầu thảo luận" : "Yêu cầu thảo luận",
                    Tag = comment,
                    Command = _viewModel?.ToggleDiscussionCommand,
                    CommandParameter = comment,
                    ToolTip = comment.RequiresDiscussion
                        ? "Click để bỏ yêu cầu thảo luận. Comment sẽ không còn cần discussion."
                        : "Click để yêu cầu thảo luận. Comment này sẽ cần được discuss trước khi resolve.",
                    Icon = new TextBlock
                    {
                        Text = "💬",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                contextMenu.Items.Add(discussionMenuItem);

                contextMenu.Items.Add(new Separator());

                // Time tracking (if applicable)
                if (comment.HasEstimatedTime || comment.HasActualTime)
                {
                    var timeTrackingMenu = new MenuItem
                    {
                        Header = "Time Tracking",
                        Icon = new TextBlock
                        {
                            Text = "⏱️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };

                    timeTrackingMenu.Items.Add(new MenuItem
                    {
                        Header = $"Estimated: {comment.FormattedEstimatedTime}",
                        IsEnabled = false,
                        Icon = new TextBlock
                        {
                            Text = "📊",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });

                    timeTrackingMenu.Items.Add(new MenuItem
                    {
                        Header = $"Actual: {comment.FormattedActualTime}",
                        IsEnabled = false,
                        Icon = new TextBlock
                        {
                            Text = "⏲️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });

                    if (comment.HasEstimatedTime && comment.HasActualTime)
                    {
                        var variance = comment.TimeVariance;
                        var varianceText = variance > 0 ? $"+{variance:F1}h (Over)" : $"{variance:F1}h (Under)";
                        var varianceColor = variance > 0 ? Colors.Red : Colors.Green;

                        timeTrackingMenu.Items.Add(new MenuItem
                        {
                            Header = $"Variance: {varianceText}",
                            Foreground = new SolidColorBrush(varianceColor),
                            IsEnabled = false,
                            Icon = new TextBlock
                            {
                                Text = "📈",
                                FontSize = fontIconSize,
                                VerticalAlignment = VerticalAlignment.Center,
                                Foreground = new SolidColorBrush(varianceColor)
                            }
                        });
                    }

                    contextMenu.Items.Add(timeTrackingMenu);
                    contextMenu.Items.Add(new Separator());
                }

                // Due date indicator
                if (comment.HasDueDate)
                {
                    var dueDateColor = comment.IsOverdue ? Colors.Red : Colors.Green;
                    var dueDateText = comment.IsOverdue ? "Overdue" : "Hạn chót";
                    var dueDateIcon = comment.IsOverdue ? "⚠️" : "📅";

                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = $"{dueDateText}: {comment.FormattedDueDate}",
                        Foreground = new SolidColorBrush(dueDateColor),
                        IsEnabled = false,
                        ToolTip = comment.IsOverdue
                            ? $"Comment đã quá hạn {Math.Abs(comment.DaysUntilDue)} ngày"
                            : $"Còn {comment.DaysUntilDue} ngày đến hạn",
                        Icon = new TextBlock
                        {
                            Text = dueDateIcon,
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = new SolidColorBrush(dueDateColor)
                        }
                    });
                    contextMenu.Items.Add(new Separator());
                }

                // Quick info section
                var infoMenu = new MenuItem
                {
                    Header = "Thông tin",
                    Icon = new TextBlock
                    {
                        Text = "ℹ️",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                infoMenu.Items.Add(new MenuItem
                {
                    Header = $"ID: {comment.Id}",
                    IsEnabled = false,
                    Icon = new TextBlock
                    {
                        Text = "🆔",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                infoMenu.Items.Add(new MenuItem
                {
                    Header = $"Created by: {comment.CreatedByName}",
                    IsEnabled = false,
                    Icon = new TextBlock
                    {
                        Text = "👤",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                infoMenu.Items.Add(new MenuItem
                {
                    Header = $"Created: {comment.FormattedCreatedAt}",
                    IsEnabled = false,
                    Icon = new TextBlock
                    {
                        Text = "📅",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                if (comment.HasBeenUpdated)
                {
                    infoMenu.Items.Add(new MenuItem
                    {
                        Header = $"Updated: {comment.FormattedUpdatedAt}",
                        IsEnabled = false,
                        Icon = new TextBlock
                        {
                            Text = "✏️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }

                if (comment.AssignedToId.HasValue)
                {
                    infoMenu.Items.Add(new MenuItem
                    {
                        Header = $"Assigned to: {comment.AssignedToName ?? "N/A"}",
                        IsEnabled = false,
                        Icon = new TextBlock
                        {
                            Text = "👷",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }

                if (comment.ReviewerId.HasValue)
                {
                    infoMenu.Items.Add(new MenuItem
                    {
                        Header = $"Reviewer: {comment.ReviewerName ?? "N/A"}",
                        IsEnabled = false,
                        Icon = new TextBlock
                        {
                            Text = "👁️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }

                if (comment.IsReply)
                {
                    infoMenu.Items.Add(new MenuItem
                    {
                        Header = $"Reply to: #{comment.ParentCommentId}",
                        IsEnabled = false,
                        Icon = new TextBlock
                        {
                            Text = "↩️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }

                if (comment.HasReplies)
                {
                    infoMenu.Items.Add(new MenuItem
                    {
                        Header = $"Replies: {comment.ReplyCount}",
                        IsEnabled = false,
                        Icon = new TextBlock
                        {
                            Text = "💬",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }

                if (comment.HasTags)
                {
                    var tagsText = string.Join(", ", comment.Tags.Take(3));
                    if (comment.Tags.Count > 3)
                    {
                        tagsText += $" (+{comment.Tags.Count - 3} more)";
                    }

                    infoMenu.Items.Add(new MenuItem
                    {
                        Header = $"Tags: {tagsText}",
                        IsEnabled = false,
                        ToolTip = string.Join(", ", comment.Tags),
                        Icon = new TextBlock
                        {
                            Text = "🏷️",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    });
                }

                contextMenu.Items.Add(infoMenu);
                contextMenu.Items.Add(new Separator());

                // Danger zone
                var dangerMenuItem = new MenuItem
                {
                    Header = "Xóa",
                    Tag = comment,
                    Command = _viewModel?.DeleteCommentCommand,
                    CommandParameter = comment,
                    Foreground = new SolidColorBrush(Colors.Red),
                    ToolTip = "⚠️ Hành động này không thể hoàn tác!\nClick để xóa comment này vĩnh viễn.",
                    Icon = new TextBlock
                    {
                        Text = "🗑️",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(Colors.Red)
                    }
                };

                if (comment.IsBlocking || comment.RequiresDiscussion || comment.Priority.IsHighPriority())
                {
                    dangerMenuItem.ToolTip += "\n\n⚠️ CHÚ Ý: Comment này có mức độ quan trọng cao!";
                    dangerMenuItem.Header = "Xóa (Quan trọng)";
                    dangerMenuItem.Icon = new TextBlock
                    {
                        Text = "⚠️🗑️",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(Colors.Red)
                    };
                }

                contextMenu.Items.Add(dangerMenuItem);

                // Advanced copy actions
                contextMenu.Items.Add(new Separator());
                var copyMenu = new MenuItem
                {
                    Header = "Copy",
                    Icon = new TextBlock
                    {
                        Text = "📋",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                copyMenu.Items.Add(new MenuItem
                {
                    Header = "Copy Content",
                    Command = new RelayCommand(_ => CopyToClipboard(comment.Content)),
                    ToolTip = "Copy nội dung comment vào clipboard",
                    Icon = new TextBlock
                    {
                        Text = "📝",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                copyMenu.Items.Add(new MenuItem
                {
                    Header = "Copy Link",
                    Command = new RelayCommand(_ => CopyToClipboard($"#{comment.Id}")),
                    ToolTip = "Copy link reference của comment",
                    Icon = new TextBlock
                    {
                        Text = "🔗",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                copyMenu.Items.Add(new MenuItem
                {
                    Header = "Copy Summary",
                    Command = new RelayCommand(_ => CopyCommentSummary(comment)),
                    ToolTip = "Copy summary đầy đủ của comment",
                    Icon = new TextBlock
                    {
                        Text = "📊",
                        FontSize = fontIconSize,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                contextMenu.Items.Add(copyMenu);

                // Workflow suggestions
                if (comment.CommentType.RequiresResponse() && !comment.IsResolved)
                {
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = "Suggest: Response Required",
                        Foreground = new SolidColorBrush(Colors.Orange),
                        IsEnabled = false,
                        ToolTip = "Comment này cần response để unblock workflow",
                        Icon = new TextBlock
                        {
                            Text = "⚡",
                            FontSize = fontIconSize,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = new SolidColorBrush(Colors.Orange)
                        }
                    });
                }

                // SLA warning
                if (comment.Priority.IsUrgent())
                {
                    var slaHours = comment.Priority.GetSlaInDays() * 24;
                    var ageHours = (DateTime.Now - comment.CreatedAt).TotalHours;

                    if (ageHours > slaHours * 0.8)
                    {
                        contextMenu.Items.Add(new MenuItem
                        {
                            Header = $"SLA Warning: {ageHours:F1}h / {slaHours}h",
                            Foreground = new SolidColorBrush(Colors.Red),
                            IsEnabled = false,
                            ToolTip = $"Comment đã vượt 80% SLA limit.\nEscalation level: {comment.Priority.GetEscalationLevel()}",
                            Icon = new TextBlock
                            {
                                Text = "🚨",
                                FontSize = fontIconSize,
                                VerticalAlignment = VerticalAlignment.Center,
                                Foreground = new SolidColorBrush(Colors.Red)
                            }
                        });
                    }
                }

                contextMenu.PlacementTarget = target;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing enhanced context menu: {ex.Message}");
                ShowSimpleContextMenu(comment, target);
            }
        }

        /// <summary>
        /// Fallback simple context menu khi có lỗi
        /// </summary>
        private void ShowSimpleContextMenu(TaskCommentModel comment, FrameworkElement target)
        {
            try
            {
                var contextMenu = new ContextMenu();

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "💬 Reply",
                    Command = _viewModel?.AddReplyCommand,
                    CommandParameter = comment
                });

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "✏️ Edit",
                    Command = _viewModel?.EditCommentCommand,
                    CommandParameter = comment,
                    Visibility = comment.CanEdit ? Visibility.Visible : Visibility.Collapsed
                });

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "👁️ View Details",
                    Command = _viewModel?.ViewCommentCommand,
                    CommandParameter = comment
                });

                contextMenu.Items.Add(new Separator());

                contextMenu.Items.Add(new MenuItem
                {
                    Header = "🗑️ Delete",
                    Command = _viewModel?.DeleteCommentCommand,
                    CommandParameter = comment,
                    Visibility = comment.CanDelete ? Visibility.Visible : Visibility.Collapsed,
                    Foreground = new SolidColorBrush(Colors.Red)
                });

                contextMenu.PlacementTarget = target;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing simple context menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Copy text to clipboard
        /// </summary>
        private void CopyToClipboard(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                    MessageBox.Show("Đã copy vào clipboard!", "Thành công",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi copy: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Copy comment summary to clipboard
        /// </summary>
        private void CopyCommentSummary(TaskCommentModel comment)
        {
            try
            {
                var summary = new StringBuilder();
                summary.AppendLine($"Comment #{comment.Id}");
                summary.AppendLine($"Type: {comment.CommentType.GetIcon()} {comment.CommentType.GetDisplayName()}");
                summary.AppendLine($"Priority: {comment.Priority.GetIcon()} {comment.Priority.GetDisplayName()}");
                summary.AppendLine($"Status: {comment.CommentStatus.GetIcon()} {comment.CommentStatus.GetDisplayName()}");
                summary.AppendLine($"Author: {comment.CreatedByName}");
                summary.AppendLine($"Created: {comment.FormattedCreatedAt}");

                if (comment.HasBeenUpdated)
                {
                    summary.AppendLine($"Updated: {comment.FormattedUpdatedAt}");
                }

                if (comment.IsBlocking)
                {
                    summary.AppendLine("🚫 BLOCKING");
                }

                if (comment.RequiresDiscussion)
                {
                    summary.AppendLine("💬 REQUIRES DISCUSSION");
                }

                if (comment.HasDueDate)
                {
                    summary.AppendLine($"Due: {comment.FormattedDueDate}");
                    if (comment.IsOverdue)
                    {
                        summary.AppendLine("⚠️ OVERDUE");
                    }
                }

                if (comment.HasTags)
                {
                    summary.AppendLine($"Tags: {string.Join(", ", comment.Tags)}");
                }

                summary.AppendLine();
                summary.AppendLine("Content:");
                summary.AppendLine(comment.Content);

                CopyToClipboard(summary.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo summary: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Export Functionality

        /// <summary>
        /// Export comments to CSV
        /// </summary>
        public void ExportCommentsToCSV()
        {
            try
            {
                var comments = _viewModel?.FlattenedComments ?? new ObservableCollection<TaskCommentModel>();
                if (!comments.Any())
                {
                    MessageBox.Show("Không có comment nào để export.", "Thông báo",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Comments to CSV",
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"TaskComments_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new System.Text.StringBuilder();

                    // Headers
                    csv.AppendLine("ID,ParentID,Level,Content,Type,Author,CreatedAt,Status,Priority,IsResolved,IsBlocking");

                    // Data
                    foreach (var comment in comments)
                    {
                        csv.AppendLine($"{comment.Id}," +
                                      $"{comment.ParentCommentId?.ToString() ?? ""}," +
                                      $"{comment.HierarchyLevel}," +
                                      $"\"{comment.Content?.Replace("\"", "\"\"")}\"," +
                                      $"{comment.CommentType}," +
                                      $"\"{comment.CreatedByName}\"," +
                                      $"{comment.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                                      $"{comment.CommentStatus}," +
                                      $"{comment.Priority}," +
                                      $"{comment.IsResolved}," +
                                      $"{comment.IsBlocking}");
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, csv.ToString(), System.Text.Encoding.UTF8);

                    MessageBox.Show($"Export thành công!\nFile: {saveDialog.FileName}", "Thành công",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi export: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Updated Column Management Methods

        /// <summary>
        /// Get dictionary của tất cả checkboxes với current state
        /// </summary>
        private Dictionary<string, (CheckBox CheckBox, bool IsVisible, ColumnConfig Config)> GetAllColumnCheckBoxesWithState()
        {
            var result = new Dictionary<string, (CheckBox, bool, ColumnConfig)>();

            if (_currentColumnConfigs == null) return result;

            foreach (var config in _currentColumnConfigs)
            {
                var checkBox = FindName(config.CheckBoxName) as CheckBox;
                if (checkBox != null)
                {
                    result[config.ColumnName] = (checkBox, checkBox.IsChecked == true, config);
                }
            }

            return result;
        }

        #endregion

        #region Configuration Persistence

        /// <summary>
        /// Save column configurations to storage
        /// </summary>
        private void SaveColumnConfigurations()
        {
            try
            {
                // Convert to simple format for saving
                var configData = _currentColumnConfigs.Select(c => new
                {
                    ColumnName = c.ColumnName,
                    IsVisible = c.IsVisibleByDefault,
                    LastModified = DateTime.Now
                }).ToList();

                // TODO: Implement actual saving logic
                // Example options:
                // 1. Save to application settings
                // 2. Save to local file (JSON/XML)
                // 3. Save to database
                // 4. Save to user profile

                //var json = System.Text.Json.JsonSerializer.Serialize(configData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                //System.Diagnostics.Debug.WriteLine($"Column configurations saved:\n{json}");

                // Example: Save to temp file for demonstration
                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskCommentsColumnConfig.json");
                //System.IO.File.WriteAllText(tempFile, json);
                System.Diagnostics.Debug.WriteLine($"Configurations saved to: {tempFile}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving column configurations: {ex.Message}");
            }
        }

        /// <summary>
        /// Load column configurations from storage
        /// </summary>
        private void LoadColumnConfigurations()
        {
            try
            {
                // TODO: Implement actual loading logic
                // For now, use default configurations

                // Example: Load from temp file
                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskCommentsColumnConfig.json");
                if (System.IO.File.Exists(tempFile))
                {
                    var json = System.IO.File.ReadAllText(tempFile);
                    //var savedConfigs = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(json);

                    System.Diagnostics.Debug.WriteLine($"Loaded saved configurations from: {tempFile}");

                    // Apply saved configurations to default configs
                    // This would need more implementation to properly merge configurations
                }

                System.Diagnostics.Debug.WriteLine("Column configurations loaded (using defaults)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading column configurations: {ex.Message}");
            }
        }

        #endregion

        #region Performance Optimization

        /// <summary>
        /// Virtualization cho large datasets
        /// </summary>
        private void OptimizeDataGridPerformance()
        {
            try
            {
                // Enable virtualization for better performance with large datasets
                CommentsDataGrid.EnableRowVirtualization = true;
                CommentsDataGrid.EnableColumnVirtualization = true;

                // Set virtualization properties correctly
                VirtualizingPanel.SetVirtualizationMode(CommentsDataGrid, VirtualizationMode.Recycling);
                VirtualizingPanel.SetScrollUnit(CommentsDataGrid, ScrollUnit.Item);
                VirtualizingPanel.SetIsVirtualizing(CommentsDataGrid, true);
                VirtualizingPanel.SetIsContainerVirtualizable(CommentsDataGrid, true);

                // Optimize scrolling performance
                ScrollViewer.SetCanContentScroll(CommentsDataGrid, true);

                System.Diagnostics.Debug.WriteLine("DataGrid performance optimization applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error optimizing DataGrid performance: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup performance optimizations
        /// </summary>
        private void SetupPerformanceOptimizations()
        {
            try
            {
                OptimizeDataGridPerformance();

                // Enable DataGrid sorting
                CommentsDataGrid.Sorting += CommentsDataGrid_Sorting;
                CommentsDataGrid.KeyDown += CommentsDataGrid_KeyDown;

                // Setup context menu for DataGrid
                SetupDataGridContextMenu();

                System.Diagnostics.Debug.WriteLine("Performance optimizations setup completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up performance optimizations: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup context menu for DataGrid
        /// </summary>
        private void SetupDataGridContextMenu()
        {
            try
            {
                if (CommentsDataGrid.ContextMenu == null)
                {
                    CommentsDataGrid.ContextMenu = new ContextMenu();
                }

                // Add export menu item
                var exportMenuItem = new MenuItem
                {
                    Header = "📊 Export to CSV",
                    Command = new RelayCommand(_ => ExportCommentsToCSV())
                };

                CommentsDataGrid.ContextMenu.Items.Add(new Separator());
                CommentsDataGrid.ContextMenu.Items.Add(exportMenuItem);

                // Add refresh menu item
                var refreshMenuItem = new MenuItem
                {
                    Header = "🔄 Refresh Comments",
                    Command = new RelayCommand(_ => _viewModel?.RefreshCommentsCommand?.Execute(null))
                };

                CommentsDataGrid.ContextMenu.Items.Add(refreshMenuItem);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up DataGrid context menu: {ex.Message}");
            }
        }

        #endregion

        #region DataGrid Events với Context Menu Integration

        /// <summary>
        /// Handle right-click trên DataGrid để show context menu
        /// </summary>
        private void CommentsDataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid == null) return;

                var hitTest = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
                var row = FindVisualParent<DataGridRow>(hitTest.VisualHit);

                if (row?.Item is TaskCommentModel comment)
                {
                    // Select the row
                    dataGrid.SelectedItem = comment;

                    // Show enhanced context menu
                    ShowEnhancedContextMenu(comment, row);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CommentsDataGrid_MouseRightButtonUp: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle double-click để view comment details
        /// </summary>
        private void CommentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid?.SelectedItem is TaskCommentModel comment)
                {
                    _viewModel?.ViewCommentCommand?.Execute(comment);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CommentsDataGrid_MouseDoubleClick: {ex.Message}");
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

        #region Column Visibility Change Tracking

        /// <summary>
        /// Get column visibility changes log
        /// </summary>
        public List<ColumnVisibilityChange> GetColumnVisibilityChanges()
        {
            return _columnVisibilityChanges?.ToList() ?? new List<ColumnVisibilityChange>();
        }

        /// <summary>
        /// Clear column visibility changes log
        /// </summary>
        public void ClearColumnVisibilityChanges()
        {
            _columnVisibilityChanges?.Clear();
            System.Diagnostics.Debug.WriteLine("Column visibility changes log cleared");
        }

        /// <summary>
        /// Export column visibility changes to text
        /// </summary>
        public string ExportColumnVisibilityChanges()
        {
            try
            {
                if (_columnVisibilityChanges == null || !_columnVisibilityChanges.Any())
                {
                    return "No column visibility changes recorded.";
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Column Visibility Changes Log");
                sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("User: nguyenbalam57");
                sb.AppendLine();

                foreach (var change in _columnVisibilityChanges.OrderBy(c => c.ChangedAt))
                {
                    sb.AppendLine($"[{change.ChangedAt:HH:mm:ss}] {change.ColumnName}");
                    sb.AppendLine($"  Changed from: {(change.OldValue ? "Visible" : "Hidden")}");
                    sb.AppendLine($"  Changed to: {(change.NewValue ? "Visible" : "Hidden")}");
                    sb.AppendLine($"  Reason: {change.Reason}");
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error exporting changes: {ex.Message}";
            }
        }

        #endregion

        #region Keyboard Shortcuts Enhancement

        /// <summary>
        /// Enhanced keyboard navigation với shortcuts
        /// </summary>
        private void CommentsDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (CommentsDataGrid.SelectedItem is TaskCommentModel selectedComment)
                {
                    var isCtrlPressed = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);
                    var isShiftPressed = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift);
                    var isAltPressed = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt);

                    switch (e.Key)
                    {
                        case Key.Space:
                            // Toggle expand/collapse
                            if (selectedComment.HasReplies)
                            {
                                _viewModel?.ExpandRepliesCommand?.Execute(selectedComment);
                                RefreshHierarchyDisplay();
                            }
                            e.Handled = true;
                            break;

                        case Key.Enter:
                            if (isCtrlPressed)
                            {
                                // Ctrl+Enter: Add reply
                                _viewModel?.AddReplyCommand?.Execute(selectedComment);
                            }
                            else
                            {
                                // Enter: View details
                                _viewModel?.ViewCommentCommand?.Execute(selectedComment);
                            }
                            e.Handled = true;
                            break;

                        case Key.R:
                            if (isCtrlPressed)
                            {
                                // Ctrl+R: Reply
                                _viewModel?.AddReplyCommand?.Execute(selectedComment);
                                e.Handled = true;
                            }
                            else if (isCtrlPressed && isShiftPressed)
                            {
                                // Ctrl+Shift+R: Resolve
                                _viewModel?.ResolveCommentCommand?.Execute(selectedComment);
                                e.Handled = true;
                            }
                            break;

                        case Key.E:
                            if (isCtrlPressed)
                            {
                                // Ctrl+E: Edit
                                if (selectedComment.CanEdit)
                                {
                                    _viewModel?.EditCommentCommand?.Execute(selectedComment);
                                }
                                e.Handled = true;
                            }
                            break;

                        case Key.Delete:
                            // Delete: Delete comment
                            if (selectedComment.CanDelete)
                            {
                                _viewModel?.DeleteCommentCommand?.Execute(selectedComment);
                            }
                            e.Handled = true;
                            break;

                        case Key.F5:
                            // F5: Refresh
                            _viewModel?.RefreshCommentsCommand?.Execute(null);
                            e.Handled = true;
                            break;

                        case Key.C:
                            if (isCtrlPressed)
                            {
                                if (isShiftPressed)
                                {
                                    // Ctrl+Shift+C: Copy summary
                                    CopyCommentSummary(selectedComment);
                                }
                                else
                                {
                                    // Ctrl+C: Copy content
                                    CopyToClipboard(selectedComment.Content);
                                }
                                e.Handled = true;
                            }
                            break;

                        case Key.B:
                            if (isCtrlPressed && isShiftPressed)
                            {
                                // Ctrl+Shift+B: Toggle blocking
                                _viewModel?.ToggleBlockingCommand?.Execute(selectedComment);
                                e.Handled = true;
                            }
                            break;

                        case Key.D:
                            if (isCtrlPressed && isShiftPressed)
                            {
                                // Ctrl+Shift+D: Toggle discussion
                                _viewModel?.ToggleDiscussionCommand?.Execute(selectedComment);
                                e.Handled = true;
                            }
                            break;

                        case Key.V:
                            if (isCtrlPressed && isShiftPressed)
                            {
                                // Ctrl+Shift+V: Verify
                                _viewModel?.VerifyCommentCommand?.Execute(selectedComment);
                                e.Handled = true;
                            }
                            break;

                        case Key.A:
                            if (isCtrlPressed && isShiftPressed)
                            {
                                // Ctrl+Shift+A: Assign
                                _viewModel?.AssignCommentCommand?.Execute(selectedComment);
                                e.Handled = true;
                            }
                            break;

                        case Key.P:
                            if (isCtrlPressed && isShiftPressed)
                            {
                                // Ctrl+Shift+P: Change priority menu
                                var selectedRow = CommentsDataGrid.ItemContainerGenerator
                                    .ContainerFromItem(selectedComment) as DataGridRow;
                                if (selectedRow != null)
                                {
                                    ShowQuickPriorityMenu(selectedComment, selectedRow);
                                }
                                e.Handled = true;
                            }
                            break;

                        case Key.F1:
                            // F1: Show help
                            ShowKeyboardShortcutsHelp();
                            e.Handled = true;
                            break;

                        case Key.Escape:
                            // Escape: Clear selection and close any open menus
                            CommentsDataGrid.SelectedItem = null;
                            e.Handled = true;
                            break;
                    }
                }

                // Global shortcuts (không cần selection)
                switch (e.Key)
                {
                    case Key.N when e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control):
                        // Ctrl+N: New comment
                        _viewModel?.AddCommentCommand?.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.F when e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control):
                        // Ctrl+F: Focus search (if implemented)
                        // TODO: Implement search functionality
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in keyboard navigation: {ex.Message}");
            }
        }

        /// <summary>
        /// Show quick priority change menu
        /// </summary>
        private void ShowQuickPriorityMenu(TaskCommentModel comment, FrameworkElement target)
        {
            try
            {
                var priorityMenu = new ContextMenu();

                priorityMenu.Items.Add(new MenuItem
                {
                    Header = "🎯 Quick Priority Change",
                    IsEnabled = false,
                    FontWeight = FontWeights.Bold
                });

                priorityMenu.Items.Add(new Separator());

                // Common priorities for quick access
                var quickPriorities = new[]
                {
            TaskPriority.Low,
            TaskPriority.Normal,
            TaskPriority.High,
            TaskPriority.Critical
        };

                foreach (var priority in quickPriorities)
                {
                    var menuItem = new MenuItem
                    {
                        Header = $"{priority.GetIcon()} {priority.GetDisplayName()}",
                        Command = _viewModel?.ChangePriorityCommand,
                        CommandParameter = new { Comment = comment, Priority = priority },
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(priority.GetHexColor() + "20"))
                    };

                    if (comment.Priority == priority)
                    {
                        menuItem.FontWeight = FontWeights.Bold;
                        menuItem.Header += " ✓";
                    }

                    priorityMenu.Items.Add(menuItem);
                }

                priorityMenu.PlacementTarget = target;
                priorityMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing quick priority menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Show keyboard shortcuts help dialog
        /// </summary>
        private void ShowKeyboardShortcutsHelp()
        {
            try
            {
                var helpWindow = new Window
                {
                    Title = "Keyboard Shortcuts - TaskComments",
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    ResizeMode = ResizeMode.CanResize
                };

                var scrollViewer = new ScrollViewer();
                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                var title = new TextBlock
                {
                    Text = "⌨️ Keyboard Shortcuts",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                stackPanel.Children.Add(title);

                var shortcuts = new Dictionary<string, string>
                {
                    ["Navigation"] = "",
                    ["↑/↓ Arrow Keys"] = "Navigate through comments",
                    ["Space"] = "Expand/Collapse replies",
                    ["Enter"] = "View comment details",
                    ["Escape"] = "Clear selection",
                    [""] = "",

                    ["Comment Actions"] = "",
                    ["Ctrl+N"] = "Add new comment",
                    ["Ctrl+R"] = "Reply to selected comment",
                    ["Ctrl+E"] = "Edit selected comment",
                    ["Delete"] = "Delete selected comment",
                    ["Ctrl+Enter"] = "Add reply to selected comment",
                    [""] = "",

                    ["Advanced Actions"] = "",
                    ["Ctrl+Shift+R"] = "Resolve comment",
                    ["Ctrl+Shift+V"] = "Verify comment",
                    ["Ctrl+Shift+A"] = "Assign comment",
                    ["Ctrl+Shift+B"] = "Toggle blocking flag",
                    ["Ctrl+Shift+D"] = "Toggle discussion requirement",
                    ["Ctrl+Shift+P"] = "Quick priority change",
                    [""] = "",

                    ["Copy Operations"] = "",
                    ["Ctrl+C"] = "Copy comment content",
                    ["Ctrl+Shift+C"] = "Copy comment summary",
                    [""] = "",

                    ["System"] = "",
                    ["F5"] = "Refresh comments",
                    ["F1"] = "Show this help",
                    ["Ctrl+F"] = "Search (coming soon)"
                };

                foreach (var shortcut in shortcuts)
                {
                    if (string.IsNullOrEmpty(shortcut.Key))
                    {
                        stackPanel.Children.Add(new TextBlock { Height = 10 });
                        continue;
                    }

                    if (string.IsNullOrEmpty(shortcut.Value))
                    {
                        // Section header
                        var header = new TextBlock
                        {
                            Text = shortcut.Key,
                            FontWeight = FontWeights.Bold,
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50)),
                            Margin = new Thickness(0, 10, 0, 5)
                        };
                        stackPanel.Children.Add(header);
                    }
                    else
                    {
                        // Shortcut item
                        var panel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(10, 2, 0, 2)
                        };

                        var keyText = new TextBlock
                        {
                            Text = shortcut.Key,
                            FontFamily = new FontFamily("Consolas"),
                            FontWeight = FontWeights.SemiBold,
                            Width = 150,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0x49, 0x5E))
                        };

                        var descText = new TextBlock
                        {
                            Text = shortcut.Value,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x6C, 0x7D))
                        };

                        panel.Children.Add(keyText);
                        panel.Children.Add(descText);
                        stackPanel.Children.Add(panel);
                    }
                }

                scrollViewer.Content = stackPanel;
                helpWindow.Content = scrollViewer;
                helpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hiển thị help: {ex.Message}", "Lỗi",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Permission Helper Methods

        /// <summary>
        /// Helper method để convert bool thành Visibility
        /// </summary>
        private Visibility GetMenuItemVisibility(bool canShow)
        {
            return canShow ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Kiểm tra quyền xác nhận comment (Verify)
        /// Chỉ reviewer hoặc team leader trở lên
        /// </summary>
        private bool CanUserVerifyComment(TaskCommentModel comment)
        {
            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null || comment == null)
                return false;

            // Admin hoặc Project Manager
            if (PermissionProject.HasPermissionManagerProject())
                return true;

            // Team Leader của project
            if (PermissionProject.HasPermissionTeamLeaderProjectOfProject(_viewModel.ProjectId).GetAwaiter().GetResult())
                return true;

            // Reviewer của comment
            if (comment.ReviewerId.HasValue && currentUser.Id == comment.ReviewerId.Value)
                return true;

            return false;
        }

        /// <summary>
        /// Kiểm tra quyền assign comment
        /// Chỉ team leader trở lên
        /// </summary>
        private bool CanUserAssignComment(TaskCommentModel comment)
        {
            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null || comment == null)
                return false;

            // Admin hoặc Project Manager
            if (PermissionProject.HasPermissionManagerProject())
                return true;

            // Team Leader của project
            if (PermissionProject.HasPermissionTeamLeaderProjectOfProject(_viewModel.ProjectId).GetAwaiter().GetResult())
                return true;

            return false;
        }

        /// <summary>
        /// Kiểm tra quyền set reviewer
        /// Chỉ team leader trở lên
        /// </summary>
        private bool CanUserSetReviewer(TaskCommentModel comment)
        {
            return CanUserAssignComment(comment);
        }

        /// <summary>
        /// Kiểm tra quyền thay đổi priority hoặc status
        /// Assigned user, reviewer, hoặc team leader trở lên
        /// </summary>
        private bool CanUserChangePriorityOrStatus(TaskCommentModel comment)
        {
            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null || comment == null)
                return false;

            // Admin hoặc Project Manager
            if (PermissionProject.HasPermissionManagerProject())
                return true;

            // Team Leader của project
            if (PermissionProject.HasPermissionTeamLeaderProjectOfProject(_viewModel.ProjectId).GetAwaiter().GetResult())
                return true;

            // Kiểm tra quyền assigned hoặc reviewer của task comment
            if (true)
            {
                var isAssigned = PermissionProject.HasPermissionAssignedTaskComment(
                    _viewModel.ProjectId,
                    comment.TaskId,
                    comment.Id
                ).GetAwaiter().GetResult();

                var isReviewer = PermissionProject.HasPermssionReviewTaskComment(
                    _viewModel.ProjectId,
                    comment.TaskId,
                    comment.Id
                ).GetAwaiter().GetResult();

                if (isAssigned || isReviewer)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra quyền thay đổi comment type
        /// Chỉ team leader trở lên
        /// </summary>
        private bool CanUserChangeCommentType(TaskCommentModel comment)
        {
            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null || comment == null)
                return false;

            // Admin hoặc Project Manager
            if (PermissionProject.HasPermissionManagerProject())
                return true;

            // Team Leader của project
            if (PermissionProject.HasPermissionTeamLeaderProjectOfProject(_viewModel.ProjectId).GetAwaiter().GetResult())
                return true;

            return false;
        }

        /// <summary>
        /// Kiểm tra quyền toggle blocking flag
        /// Chỉ team leader trở lên
        /// </summary>
        private bool CanUserToggleBlocking(TaskCommentModel comment)
        {
            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null || comment == null)
                return false;

            // Admin hoặc Project Manager
            if (PermissionProject.HasPermissionManagerProject())
                return true;

            // Team Leader của project
            if (PermissionProject.HasPermissionTeamLeaderProjectOfProject(_viewModel.ProjectId).GetAwaiter().GetResult())
                return true;

            return false;
        }

        /// <summary>
        /// Kiểm tra quyền toggle discussion flag
        /// Assigned user, reviewer, creator, hoặc team leader trở lên
        /// </summary>
        private bool CanUserToggleDiscussion(TaskCommentModel comment)
        {
            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null || comment == null)
                return false;

            // Admin hoặc Project Manager
            if (PermissionProject.HasPermissionManagerProject())
                return true;

            // Team Leader của project
            if ( PermissionProject.HasPermissionTeamLeaderProjectOfProject(_viewModel.ProjectId).GetAwaiter().GetResult())
                return true;

            // Kiểm tra quyền assigned hoặc reviewer
            if (true)
            {
                var isAssigned = PermissionProject.HasPermissionAssignedTaskComment(
                    _viewModel.ProjectId,
                    comment.TaskId,
                    comment.Id
                ).GetAwaiter().GetResult();

                var isReviewer = PermissionProject.HasPermssionReviewTaskComment(
                    _viewModel.ProjectId,
                    comment.TaskId,
                    comment.Id
                ).GetAwaiter().GetResult();

                if (isAssigned || isReviewer)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra quyền xóa comment
        /// Chỉ creator (trong 24h, chưa có reply, chưa resolve), team leader trở lên, hoặc admin
        /// </summary>
        private bool CanUserDeleteComment(TaskCommentModel comment)
        {
            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null || comment == null)
                return false;

            // Admin hoặc Project Manager - luôn có quyền xóa
            if (PermissionProject.HasPermissionManagerProject())
                return true;

            // Team Leader của project
            if (PermissionProject.HasPermissionTeamLeaderProjectOfProject(_viewModel.ProjectId).GetAwaiter().GetResult())
                return true;

            // Creator - chỉ được xóa trong vòng 24h và thỏa mãn điều kiện
            if ( currentUser.Id == comment.CreatedById)
            {
                var hoursSinceCreated = (DateTime.Now - comment.CreatedAt).TotalHours;

                // Cho phép xóa nếu:
                // 1. Trong vòng 24h
                // 2. Chưa có reply
                // 3. Chưa được resolve
                if (hoursSinceCreated <= 24 &&
                    !comment.HasReplies &&
                    !comment.IsResolved)
                {
                    return true;
                }
            }

            return false;
        }




        #endregion

        #region Create Popup - Main Method

        /// <summary>
        /// Tạo Column Visibility Popup hoàn chỉnh bằng code
        /// </summary>
        private void CreateColumnVisibilityPopup()
        {
            try
            {
                // Main Popup
                _columnVisibilityPopup = new Popup
                {
                    PlacementTarget = ColumnVisibilityButton,
                    Placement = PlacementMode.Bottom,
                    StaysOpen = false,
                    AllowsTransparency = true
                };

                // Main Border with shadow
                var mainBorder = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(0),
                    MinWidth = 380,
                    MaxWidth = 450,
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.1,
                        ShadowDepth = 2,
                        BlurRadius = 8
                    }
                };

                // Main Grid
                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search & Filters
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

                // Add sections
                mainGrid.Children.Add(CreateHeaderSection());
                Grid.SetRow(mainGrid.Children[mainGrid.Children.Count - 1], 0);

                mainGrid.Children.Add(CreateScrollableContentSection());
                Grid.SetRow(mainGrid.Children[mainGrid.Children.Count - 1], 2);

                mainGrid.Children.Add(CreateFooterSection());
                Grid.SetRow(mainGrid.Children[mainGrid.Children.Count - 1], 3);

                mainBorder.Child = mainGrid;
                _columnVisibilityPopup.Child = mainBorder;

                // Event handlers
                _columnVisibilityPopup.Opened += (s, e) =>
                {
                    Mouse.Capture(_columnVisibilityPopup.Child, CaptureMode.SubTree);
                };

                _columnVisibilityPopup.Closed += (s, e) =>
                {
                    Mouse.Capture(null);
                };

                System.Diagnostics.Debug.WriteLine("✓ Column Visibility Popup created successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating popup: {ex.Message}");
            }
        }

        #endregion

        #region Create Header Section

        private Border CreateHeaderSection()
        {
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xF9, 0xFA)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(15, 10, 15, 10)
            };

            var stackPanel = new StackPanel();

            var titleText = new TextBlock
            {
                Text = "Chọn cột hiển thị",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var subtitleText = new TextBlock
            {
                Text = "Tùy chỉnh các cột hiển thị trong bảng",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(subtitleText);
            headerBorder.Child = stackPanel;

            return headerBorder;
        }

        #endregion

        #region Create Scrollable Content Section

        private ScrollViewer CreateScrollableContentSection()
        {
            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(15, 10, 15, 10)
            };

            _columnsStackPanel = new StackPanel();

            // Add column checkboxes by category
            AddColumnCategory(_columnsStackPanel, "📋 Cột cơ bản:", new[]
            {
                ("IdColumnCheckBox", "🆔 ID Comment", true),
                ("ContentColumnCheckBox", "📝 Nội dung", true),
                ("TypeColumnCheckBox", "🏷️ Loại comment", true),
                ("AuthorColumnCheckBox", "👤 Người tạo", true),
                ("TimeColumnCheckBox", "⏰ Thời gian", true)
            });

            AddSeparator(_columnsStackPanel);

            AddColumnCategory(_columnsStackPanel, "📊 Trạng thái và Ưu tiên:", new[]
            {
                ("StatusColumnCheckBox", "📊 Trạng thái comment", false),
                ("PriorityColumnCheckBox", "🎯 Ưu tiên", false)
            });

            AddSeparator(_columnsStackPanel);

            AddColumnCategory(_columnsStackPanel, "👥 Phân công:", new[]
            {
                ("AssigneeColumnCheckBox", "👥 Người thực hiện", false),
                ("ReviewerColumnCheckBox", "🔍 Người xác nhận", false)
            });

            AddSeparator(_columnsStackPanel);

            AddColumnCategory(_columnsStackPanel, "🐛 Chi tiết Issue:", new[]
            {
                ("IssueTitleColumnCheckBox", "🐛 Tiêu đề", false),
                ("SuggestedFixColumnCheckBox", "💡 Đề xuất cách sửa", false),
                ("RelatedModuleColumnCheckBox", "📦 Liên quan đến", false)
            });

            AddSeparator(_columnsStackPanel);

            AddColumnCategory(_columnsStackPanel, "⏰ Theo dõi thời gian:", new[]
            {
                ("DueDateColumnCheckBox", "📅 Thời gian hoàn thành", false),
                ("EstimatedTimeColumnCheckBox", "⏱️ Số giờ ước tính", false),
                ("ActualTimeColumnCheckBox", "⏲️ Số giờ thực tế", false)
            });

            AddSeparator(_columnsStackPanel);

            AddColumnCategory(_columnsStackPanel, "✅ Giải quyết:", new[]
            {
                ("ResolvedColumnCheckBox", "✅ Đã giải quyết", false),
                ("VerifiedColumnCheckBox", "🔒 Đã xác nhận", false)
            });

            AddSeparator(_columnsStackPanel);

            AddColumnCategory(_columnsStackPanel, "🚩 Flags:", new[]
            {
                ("BlockingColumnCheckBox", "🚫 Đã khóa", false),
                ("DiscussionColumnCheckBox", "💬 Cần thảo luận", false),
                ("TagsColumnCheckBox", "🏷️ Tags", false)
            });

            scrollViewer.Content = _columnsStackPanel;
            return scrollViewer;
        }

        private void AddColumnCategory(
            StackPanel parent, 
            string categoryTitle, 
            (string name, string content, bool isChecked)[] checkboxes)
        {
            // Category header
            var header = new TextBlock
            {
                Text = categoryTitle,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50))
            };
            parent.Children.Add(header);

            // Checkboxes
            foreach (var (name, content, isChecked) in checkboxes)
            {
                var checkBox = new CheckBox
                {
                    Name = name,
                    Content = content,
                    IsChecked = isChecked,
                    Margin = new Thickness(0, 4, 0, 4),
                    FontSize = 12
                };
                checkBox.Checked += ColumnCheckBox_Changed;
                checkBox.Unchecked += ColumnCheckBox_Changed;

                // Register checkbox in control's name scope
                try
                {
                    RegisterName(name, checkBox);
                }
                catch
                {
                    // Name already registered, that's okay
                }

                parent.Children.Add(checkBox);
            }
        }

        private void AddSeparator(StackPanel parent)
        {
            var separator = new Separator
            {
                Margin = new Thickness(0, 15, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8))
            };
            parent.Children.Add(separator);
        }

        #endregion

        #region Create Footer Section

        private Border CreateFooterSection()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xF9, 0xFA)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(15, 10, 15, 10)
            };

            var stackPanel = new StackPanel();

            // Quick Actions
            var actionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var showAllButton = CreateQuickActionButton("✅ Tất cả", Color.FromRgb(0x28, 0xA7, 0x45), ShowAllColumns_Click);
            actionsPanel.Children.Add(showAllButton);

            var showBasicButton = CreateQuickActionButton("📋 Cơ bản", Color.FromRgb(0x00, 0x7B, 0xFF), ShowBasicColumns_Click);
            actionsPanel.Children.Add(showBasicButton);

            var hideOptionalButton = CreateQuickActionButton("➖ Tùy chọn", Color.FromRgb(0x6C, 0x75, 0x7D), HideOptionalColumns_Click);
            actionsPanel.Children.Add(hideOptionalButton);

            stackPanel.Children.Add(actionsPanel);

            // Column Count Info
            _columnCountInfo = new TextBlock
            {
                Text = "Đang hiển thị 5/20 cột",
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D))
            };
            stackPanel.Children.Add(_columnCountInfo);

            border.Child = stackPanel;
            return border;
        }

        private Button CreateQuickActionButton(string content, Color backgroundColor, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = content,
                Background = new SolidColorBrush(backgroundColor),
                Foreground = Brushes.White,
                FontSize = 11,
                Padding = new Thickness(8, 4, 8 ,4),
                Height = 28,
                MinWidth = 60,
                Margin = new Thickness(2),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            button.Click += clickHandler;
            return button;
        }

        #endregion

        #region Event Handlers 

        private void ColumnSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchBox = sender as TextBox;
                if (searchBox == null) return;

                var placeholderText = "Nhập tên cột...";
                if (searchBox.Text == placeholderText)
                {
                    _columnSearchText = "";
                }
                else
                {
                    _columnSearchText = searchBox.Text?.Trim()?.ToLower() ?? "";
                }

                ApplyColumnFilters();
                UpdateActiveFiltersDisplay();

                System.Diagnostics.Debug.WriteLine($"Column search text changed: '{_columnSearchText}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ColumnSearchBox_TextChanged: {ex.Message}");
            }
        }

        private void CommentTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var comboBox = sender as ComboBox;
                if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    _selectedCommentTypeFilter = selectedItem.Tag?.ToString() ?? "All";

                    ApplyColumnFilters();
                    UpdateActiveFiltersDisplay();

                    System.Diagnostics.Debug.WriteLine($"Comment type filter changed: {_selectedCommentTypeFilter}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CommentTypeFilter_SelectionChanged: {ex.Message}");
            }
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var comboBox = sender as ComboBox;
                if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    _selectedStatusFilter = selectedItem.Tag?.ToString() ?? "All";

                    ApplyColumnFilters();
                    UpdateActiveFiltersDisplay();

                    System.Diagnostics.Debug.WriteLine($"Status filter changed: {_selectedStatusFilter}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in StatusFilter_SelectionChanged: {ex.Message}");
            }
        }

        private void PriorityFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var comboBox = sender as ComboBox;
                if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    _selectedPriorityFilter = selectedItem.Tag?.ToString() ?? "All";

                    ApplyColumnFilters();
                    UpdateActiveFiltersDisplay();

                    System.Diagnostics.Debug.WriteLine($"Priority filter changed: {_selectedPriorityFilter}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PriorityFilter_SelectionChanged: {ex.Message}");
            }
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear search box
                if (_columnSearchBox != null)
                {
                    _columnSearchBox.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xB5, 0xBD));
                }

                // Reset filter combos
                if (_commentTypeFilter != null)
                {
                    _commentTypeFilter.SelectedIndex = 0;
                }

                if (_statusFilter != null)
                {
                    _statusFilter.SelectedIndex = 0;
                }

                if (_priorityFilter != null)
                {
                    _priorityFilter.SelectedIndex = 0;
                }

                // Reset internal state
                _columnSearchText = "";
                _selectedCommentTypeFilter = "All";
                _selectedStatusFilter = "All";
                _selectedPriorityFilter = "All";

                // Reapply filters
                ApplyColumnFilters();
                UpdateActiveFiltersDisplay();

                System.Diagnostics.Debug.WriteLine("All column filters cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing filters: {ex.Message}");
            }
        }

        private void ApplyColumnFilters()
        {
            try
            {
                if (_currentColumnConfigs == null) return;

                int visibleCount = 0;
                int hiddenByFilterCount = 0;

                foreach (var config in _currentColumnConfigs)
                {
                    var checkBox = FindName(config.CheckBoxName) as CheckBox;
                    if (checkBox == null) continue;

                    bool shouldShow = ShouldShowColumn(config);

                    checkBox.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;

                    if (shouldShow)
                    {
                        visibleCount++;
                    }
                    else
                    {
                        hiddenByFilterCount++;
                    }
                }

                UpdateSectionHeadersVisibility();

                System.Diagnostics.Debug.WriteLine(
                    $"Column filters applied: {visibleCount} visible, {hiddenByFilterCount} hidden by filters");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying column filters: {ex.Message}");
            }
        }

        private bool ShouldShowColumn(ColumnConfig config)
        {
            if (config == null) return false;

            // Search filter
            if (!string.IsNullOrEmpty(_columnSearchText))
            {
                var searchTerms = _columnSearchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                bool matchesSearch = searchTerms.All(term =>
                    config.DisplayName.ToLower().Contains(term) ||
                    config.ColumnName.ToLower().Contains(term) ||
                    config.Category.ToLower().Contains(term));

                if (!matchesSearch) return false;
            }

            // Type/Status/Priority filters
            if (_selectedCommentTypeFilter != "All" && config.Category != "Basic" && config.ColumnName != "TypeColumn")
            {
                return false;
            }

            if (_selectedStatusFilter != "All" && config.Category != "Status" && config.Category != "Resolution" && config.Category != "Basic")
            {
                return false;
            }

            if (_selectedPriorityFilter != "All" && config.Category != "Status" && config.Category != "Basic")
            {
                return false;
            }

            return true;
        }

        private void UpdateSectionHeadersVisibility()
        {
            try
            {
                if (_columnsStackPanel == null) return;

                foreach (var child in _columnsStackPanel.Children)
                {
                    if (child is TextBlock header && header.FontWeight == FontWeights.SemiBold)
                    {
                        // Check if any checkbox in this section is visible
                        int headerIndex = _columnsStackPanel.Children.IndexOf(header);
                        bool hasVisibleCheckboxes = false;

                        for (int i = headerIndex + 1; i < _columnsStackPanel.Children.Count; i++)
                        {
                            if (_columnsStackPanel.Children[i] is CheckBox checkBox)
                            {
                                if (checkBox.Visibility == Visibility.Visible)
                                {
                                    hasVisibleCheckboxes = true;
                                    break;
                                }
                            }
                            else if (_columnsStackPanel.Children[i] is Separator)
                            {
                                break; // End of section
                            }
                        }

                        header.Visibility = hasVisibleCheckboxes ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else if (child is Separator separator)
                    {
                        // Hide separator if next header is hidden
                        int separatorIndex = _columnsStackPanel.Children.IndexOf(separator);
                        if (separatorIndex + 1 < _columnsStackPanel.Children.Count)
                        {
                            var nextElement = _columnsStackPanel.Children[separatorIndex + 1];
                            separator.Visibility = nextElement is TextBlock nextHeader &&
                                                  nextHeader.Visibility == Visibility.Visible
                                ? Visibility.Visible
                                : Visibility.Collapsed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating section headers: {ex.Message}");
            }
        }

        private void UpdateActiveFiltersDisplay()
        {
            try
            {
                var activeFilters = new List<string>();

                if (!string.IsNullOrEmpty(_columnSearchText))
                {
                    activeFilters.Add($"Search: '{_columnSearchText}'");
                }

                if (_selectedCommentTypeFilter != "All")
                {
                    activeFilters.Add($"Type: {_selectedCommentTypeFilter}");
                }

                if (_selectedStatusFilter != "All")
                {
                    activeFilters.Add($"Status: {_selectedStatusFilter}");
                }

                if (_selectedPriorityFilter != "All")
                {
                    activeFilters.Add($"Priority: {_selectedPriorityFilter}");
                }

                if (_activeFiltersPanel != null && _activeFiltersText != null)
                {
                    if (activeFilters.Count > 0)
                    {
                        _activeFiltersPanel.Visibility = Visibility.Visible;
                        _activeFiltersText.Text = string.Join(" • ", activeFilters);
                    }
                    else
                    {
                        _activeFiltersPanel.Visibility = Visibility.Collapsed;
                        _activeFiltersText.Text = "";
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Active filters: {string.Join(", ", activeFilters)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating active filters display: {ex.Message}");
            }
        }

        private void ShowAllColumns_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentColumnConfigs == null) return;

                foreach (var config in _currentColumnConfigs)
                {
                    var checkBox = FindName(config.CheckBoxName) as CheckBox;
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = true;
                    }
                }

                UpdateColumnVisibilityButtonText();
                UpdateColumnCountInfo();

                System.Diagnostics.Debug.WriteLine("Showed all columns");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing all columns: {ex.Message}");
            }
        }

        private void ShowBasicColumns_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentColumnConfigs == null) return;

                var basicCheckboxNames = new[]
                {
                    "IdColumnCheckBox",
                    "ContentColumnCheckBox",
                    "TypeColumnCheckBox",
                    "AuthorColumnCheckBox",
                    "TimeColumnCheckBox"
                };

                foreach (var config in _currentColumnConfigs)
                {
                    var checkBox = FindName(config.CheckBoxName) as CheckBox;
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = basicCheckboxNames.Contains(config.CheckBoxName);
                    }
                }

                UpdateColumnVisibilityButtonText();
                UpdateColumnCountInfo();

                System.Diagnostics.Debug.WriteLine("Showed basic columns only");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing basic columns: {ex.Message}");
            }
        }

        private void HideOptionalColumns_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentColumnConfigs == null) return;

                var essentialCheckboxNames = new[]
                {
                    "IdColumnCheckBox",
                    "ContentColumnCheckBox",
                    "AuthorColumnCheckBox",
                    "TimeColumnCheckBox"
                };

                foreach (var config in _currentColumnConfigs)
                {
                    var checkBox = FindName(config.CheckBoxName) as CheckBox;
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = essentialCheckboxNames.Contains(config.CheckBoxName);
                    }
                }

                UpdateColumnVisibilityButtonText();
                UpdateColumnCountInfo();

                System.Diagnostics.Debug.WriteLine("Hid optional columns");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding optional columns: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show column visibility popup
        /// </summary>
        public void ShowColumnVisibilityPopup()
        {
            try
            {
                if (_columnVisibilityPopup == null)
                {
                    CreateColumnVisibilityPopup();
                }

                if (_columnVisibilityPopup != null)
                {
                    _columnVisibilityPopup.IsOpen = true;
                    System.Diagnostics.Debug.WriteLine("Column visibility popup opened");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing popup: {ex.Message}");
            }
        }

        /// <summary>
        /// Hide column visibility popup
        /// </summary>
        public void HideColumnVisibilityPopup()
        {
            try
            {
                if (_columnVisibilityPopup != null)
                {
                    _columnVisibilityPopup.IsOpen = false;
                    System.Diagnostics.Debug.WriteLine("Column visibility popup closed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding popup: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggle column visibility popup
        /// </summary>
        public void ToggleColumnVisibilityPopup()
        {
            try
            {
                if (_columnVisibilityPopup == null)
                {
                    CreateColumnVisibilityPopup();
                }

                if (_columnVisibilityPopup != null)
                {
                    _columnVisibilityPopup.IsOpen = !_columnVisibilityPopup.IsOpen;
                    System.Diagnostics.Debug.WriteLine($"Column visibility popup toggled: {_columnVisibilityPopup.IsOpen}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling popup: {ex.Message}");
            }
        }

        /// <summary>
        /// Get filter statistics
        /// </summary>
        public string GetFilterStatistics()
        {
            var stats = new StringBuilder();
            stats.AppendLine("Column Visibility Filter Statistics:");
            stats.AppendLine($"- Search text: {(_columnSearchText ?? "None")}");
            stats.AppendLine($"- Comment type: {_selectedCommentTypeFilter}");
            stats.AppendLine($"- Status: {_selectedStatusFilter}");
            stats.AppendLine($"- Priority: {_selectedPriorityFilter}");

            if (_currentColumnConfigs != null)
            {
                int visibleColumns = 0;
                int hiddenColumns = 0;

                foreach (var config in _currentColumnConfigs)
                {
                    var checkBox = FindName(config.CheckBoxName) as CheckBox;
                    if (checkBox != null)
                    {
                        if (checkBox.Visibility == Visibility.Visible)
                            visibleColumns++;
                        else
                            hiddenColumns++;
                    }
                }

                stats.AppendLine($"- Visible columns: {visibleColumns}");
                stats.AppendLine($"- Hidden columns: {hiddenColumns}");
            }

            return stats.ToString();
        }

        /// <summary>
        /// Export column filter settings
        /// </summary>
        public Dictionary<string, object> ExportColumnFilterSettings()
        {
            return new Dictionary<string, object>
            {
                { "SearchText", _columnSearchText },
                { "CommentTypeFilter", _selectedCommentTypeFilter },
                { "StatusFilter", _selectedStatusFilter },
                { "PriorityFilter", _selectedPriorityFilter },
                { "Timestamp", DateTime.Now },
                { "User", App.GetCurrentUser()?.Username ?? "Unknown" }
            };
        }

        /// <summary>
        /// Import column filter settings
        /// </summary>
        public void ImportColumnFilterSettings(Dictionary<string, object> settings)
        {
            try
            {
                if (settings == null) return;

                if (settings.ContainsKey("SearchText") && _columnSearchBox != null)
                {
                    _columnSearchText = settings["SearchText"]?.ToString() ?? "";
                    _columnSearchBox.Text = string.IsNullOrEmpty(_columnSearchText) ? "Nhập tên cột..." : _columnSearchText;
                    _columnSearchBox.Foreground = string.IsNullOrEmpty(_columnSearchText)
                        ? new SolidColorBrush(Color.FromRgb(0xAD, 0xB5, 0xBD))
                        : Brushes.Black;
                }

                if (settings.ContainsKey("CommentTypeFilter") && _commentTypeFilter != null)
                {
                    _selectedCommentTypeFilter = settings["CommentTypeFilter"]?.ToString() ?? "All";
                    SelectComboBoxItemByTag(_commentTypeFilter, _selectedCommentTypeFilter);
                }

                if (settings.ContainsKey("StatusFilter") && _statusFilter != null)
                {
                    _selectedStatusFilter = settings["StatusFilter"]?.ToString() ?? "All";
                    SelectComboBoxItemByTag(_statusFilter, _selectedStatusFilter);
                }

                if (settings.ContainsKey("PriorityFilter") && _priorityFilter != null)
                {
                    _selectedPriorityFilter = settings["PriorityFilter"]?.ToString() ?? "All";
                    SelectComboBoxItemByTag(_priorityFilter, _selectedPriorityFilter);
                }

                ApplyColumnFilters();
                UpdateActiveFiltersDisplay();

                System.Diagnostics.Debug.WriteLine("Column filter settings imported successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing filter settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to select ComboBox item by tag
        /// </summary>
        private void SelectComboBoxItemByTag(ComboBox comboBox, string tag)
        {
            if (comboBox == null || string.IsNullOrEmpty(tag)) return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == tag)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        #endregion

        #region Initialization Integration

        /// <summary>
        /// Enhanced initialization to include popup creation
        /// Call this in your existing constructor or Loaded event
        /// </summary>
        private void InitializeColumnVisibilityPopup()
        {
            try
            {
                // Create the popup
                CreateColumnVisibilityPopup();

                // Wire up the visibility button click event
                if (ColumnVisibilityButton != null)
                {
                    ColumnVisibilityButton.Click -= ColumnVisibilityButton_Click;
                    ColumnVisibilityButton.Click += ColumnVisibilityButton_Click;
                }

                System.Diagnostics.Debug.WriteLine("✓ Column visibility popup initialization completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing column visibility popup: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle column visibility button click - Updated
        /// </summary>
        private void ColumnVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleColumnVisibilityPopup();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ColumnVisibilityButton_Click: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods for Column Management

        /// <summary>
        /// Update column count info display
        /// </summary>
        private void UpdateColumnCountInfo()
        {
            try
            {
                if (_columnCountInfo == null || _currentColumnConfigs == null) return;

                int visibleCount = 0;
                int totalCount = _currentColumnConfigs.Count;

                foreach (var config in _currentColumnConfigs)
                {
                    var checkBox = FindName(config.CheckBoxName) as CheckBox;
                    if (checkBox?.IsChecked == true)
                    {
                        visibleCount++;
                    }
                }

                _columnCountInfo.Text = $"Đang hiển thị {visibleCount}/{totalCount} cột";

                // Change color based on count
                if (visibleCount == 0)
                {
                    _columnCountInfo.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (visibleCount <= 5)
                {
                    _columnCountInfo.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
                }
                else if (visibleCount <= 10)
                {
                    _columnCountInfo.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                }
                else
                {
                    _columnCountInfo.Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating column count info: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup popup resources
        /// </summary>
        private void CleanupColumnVisibilityPopup()
        {
            try
            {
                if (_columnVisibilityPopup != null)
                {
                    _columnVisibilityPopup.IsOpen = false;
                    _columnVisibilityPopup = null;
                }

                // Clear references
                _columnSearchBox = null;
                _commentTypeFilter = null;
                _statusFilter = null;
                _priorityFilter = null;
                _activeFiltersPanel = null;
                _activeFiltersText = null;
                _clearFiltersButton = null;
                _columnsStackPanel = null;
                _columnCountInfo = null;

                System.Diagnostics.Debug.WriteLine("✓ Column visibility popup cleaned up");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up popup: {ex.Message}");
            }
        }

        #endregion
    }


    /// <summary>
    /// Model để track column visibility changes
    /// </summary>
    public class ColumnVisibilityChange
    {
        public string ColumnName { get; set; }
        public string CheckBoxName { get; set; }
        public bool OldValue { get; set; }
        public bool NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
        public string Reason { get; set; } = "User preference";

        public override string ToString()
        {
            return $"{ChangedAt:HH:mm:ss} - {ColumnName}: {OldValue} → {NewValue} by {ChangedBy}";
        }
    }
}
