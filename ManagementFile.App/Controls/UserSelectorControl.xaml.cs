using ManagementFile.App.Models;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using ManagementFile.Contracts.DTOs.UserManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ManagementFile.App.Controls
{
    /// <summary>
    /// Interaction logic for UserSelectorControl.xaml
    /// </summary>
    public partial class UserSelectorControl : UserControl, INotifyPropertyChanged
    {
        private readonly UserManagementService _userService;
        private CancellationTokenSource _searchCts;
        private DispatcherTimer _debounceTimer;

        // Internal state properties (NO DataContext binding)
        private string _searchText = "";
        private bool _isDropdownOpen;
        private bool _isLoading;
        private ObservableCollection<UserModel> _users = new ObservableCollection<UserModel>();

        #region Dependency Properties (unchanged)

        public static readonly DependencyProperty SelectedUserProperty =
            DependencyProperty.Register("SelectedUser", typeof(UserModel), typeof(UserSelectorControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedUserChanged));

        public static readonly DependencyProperty ProjectIdProperty =
            DependencyProperty.Register("ProjectId", typeof(int), typeof(UserSelectorControl),
                new PropertyMetadata(0, OnProjectIdChanged));

        public static readonly DependencyProperty SearchScopeProperty =
            DependencyProperty.Register("SearchScope", typeof(string), typeof(UserSelectorControl),
                new PropertyMetadata("ProjectMembers", OnSearchScopeChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(UserSelectorControl),
                new PropertyMetadata("Chọn người dùng...", OnPlaceholderChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(UserSelectorControl),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        #endregion

        #region Properties

        public UserModel SelectedUser
        {
            get => (UserModel)GetValue(SelectedUserProperty);
            set => SetValue(SelectedUserProperty, value);
        }

        public int ProjectId
        {
            get => (int)GetValue(ProjectIdProperty);
            set => SetValue(ProjectIdProperty, value);
        }

        public string SearchScope
        {
            get => (string)GetValue(SearchScopeProperty);
            set => SetValue(SearchScopeProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        // Internal properties for UI management
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    SearchBox.Text = value;
                    OnPropertyChanged();
                    if (!string.IsNullOrEmpty(value))
                    {
                        StartSearch();
                    }
                }
            }
        }

        public bool IsDropdownOpen
        {
            get => _isDropdownOpen;
            set
            {
                if (_isDropdownOpen != value)
                {
                    _isDropdownOpen = value;
                    System.Diagnostics.Debug.WriteLine($"[UserSelector] IsDropdownOpen set to: {value}");

                    // Update UI
                    UsersPopup.IsOpen = value;

                    if (value)
                    {
                        // Show search box, hide others
                        SearchBox.Visibility = Visibility.Visible;
                        SelectedUserDisplay.Visibility = Visibility.Collapsed;
                        PlaceholderText.Visibility = Visibility.Collapsed;
                        ActionIcon.Text = "▲";

                        // Load users if needed
                        if (_users.Count == 0)
                        {
                            LoadInitialUsersAsync();
                        }

                        // Focus search box
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                        {
                            SearchBox.Focus();
                            SearchBox.SelectAll();
                        }));
                    }
                    else
                    {
                        // Hide search box, show appropriate display
                        SearchBox.Visibility = Visibility.Collapsed;
                        SearchText = "";
                        UpdateSelectedUserDisplay();
                    }

                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    UpdateLoadingState();
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<UserModel> Users
        {
            get => _users;
            private set
            {
                if (_users != null)
                    _users.CollectionChanged -= Users_CollectionChanged;

                _users = value;

                if (_users != null)
                    _users.CollectionChanged += Users_CollectionChanged;

                UsersListBox.ItemsSource = _users;
                UpdateUserCount();
                OnPropertyChanged();
            }
        }

        #endregion

        public UserSelectorControl()
        {
            InitializeComponent();

            // Get service
            _userService = App.GetService<UserManagementService>();

            // Initialize debounce timer
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _debounceTimer.Tick += DebounceTimer_Tick;

            // Subscribe to collection changes
            _users.CollectionChanged += Users_CollectionChanged;
            UsersListBox.ItemsSource = _users;

            // Initialize UI
            Loaded += OnControlLoaded;

            System.Diagnostics.Debug.WriteLine($"[UserSelector] Constructor completed");
        }

        #region Event Handlers

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSelector] Control loaded");
            System.Diagnostics.Debug.WriteLine($"[UserSelector] ProjectId: {ProjectId}");
            System.Diagnostics.Debug.WriteLine($"[UserSelector] SearchScope: {SearchScope}");
            System.Diagnostics.Debug.WriteLine($"[UserSelector] IsReadOnly: {IsReadOnly}");

            UpdatePlaceholder();
            UpdateSelectedUserDisplay();
            UpdateReadOnlyState();

            // Load initial users if we have ProjectId
            if (ProjectId > 0)
            {
                LoadInitialUsersAsync();
            }
        }

        private static void OnSelectedUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UserSelectorControl)d;
            System.Diagnostics.Debug.WriteLine($"[UserSelector] SelectedUser changed from {(e.OldValue as UserModel)?.FullName ?? "NULL"} to {(e.NewValue as UserModel)?.FullName ?? "NULL"}");

            control.UpdateSelectedUserDisplay();
            control.OnPropertyChanged(nameof(control.SelectedUser));
        }

        private static void OnProjectIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UserSelectorControl)d;
            System.Diagnostics.Debug.WriteLine($"[UserSelector] ProjectId changed from {e.OldValue} to {e.NewValue}");

            if ((int)e.NewValue > 0 && control.IsLoaded)
            {
                control.LoadInitialUsersAsync();
            }
        }

        private static void OnSearchScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UserSelectorControl)d;
            System.Diagnostics.Debug.WriteLine($"[UserSelector] SearchScope changed from {e.OldValue} to {e.NewValue}");
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UserSelectorControl)d;
            control.UpdatePlaceholder();
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UserSelectorControl)d;
            System.Diagnostics.Debug.WriteLine($"[UserSelector] IsReadOnly changed from {e.OldValue} to {e.NewValue}");
            control.UpdateReadOnlyState();
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSelector] ActionButton_Click - IsReadOnly: {IsReadOnly}, SelectedUser: {SelectedUser?.FullName ?? "NULL"}");

            if (IsReadOnly) return;

            if (SelectedUser != null)
            {
                // Clear selection
                System.Diagnostics.Debug.WriteLine($"[UserSelector] Clearing selection");
                SelectedUser = null;
            }
            else
            {
                // Toggle dropdown
                System.Diagnostics.Debug.WriteLine($"[UserSelector] Toggling dropdown from {IsDropdownOpen} to {!IsDropdownOpen}");
                IsDropdownOpen = !IsDropdownOpen;
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!IsReadOnly && !IsDropdownOpen)
            {
                IsDropdownOpen = true;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Delay closing to allow selection
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (!UsersListBox.IsKeyboardFocusWithin && !SearchBox.IsKeyboardFocused)
                {
                    IsDropdownOpen = false;
                }
            }));
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SearchText = SearchBox.Text;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    if (UsersListBox.Items.Count > 0)
                    {
                        UsersListBox.SelectedIndex = 0;
                        var firstItem = UsersListBox.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                        firstItem?.Focus();
                    }
                    e.Handled = true;
                    break;

                case Key.Escape:
                    IsDropdownOpen = false;
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (UsersListBox.SelectedItem is UserModel selectedUser)
                    {
                        SelectUser(selectedUser);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void UsersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UsersListBox.SelectedItem is UserModel selectedUser)
            {
                System.Diagnostics.Debug.WriteLine($"[UserSelector] Mouse double click on: {selectedUser.FullName}");
                SelectUser(selectedUser);
            }
        }

        private void UsersListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (UsersListBox.SelectedItem is UserModel selectedUser)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UserSelector] Enter key pressed on: {selectedUser.FullName}");
                        SelectUser(selectedUser);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    IsDropdownOpen = false;
                    e.Handled = true;
                    break;

                case Key.Up:
                    if (UsersListBox.SelectedIndex == 0)
                    {
                        SearchBox.Focus();
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void DebounceTimer_Tick(object sender, EventArgs e)
        {
            _debounceTimer.Stop();
            SearchUsersAsync();
        }

        private void Users_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateUserCount();
            UpdateListState();
        }

        #endregion

        #region Private Methods

        private void UpdateSelectedUserDisplay()
        {
            if (SelectedUser != null)
            {
                // Show selected user
                UserNameText.Text = SelectedUser.FullName;
                UserInitials.Text = GetInitials(SelectedUser.FullName);
                SelectedUserDisplay.Visibility = Visibility.Visible;
                PlaceholderText.Visibility = Visibility.Collapsed;
                ActionIcon.Text = "✕";
            }
            else
            {
                // Show placeholder
                SelectedUserDisplay.Visibility = Visibility.Collapsed;
                PlaceholderText.Visibility = IsDropdownOpen ? Visibility.Collapsed : Visibility.Visible;
                ActionIcon.Text = IsDropdownOpen ? "▲" : "▼";
            }
        }

        private void UpdatePlaceholder()
        {
            PlaceholderText.Text = Placeholder ?? "Chọn người dùng...";
        }

        private void UpdateReadOnlyState()
        {
            SearchBox.IsEnabled = !IsReadOnly;
            ActionButton.IsEnabled = !IsReadOnly;
        }

        private void UpdateLoadingState()
        {
            LoadingIndicator.Visibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
            UsersScrollViewer.Visibility = IsLoading ? Visibility.Collapsed : Visibility.Visible;

            UpdateListState();
        }

        private void UpdateListState()
        {
            if (IsLoading)
            {
                EmptyState.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyState.Visibility = _users.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateUserCount()
        {
            UserCountText.Text = $"📊 {_users.Count} người dùng";
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "??";

            var words = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                return $"{words[0][0]}{words[words.Length - 1][0]}".ToUpper();
            }
            else if (words.Length == 1)
            {
                return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpper();
            }
            return "??";
        }

        private void StartSearch()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async void SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2)
            {
                await LoadInitialUsersAsync();
                return;
            }

            // Cancel previous search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            try
            {
                IsLoading = true;
                _users.Clear();

                var users = await SearchUsersByScope(SearchText);

                if (!_searchCts.Token.IsCancellationRequested)
                {
                    foreach (var user in users)
                    {
                        _users.Add(user);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, ignore
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching users: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadInitialUsersAsync()
        {
            try
            {
                IsLoading = true;
                _users.Clear();

                var users = await SearchUsersByScope("");

                foreach (var user in users)
                {
                    _users.Add(user);
                }

                System.Diagnostics.Debug.WriteLine($"[UserSelector] Loaded {_users.Count} users");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading initial users: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<List<UserModel>> SearchUsersByScope(string searchText)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSelector] SearchUsersByScope - ProjectId: {ProjectId}, SearchScope: {SearchScope}");

            if (SearchScope == "ProjectMembers" && ProjectId > 0)
            {
                var searchOptions = new UserSearchOptions
                {
                    FilterMode = UserFilterMode.ProjectMembersOnly,
                    ProjectId = ProjectId,
                };

                System.Diagnostics.Debug.WriteLine($"[UserSelector] Searching project members for ProjectId: {ProjectId}");
                return await _userService.SearchUsersAsync(searchText, searchOptions);
            }
            else
            {
                var searchOptions = new UserSearchOptions
                {
                    FilterMode = UserFilterMode.AllUsers,
                    
                };

                System.Diagnostics.Debug.WriteLine($"[UserSelector] Searching all users");
                return await _userService.SearchUsersAsync(searchText, searchOptions);
            }
        }

        private void SelectUser(UserModel user)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UserSelector] SelectUser called with: {user?.FullName ?? "NULL"}");

                SelectedUser = user;
                IsDropdownOpen = false;

                System.Diagnostics.Debug.WriteLine($"[UserSelector] SelectUser completed - SelectedUser: {SelectedUser?.FullName ?? "NULL"}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserSelector] Error in SelectUser: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
