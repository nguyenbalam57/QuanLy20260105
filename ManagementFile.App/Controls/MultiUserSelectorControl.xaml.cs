using ManagementFile.App.Models;
using ManagementFile.App.Services;
using ManagementFile.Contracts.DTOs.UserManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ManagementFile.App.Models.Users;

namespace ManagementFile.App.Controls
{
    /// <summary>
    /// MultiUserSelector Control with search, mention functionality
    /// </summary>
    public partial class MultiUserSelectorControl : UserControl, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _searchDelayTimer;
        private readonly UserManagementService _userService;
        private readonly ProjectApiService _projectService;
        private int _currentPage = 1;
        private const int PageSize = 20;
        private bool _hasMoreItems = true;
        private bool _isLoading = false;
        private string _lastSearchText = "";

        // Internal properties (NO DataContext binding)
        private string _searchText = "";
        private ObservableCollection<UserModel> _users = new ObservableCollection<UserModel>();
        private string _searchStatusText = "Nhập để tìm kiếm...";
        private string _footerText = "Nhấn F5 để làm mới danh sách";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Dependency Properties

        public static readonly DependencyProperty SelectedUsersProperty =
            DependencyProperty.Register("SelectedUsers", typeof(ObservableCollection<UserModel>),
                typeof(MultiUserSelectorControl),
                new FrameworkPropertyMetadata(new ObservableCollection<UserModel>(),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedUsersChanged));

