using ManagementFile.AdminManagement.Commands;
using ManagementFile.AdminManagement.Services;
using ManagementFile.AdminManagement.Views;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.UserManagement;
using ManagementFile.Contracts.Responses.UserManagement;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.AdminManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho User Management
    /// </summary>
    public class UserManagementViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        
        #region Fields
        private ObservableCollection<UserDto> _users;
        private UserDto _selectedUser;
        private string _searchTerm = "";
        private UserRole? _selectedRole = UserRole.All;
        private Department? _selectedDepartment = Department.All;
        private bool? _isActiveFilter = true;
        private bool _isLoading = false;
        private int _totalCount = 0;
        private int _pageNumber = 1;
        private int _pageSize = 20;
        private int _totalPages = 0;
        private string _statusMessage = "";

        private ObservableCollection<UserRoleItem> _roles;
        private ObservableCollection<DepartmentItem> _departments;

        #endregion

        public UserManagementViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            
            Users = new ObservableCollection<UserDto>();
            
            // Initialize commands
            LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
            SearchCommand = new AsyncRelayCommand(SearchUsersAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            AddUserCommand = new AsyncRelayCommand(AddUserAsync);
            EditUserCommand = new AsyncRelayCommand<UserDto>(EditUserAsync);
            DeleteUserCommand = new AsyncRelayCommand<UserDto>(DeleteUserAsync);
            UnlockUserCommand = new AsyncRelayCommand<UserDto>(UnlockUserAsync);
            ChangePasswordCommand = new AsyncRelayCommand<UserDto>(ChangePasswordAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CanGoPreviousPage);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, CanGoNextPage);
            ResetPasswordCommand = new AsyncRelayCommand<UserDto>(ResetPasswordAsync);

            Roles = UserRoleExtensions.GetAllUserRoleItems();
            Departments = DepartmentExtensions.GetAllDepartmentItems();

            // Load initial data
            _ = LoadUsersAsync();
        }

        #region Properties

        public ObservableCollection<UserDto> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public UserDto SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        public ObservableCollection<UserRoleItem> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
        }

        public UserRole? SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public ObservableCollection<DepartmentItem> Departments
        {
            get => _departments;
            set => SetProperty(ref _departments, value);
        }

        public Department? SelectedDepartment
        {
            get => _selectedDepartment;
            set => SetProperty(ref _selectedDepartment, value);
        }

        public bool? IsActiveFilter
        {
            get => _isActiveFilter;
            set => SetProperty(ref _isActiveFilter, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int PageNumber
        {
            get => _pageNumber;
            set => SetProperty(ref _pageNumber, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string FilterInfo => 
            $"Hiển thị {Users?.Count ?? 0} / {TotalCount} users | Trang {PageNumber} / {TotalPages}";

        #endregion

        #region Commands

        public ICommand LoadUsersCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand UnlockUserCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand ResetPasswordCommand { get; }

        #endregion

        #region Command Implementations

        private async Task LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang tải danh sách users...";

                var searchRequest = new UserSearchRequest
                {
                    SearchTerm = SearchTerm,
                    Role = SelectedRole != UserRole.All ? SelectedRole : null,
                    Department = SelectedDepartment != Department.All ? SelectedDepartment : null,
                    IsActive = IsActiveFilter,
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    SortBy = "FullName",
                    SortDirection = "asc"
                };

                var response = await _apiService.GetUsersAsync(searchRequest);
                
                if (response != null)
                {
                    Users.Clear();
                    foreach (var user in response.Users)
                    {
                        Users.Add(user);
                    }

                    TotalCount = response.TotalCount;
                    TotalPages = response.TotalPages;
                    
                    StatusMessage = $"Đã tải {Users.Count} users";
                    OnPropertyChanged(nameof(FilterInfo));
                }
                else
                {
                    StatusMessage = "Không thể tải danh sách users";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi tải danh sách users: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchUsersAsync()
        {
            PageNumber = 1; // Reset to first page when searching
            await LoadUsersAsync();
        }

        private void ClearFilters()
        {
            SearchTerm = "";
            SelectedRole = null;
            SelectedDepartment = null;
            IsActiveFilter = null;
            PageNumber = 1;
            _ = LoadUsersAsync();
        }

        private async Task AddUserAsync()
        {
            try
            {
                // Create AddEditUserDialogViewModel using DI
                var dialogViewModel = App.GetRequiredService<AddEditUserDialogViewModel>();
                dialogViewModel.Initialize(null); // null means add mode
                
                var dialog = App.GetRequiredService<AddEditUserDialog>();
                dialog.DataContext = dialogViewModel;

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                    StatusMessage = "Đã thêm user mới thành công";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm user: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EditUserAsync(UserDto user)
        {
            if (user == null) return;

            try
            {
                // Create AddEditUserDialogViewModel using DI
                var dialogViewModel = App.GetRequiredService<AddEditUserDialogViewModel>();
                dialogViewModel.Initialize(user); // pass user for edit mode
                
                var dialog = App.GetRequiredService<AddEditUserDialog>();
                dialog.DataContext = dialogViewModel;

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                    StatusMessage = $"Đã cập nhật thông tin user {user.FullName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi sửa user: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteUserAsync(UserDto user)
        {
            if (user == null) return;

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa user '{user.FullName}'?\n\nThao tác này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusMessage = $"Đang xóa user {user.FullName}...";
                    var success = await _apiService.DeleteUserAsync(user.Id);
                    
                    if (success)
                    {
                        await RefreshAsync();
                        StatusMessage = $"Đã xóa user {user.FullName} thành công";
                    }
                    else
                    {
                        StatusMessage = "Không thể xóa user";
                        MessageBox.Show("Không thể xóa user. Vui lòng thử lại.", "Lỗi", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi khi xóa user: {ex.Message}";
                    MessageBox.Show($"Lỗi khi xóa user: {ex.Message}", "Lỗi", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task UnlockUserAsync(UserDto user)
        {
            if (user == null) return;

            try
            {
                StatusMessage = $"Đang mở khóa tài khoản {user.Username}...";
                var success = await _apiService.UnlockUserAsync(user.Id);
                
                if (success)
                {
                    await RefreshAsync();
                    StatusMessage = $"Đã mở khóa tài khoản {user.Username} thành công";
                }
                else
                {
                    StatusMessage = "Không thể mở khóa tài khoản";
                    MessageBox.Show("Không thể mở khóa tài khoản. Vui lòng thử lại.", "Lỗi", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi mở khóa tài khoản: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ChangePasswordAsync(UserDto user)
        {
            if (user == null) return;

            try
            {
                // Create ChangePasswordDialogViewModel using DI
                var dialogViewModel = App.GetRequiredService<ChangePasswordDialogViewModel>();
                dialogViewModel.Initialize(user);
                
                var dialog = App.GetRequiredService<ChangePasswordDialog>();
                dialog.DataContext = dialogViewModel;

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = $"Đã đổi mật khẩu cho user {user.Username} thành công";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đổi mật khẩu: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ResetPasswordAsync(UserDto user)
        {
            if (user == null) return;
            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn đặt lại mật khẩu cho user '{user.FullName}'?",
                "Xác nhận đặt lại mật khẩu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var request = new ResetPasswordRequest
                    {
                        NewPassword = "TCV@000000"
                    };

                    StatusMessage = $"Đang đặt lại mật khẩu cho user {user.Username}...";
                    var success = await _apiService.ResetUserPasswordAsync(user.Id, request);
                    
                    if (success)
                    {
                        StatusMessage = $"Đã đặt lại mật khẩu cho user {user.Username} thành công";
                        MessageBox.Show($"Mật khẩu mới đã được gửi đến email của user {user.Username}.", 
                                        "Đặt lại mật khẩu thành công", 
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusMessage = "Không thể đặt lại mật khẩu";
                        MessageBox.Show("Không thể đặt lại mật khẩu. Vui lòng thử lại.", "Lỗi", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi: {ex.Message}";
                    MessageBox.Show($"Lỗi khi đặt lại mật khẩu: {ex.Message}", "Lỗi", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task RefreshAsync()
        {
            await LoadUsersAsync();
        }

        private async Task PreviousPageAsync()
        {
            if (PageNumber > 1)
            {
                PageNumber--;
                await LoadUsersAsync();
            }
        }

        private async Task NextPageAsync()
        {
            if (PageNumber < TotalPages)
            {
                PageNumber++;
                await LoadUsersAsync();
            }
        }

        private bool CanGoPreviousPage() => PageNumber > 1;
        private bool CanGoNextPage() => PageNumber < TotalPages;

        #endregion
    }
}