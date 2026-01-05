using ManagementFile.App.Controls;
using ManagementFile.App.Models;
using ManagementFile.App.Models.Users;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ManagementFile.App.Views.Dialogs.Users
{
    /// <summary>
    /// UserDialog - Simple dialog for selecting single or multiple users
    /// Usage:
    ///   // Single selection - returns userId
    ///   var userId = UserDialog.ShowSingle("Select User", "Choose a user:", projectId);
    ///   
    ///   // Multi selection - returns List of userIds
    ///   var userIds = UserDialog.ShowMultiple("Select Users", "Choose users:", projectId);
    ///   
    ///   // Get full UserModel objects
    ///   var user = UserDialog.ShowSingleUser("Select User", "Choose:", projectId);
    ///   var users = UserDialog.ShowMultipleUsers("Select Users", "Choose:", projectId);
    /// </summary>
    public partial class UserDialog : Window, INotifyPropertyChanged
    {
        #region Fields

        private bool _isSingleSelection = true;
        private UserModel _selectedUser;
        private ObservableCollection<UserModel> _selectedUsers;
        private UserModel _initialSelectedUser;
        private List<UserModel> _initialSelectedUsers;

        #endregion

        #region Properties

        public bool IsSingleSelection
        {
            get => _isSingleSelection;
            private set
            {
                if (_isSingleSelection != value)
                {
                    _isSingleSelection = value;
                    OnPropertyChanged(nameof(IsSingleSelection));
                }
            }
        }

        public UserModel SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged(nameof(SelectedUser));
                    UpdateSelectedUserDisplay();
                    UpdateConfirmButton();
                }
            }
        }

        public ObservableCollection<UserModel> SelectedUsers
        {
            get => _selectedUsers;
            set
            {
                _selectedUsers = value;
                OnPropertyChanged(nameof(SelectedUsers));
                UpdateConfirmButton();
            }
        }

        public int ProjectId { get; set; }
        public string SearchScope { get; set; } = "ProjectMembers";
        public bool IsConfirmed { get; private set; }

        #endregion

        #region Constructor

        private UserDialog()
        {
            InitializeComponent();

            DataContext = this;

            Loaded += UserDialog_Loaded;

            System.Diagnostics.Debug.WriteLine("[UserDialog] Constructor initialized");
        }

        #endregion

        #region Static Show Methods - Returns User IDs

        /// <summary>
        /// Show dialog for single user selection
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Message to display</param>
        /// <param name="projectId">Project ID for filtering (0 for all users)</param>
        /// <param name="searchScope">"ProjectMembers" or "AllUsers"</param>
        /// <param name="owner">Owner window</param>
        /// <returns>Selected user ID or null if cancelled</returns>
        public static int? ShowSingle(
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn một người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            UserModel selectedUser = null,
            Window owner = null)
        {
            var dialog = new UserDialog
            {
                IsSingleSelection = true,
                ProjectId = projectId,
                SearchScope = searchScope,
                SelectedUser = selectedUser ?? new UserModel(),
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.MessageTextBlock.Text = message;
            dialog.Title = title;

            System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowSingle called - ProjectId: {projectId}, SearchScope: {searchScope}");

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed && dialog.SelectedUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowSingle result: {dialog.SelectedUser.Id} ({dialog.SelectedUser.FullName})");
                return dialog.SelectedUser.Id;
            }

            System.Diagnostics.Debug.WriteLine("[UserDialog] ShowSingle cancelled or no selection");
            return null;
        }

        /// <summary>
        /// Show dialog for multiple user selection
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Message to display</param>
        /// <param name="projectId">Project ID for filtering (0 for all users)</param>
        /// <param name="searchScope">"ProjectMembers" or "AllUsers"</param>
        /// <param name="owner">Owner window</param>
        /// <returns>List of selected user IDs or empty list if cancelled</returns>
        public static List<int> ShowMultiple(
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            List<UserModel> selectedUsers = null,
            Window owner = null)
        {
            var dialog = new UserDialog
            {
                IsSingleSelection = false,
                ProjectId = projectId,
                SearchScope = searchScope,
                SelectedUsers = new ObservableCollection<UserModel>(selectedUsers ?? new List<UserModel>()),
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.MessageTextBlock.Text = message;
            dialog.Title = title;

            System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowMultiple called - ProjectId: {projectId}, SearchScope: {searchScope}");

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed && dialog.SelectedUsers != null)
            {
                var userIds = dialog.SelectedUsers.Select(u => u.Id).ToList();
                System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowMultiple result: {userIds.Count} users selected");
                return userIds;
            }

            System.Diagnostics.Debug.WriteLine("[UserDialog] ShowMultiple cancelled or no selection");
            return new List<int>();
        }

        #endregion

        #region Static Show Methods - Returns UserModel Objects

        /// <summary>
        /// Show dialog and return selected UserModel (single selection)
        /// </summary>
        public static UserModel ShowSingleUser(
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn một người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            UserModel selectedUser = null,
            Window owner = null)
        {
            var dialog = new UserDialog
            {
                IsSingleSelection = true,
                ProjectId = projectId,
                SearchScope = searchScope,
                SelectedUser = selectedUser ?? new UserModel(),
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.MessageTextBlock.Text = message;
            dialog.Title = title;

            System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowSingleUser called - ProjectId: {projectId}");

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed)
            {
                System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowSingleUser result: {dialog.SelectedUser?.FullName ?? "NULL"}");
                return dialog.SelectedUser;
            }

            System.Diagnostics.Debug.WriteLine("[UserDialog] ShowSingleUser cancelled");
            return null;
        }

        /// <summary>
        /// Show dialog and return selected UserModels (multiple selection)
        /// </summary>
        public static List<UserModel> ShowMultipleUsers(
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            List<UserModel> selectedUsers = null,
            Window owner = null)
        {
            var dialog = new UserDialog
            {
                IsSingleSelection = false,
                ProjectId = projectId,
                SearchScope = searchScope,
                SelectedUsers = new ObservableCollection<UserModel>(selectedUsers ?? new List<UserModel>()),
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.MessageTextBlock.Text = message;
            dialog.Title = title;

            System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowMultipleUsers called - ProjectId: {projectId}");

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed && dialog.SelectedUsers != null)
            {
                var users = dialog.SelectedUsers.ToList();
                System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowMultipleUsers result: {users.Count} users");
                return users;
            }

            System.Diagnostics.Debug.WriteLine("[UserDialog] ShowMultipleUsers cancelled");
            return new List<UserModel>();
        }

        #endregion

        #region Static Show Methods - With Default Values

        /// <summary>
        /// Show dialog with pre-selected user (for editing)
        /// </summary>
        public static int? ShowSingleWithDefault(
            UserModel defaultUser,
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn một người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            Window owner = null)
        {
            var dialog = new UserDialog
            {
                IsSingleSelection = true,
                ProjectId = projectId,
                SearchScope = searchScope,
                _initialSelectedUser = defaultUser
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.MessageTextBlock.Text = message;
            dialog.Title = title;

            System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowSingleWithDefault called - Default: {defaultUser?.FullName ?? "NULL"}");

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed && dialog.SelectedUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowSingleWithDefault result: {dialog.SelectedUser.Id}");
                return dialog.SelectedUser.Id;
            }

            System.Diagnostics.Debug.WriteLine("[UserDialog] ShowSingleWithDefault cancelled");
            return null;
        }

        /// <summary>
        /// Show dialog with pre-selected users (for editing)
        /// </summary>
        public static List<int> ShowMultipleWithDefaults(
            List<UserModel> defaultUsers,
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            Window owner = null)
        {
            var dialog = new UserDialog
            {
                IsSingleSelection = false,
                ProjectId = projectId,
                SearchScope = searchScope,
                _initialSelectedUsers = defaultUsers
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.MessageTextBlock.Text = message;
            dialog.Title = title;

            System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowMultipleWithDefaults called - Default count: {defaultUsers?.Count ?? 0}");

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed && dialog.SelectedUsers != null)
            {
                var userIds = dialog.SelectedUsers.Select(u => u.Id).ToList();
                System.Diagnostics.Debug.WriteLine($"[UserDialog] ShowMultipleWithDefaults result: {userIds.Count} users");
                return userIds;
            }

            System.Diagnostics.Debug.WriteLine("[UserDialog] ShowMultipleWithDefaults cancelled");
            return new List<int>();
        }

        /// <summary>
        /// Show dialog with pre-selected user - returns UserModel
        /// </summary>
        public static UserModel ShowSingleUserWithDefault(
            UserModel defaultUser,
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn một người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            Window owner = null)
        {
            var dialog = new UserDialog
            {
                IsSingleSelection = true,
                ProjectId = projectId,
                SearchScope = searchScope,
                _initialSelectedUser = defaultUser
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.MessageTextBlock.Text = message;
            dialog.Title = title;

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed)
            {
                return dialog.SelectedUser;
            }

            return null;
        }

        /// <summary>
        /// Show dialog with pre-selected users - returns List of UserModels
        /// </summary>
        public static List<UserModel> ShowMultipleUsersWithDefaults(
            List<UserModel> defaultUsers,
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            Window owner = null)
        {
            var dialog = new UserDialog
            {
                IsSingleSelection = false,
                ProjectId = projectId,
                SearchScope = searchScope,
                _initialSelectedUsers = defaultUsers
            };

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            dialog.TitleTextBlock.Text = title;
            dialog.MessageTextBlock.Text = message;
            dialog.Title = title;

            var result = dialog.ShowDialog();

            if (result == true && dialog.IsConfirmed && dialog.SelectedUsers != null)
            {
                return dialog.SelectedUsers.ToList();
            }

            return new List<UserModel>();
        }

        #endregion

        #region Async Methods

        /// <summary>
        /// Async version - Show dialog for single user selection
        /// </summary>
        public static Task<int?> ShowSingleAsync(
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn một người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            UserModel selectedUser = null,
            Window owner = null)
        {
            return Task.FromResult(ShowSingle(title, message, projectId, searchScope, selectedUser, owner));
        }

        /// <summary>
        /// Async version - Show dialog for multiple user selection
        /// </summary>
        public static Task<List<int>> ShowMultipleAsync(
            string title = "Chọn Người Dùng",
            string message = "Vui lòng chọn người dùng từ danh sách",
            int projectId = 0,
            string searchScope = "ProjectMembers",
            List<UserModel> selectedUsers = null,
            Window owner = null)
        {
            return Task.FromResult(ShowMultiple(title, message, projectId, searchScope, selectedUsers, owner));
        }

        #endregion

        #region Event Handlers

        private void UserDialog_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[UserDialog] Dialog loaded - Mode: {(IsSingleSelection ? "Single" : "Multi")}, ProjectId: {ProjectId}, SearchScope: {SearchScope}");

            // Setup mode
            if (IsSingleSelection)
            {
                SingleSelectorPanel.Visibility = Visibility.Visible;
                MultiSelectorPanel.Visibility = Visibility.Collapsed;

                HelpText.Text = "• Nhập tên hoặc email để tìm kiếm\n" +
                                "• Click hoặc nhấn Enter để chọn\n" +
                                "• F5 để làm mới danh sách";
            }
            else
            {
                SingleSelectorPanel.Visibility = Visibility.Collapsed;
                MultiSelectorPanel.Visibility = Visibility.Visible;

                HelpText.Text = "• Sử dụng @ để mention người dùng\n" +
                                "• Double-click hoặc Enter để thêm\n" +
                                "• Click nút ✕ để xóa người đã chọn\n" +
                                "• F5 để làm mới danh sách";
            }

            UpdateConfirmButton();
            System.Diagnostics.Debug.WriteLine("[UserDialog] Dialog loaded successfully");
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (IsSingleSelection && SelectedUser == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn một người dùng.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!IsSingleSelection && (SelectedUsers == null || SelectedUsers.Count == 0))
            {
                MessageBox.Show(
                    "Vui lòng chọn ít nhất một người dùng.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsConfirmed = true;
            DialogResult = true;

            if (IsSingleSelection)
            {
                System.Diagnostics.Debug.WriteLine($"[UserDialog] Confirmed - Selected user: {SelectedUser.FullName} (ID: {SelectedUser.Id})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[UserDialog] Confirmed - Selected {SelectedUsers.Count} users");
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;

            System.Diagnostics.Debug.WriteLine("[UserDialog] Cancelled by user");

            Close();
        }

        private void Selector_Loaded(object sender, RoutedEventArgs e)
        {
            var reviewerSelector = sender as UserSelectorControl;
            if (reviewerSelector != null)
            {
                // Subscribe to property changes manually
                var dpd = DependencyPropertyDescriptor.FromProperty(UserSelectorControl.SelectedUserProperty, typeof(UserSelectorControl));
                dpd.AddValueChanged(reviewerSelector, Selector_SelectedUserChanged);
            }
        }

        private void Selector_SelectedUserChanged(object sender, EventArgs e)
        {
            var reviewerSelector = sender as UserSelectorControl;
            var selectedUser = reviewerSelector?.SelectedUser;

            System.Diagnostics.Debug.WriteLine($"[Dialog] Manual event - Selected user: {selectedUser?.FullName ?? "NULL"}");

            // Manually update ViewModel if binding isn't working

            SelectedUser = selectedUser;

        }

        #endregion

        #region Private Methods

        private void UpdateSelectedUserDisplay()
        {
            if (SelectedUser != null)
            {

                ResultSummaryText.Text = $"✓ Đã chọn: {SelectedUser.FullName}";
                ResultSummaryText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
            else
            {

                ResultSummaryText.Text = "Chưa có người dùng được chọn";
                ResultSummaryText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D"));
            }
        }

        private void UpdateConfirmButton()
        {
            bool canConfirm = IsSingleSelection
                ? SelectedUser != null
                : (SelectedUsers != null && SelectedUsers.Count > 0);

            ConfirmButton.IsEnabled = canConfirm;

            System.Diagnostics.Debug.WriteLine($"[UserDialog] Confirm button enabled: {canConfirm}");
        }

        

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}