        private static void OnSelectedUsersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiUserSelectorControl)d;
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] SelectedUsers changed from {(e.OldValue as ObservableCollection<UserModel>)?.Count ?? 0} to {(e.NewValue as ObservableCollection<UserModel>)?.Count ?? 0} users");

            if (e.NewValue != null && !(e.NewValue is ObservableCollection<UserModel>))
            {
                control.SelectedUsers = new ObservableCollection<UserModel>();
            }
            control.UpdateSelectedUsersDisplay();
            control.OnPropertyChanged(nameof(control.SelectedUsers));
        }

        public ObservableCollection<UserModel> SelectedUsers
        {
            get => (ObservableCollection<UserModel>)GetValue(SelectedUsersProperty);
            set => SetValue(SelectedUsersProperty, value);
        }

        public static readonly DependencyProperty ProjectIdProperty =
            DependencyProperty.Register("ProjectId", typeof(int), typeof(MultiUserSelectorControl),
                new PropertyMetadata(0, OnProjectIdChanged));

        private static void OnProjectIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiUserSelectorControl)d;
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] ProjectId changed from {e.OldValue} to {e.NewValue}");
        }

        public int ProjectId
        {
            get => (int)GetValue(ProjectIdProperty);
            set => SetValue(ProjectIdProperty, value);
        }

        public static readonly DependencyProperty SearchScopeProperty =
            DependencyProperty.Register("SearchScope", typeof(string), typeof(MultiUserSelectorControl),
                new PropertyMetadata("AllUsers", OnSearchScopeChanged));

        private static void OnSearchScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiUserSelectorControl)d;
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] SearchScope changed from {e.OldValue} to {e.NewValue}");
        }

        public string SearchScope
        {
            get => (string)GetValue(SearchScopeProperty);
            set => SetValue(SearchScopeProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(MultiUserSelectorControl),
                new PropertyMetadata("@Mention người dùng...", OnPlaceholderChanged));

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiUserSelectorControl)d;
            control.UpdatePlaceholder();
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(MultiUserSelectorControl),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MultiUserSelectorControl)d;
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] IsReadOnly changed from {e.OldValue} to {e.NewValue}");
            control.UpdateReadOnlyState();
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion

        #region Internal Properties (Manual UI Management)

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    SearchTextBox.Text = value;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(value) && value != Placeholder)
                    {
                        _searchDelayTimer.Stop();
                        _searchDelayTimer.Start();
                    }
                }
            }
        }

        public ObservableCollection<UserModel> Users
        {
            get => _users;
            set
            {
                _users = value;
                SearchResultsList.ItemsSource = _users;
                OnPropertyChanged();
            }
        }

        public string SearchStatusText
        {
            get => _searchStatusText;
            set
            {
                _searchStatusText = value;
                OnPropertyChanged();
            }
        }

        public string FooterText
        {
            get => _footerText;
            set
            {
                _footerText = value;
                OnPropertyChanged();
            }
        }

        public bool HasSelectedUsers => SelectedUsers != null && SelectedUsers.Count > 0;

        #endregion

        public MultiUserSelectorControl()
        {
            InitializeComponent();

            // DON'T set DataContext = this - Let parent handle DataContext

            // Initialize services
            _userService = App.GetService<UserManagementService>();
            _projectService = App.GetService<ProjectApiService>();

            // Setup search delay timer
            _searchDelayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDelayTimer.Tick += SearchDelayTimer_Tick;

            // Initialize UI
            Loaded += OnControlLoaded;

            // Initialize SelectedUsers if null
            if (SelectedUsers == null)
            {
                SelectedUsers = new ObservableCollection<UserModel>();
            }

            // Setup manual ItemsSource
            SearchResultsList.ItemsSource = _users;
            SelectedUsersPanel.ItemsSource = SelectedUsers;

            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] Constructor completed");
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] Control loaded");
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] ProjectId: {ProjectId}");
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] SearchScope: {SearchScope}");
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] IsReadOnly: {IsReadOnly}");
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] Placeholder: {Placeholder}");

            UpdatePlaceholder();
            UpdateReadOnlyState();
            UpdateSelectedUsersDisplay();

            // Setup scroll event for pagination
            var scrollViewer = GetScrollViewer(SearchResultsList);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
        }

        #region UI Update Methods

        private void UpdatePlaceholder()
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                SearchTextBox.Text = Placeholder ?? "@Mention người dùng...";
                _searchText = Placeholder ?? "@Mention người dùng...";
            }
        }

        private void UpdateReadOnlyState()
        {
            SearchTextBox.IsEnabled = !IsReadOnly;
            SearchButton.IsEnabled = !IsReadOnly;
        }

        private void UpdateSelectedUsersDisplay()
        {
            SelectedUsersPanel.ItemsSource = SelectedUsers;
            SelectedUsersPanel.Visibility = HasSelectedUsers ? Visibility.Visible : Visibility.Collapsed;
            OnPropertyChanged(nameof(HasSelectedUsers));
        }

        #endregion

        #region Helper Methods

        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer scrollViewer)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        #endregion

        #region Search Logic

        private async void SearchDelayTimer_Tick(object sender, EventArgs e)
        {
            _searchDelayTimer.Stop();

            System.Diagnostics.Debug.WriteLine($"[2025-10-10 00:54:59] MultiUserSelectorControl search triggered by user 'nguyenbalam57' with text: '{SearchText}'");

            await SearchUsers();
        }

        private async Task SearchUsers()
        {
            if (_isLoading || (SearchText == Placeholder) || string.IsNullOrWhiteSpace(SearchText))
                return;

            _isLoading = true;
            _currentPage = 1;
            SearchStatusText = "Đang tìm kiếm...";

            try
            {
                _lastSearchText = SearchText;
                Users.Clear();
                var results = await FetchUsers(SearchText, _currentPage);

                foreach (var user in results)
                {
                    // Don't show users that are already selected
                    if (SelectedUsers == null || !SelectedUsers.Any(u => u.Id == user.Id))
                    {
                        Users.Add(user);
                    }
                }

                _hasMoreItems = results.Count >= PageSize;
                UpdateStatusText(results.Count);

                if (Users.Count > 0 && !SearchResultsPopup.IsOpen)
                {
                    SearchResultsPopup.IsOpen = true;
                }
                else if (Users.Count == 0 && !string.IsNullOrWhiteSpace(SearchText))
                {
                    SearchStatusText = "❌ Không tìm thấy người dùng";
                    if (!SearchResultsPopup.IsOpen)
                    {
                        SearchResultsPopup.IsOpen = true;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] MultiUserSelectorControl found {Users.Count} users for search '{SearchText}'");
            }
            catch (Exception ex)
            {
                SearchStatusText = $"Lỗi: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] MultiUserSelectorControl search error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadMoreUsers()
        {
            if (_isLoading || !_hasMoreItems)
                return;

            _isLoading = true;
            _currentPage++;
            FooterText = "Đang tải thêm...";

            try
            {
                var results = await FetchUsers(_lastSearchText, _currentPage);

                foreach (var user in results)
                {
                    // Don't show users that are already selected
                    if (SelectedUsers == null || !SelectedUsers.Any(u => u.Id == user.Id))
                    {
                        Users.Add(user);
                    }
                }

                _hasMoreItems = results.Count >= PageSize;
                FooterText = _hasMoreItems ? "Kéo xuống để tải thêm" : "Đã tải tất cả";
            }
            catch (Exception ex)
            {
                FooterText = $"Lỗi: {ex.Message}";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task<List<UserModel>> FetchUsers(string searchText, int page)
        {
            System.Diagnostics.Debug.WriteLine($"[MultiUserSelector] FetchUsers - ProjectId: {ProjectId}, SearchScope: {SearchScope}, Page: {page}");

            if (SearchScope == "ProjectMembers" && ProjectId > 0)
            {
                var searchUserOption = new UserSearchOptions
                {
                    FilterMode = UserFilterMode.ProjectMembersOnly,
                    ProjectId = ProjectId,
                    PageNumber = page,
                };
                return await _userService.SearchUsersAsync(searchText, searchUserOption);
            }
            else
            {
                var searchOptions = new UserSearchOptions
                {
                    PageNumber = page,
                    FilterMode = UserFilterMode.AllUsers
                };
                return await _userService.SearchUsersAsync(searchText, searchOptions);
            }
        }

        private void UpdateStatusText(int resultCount)
        {
            if (resultCount == 0)
            {
                SearchStatusText = "❌ Không tìm thấy người dùng";
            }
            else if (resultCount == 1)
            {
                SearchStatusText = "✅ Tìm thấy 1 người dùng";
            }
            else
            {
                SearchStatusText = $"✅ Tìm thấy {resultCount} người dùng";
            }
        }

        #endregion

        #region Event Handlers

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null &&
                scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 20 &&
                _hasMoreItems &&
                !_isLoading &&
                Users.Count > 0)
            {
                try
                {
                    await LoadMoreUsers();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[2025-10-10 00:58:53] Error in LoadMoreUsers: {ex.Message}");
                }
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SearchUsers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[2025-10-10 00:58:53] Error in RefreshButton_Click: {ex.Message}");
                SearchStatusText = $"Lỗi: {ex.Message}";
            }
        }

        private async void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            // Update internal search text from TextBox
            SearchText = SearchTextBox.Text;

            switch (e.Key)
            {
                case Key.Down:
                    if (SearchResultsPopup.IsOpen && Users.Count > 0)
                    {
                        SearchResultsList.SelectedIndex = 0;
                        var item = SearchResultsList.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                        item?.Focus();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(SearchText) && SearchText != Placeholder)
                        {
                            SearchResultsPopup.IsOpen = true;
                            _searchDelayTimer.Stop();
                            _searchDelayTimer.Start();
                        }
                    }
                    break;

                case Key.Escape:
                    SearchResultsPopup.IsOpen = false;
                    break;

                case Key.F5:
                    try
                    {
                        await SearchUsers();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[2025-10-10 00:58:53] Error in F5 SearchUsers: {ex.Message}");
                        SearchStatusText = $"Lỗi: {ex.Message}";
                    }
                    break;
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == Placeholder)
            {
                SearchTextBox.Text = "";
                SearchText = "";
            }

            if (!IsReadOnly && !string.IsNullOrEmpty(SearchText) && SearchText != Placeholder)
            {
                SearchResultsPopup.IsOpen = true;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                SearchTextBox.Text = Placeholder;
                SearchText = Placeholder;
            }

            // Delay closing to allow clicking on popup items
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!SearchResultsList.IsKeyboardFocusWithin)
                {
                    SearchResultsPopup.IsOpen = false;
                }
            }), DispatcherPriority.Input);
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SearchResultsPopup.IsOpen)
            {
                SearchResultsPopup.IsOpen = true;
                try
                {
                    await SearchUsers();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[2025-10-10 00:58:53] Error in SearchButton_Click: {ex.Message}");
                    SearchStatusText = $"Lỗi: {ex.Message}";
                }
            }
            else
            {
                SearchResultsPopup.IsOpen = false;
            }
        }

        private void SearchResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsList.SelectedItem is UserModel selectedUser)
            {
                AddUser(selectedUser);
                SearchResultsPopup.IsOpen = false;
            }
        }

        private void SearchResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResultsList.SelectedItem is UserModel selectedUser)
            {
                AddUser(selectedUser);
                SearchResultsPopup.IsOpen = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SearchResultsPopup.IsOpen = false;
                SearchTextBox.Focus();
                e.Handled = true;
            }
        }

        private void RemoveUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var userToRemove = SelectedUsers?.FirstOrDefault(u => u.Id == userId);
                if (userToRemove != null)
                {
                    SelectedUsers.Remove(userToRemove);
                    UpdateSelectedUsersDisplay();

                    System.Diagnostics.Debug.WriteLine($"[2025-10-10 00:58:53] User 'nguyenbalam57' removed user '{userToRemove.UserName}' from mention list");
                }
            }
        }

        private void AddUser(UserModel user)
        {
            if (user != null && (SelectedUsers == null || !SelectedUsers.Any(u => u.Id == user.Id)))
            {
                if (SelectedUsers == null)
                {
                    SelectedUsers = new ObservableCollection<UserModel>();
                }

                SelectedUsers.Add(user);
                SearchText = "";
                SearchTextBox.Text = Placeholder;
                UpdateSelectedUsersDisplay();

                System.Diagnostics.Debug.WriteLine($"[2025-10-10 00:58:53] User 'nguyenbalam57' added user '{user.UserName}' to mention list");
            }
        }

        #endregion
    }
}