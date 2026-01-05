using ManagementFile.AdminManagement.Commands;
using ManagementFile.AdminManagement.Services;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.UserManagement;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.AdminManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho Add/Edit User Dialog
    /// </summary>
    public class AddEditUserDialogViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private UserDto _originalUser;
        private bool _isEditMode;

        // Fields
        private string _username = "";
        private string _email = "";
        private string _fullName = "";
        private string _password = "";
        private string _confirmPassword = "";
        private UserRole _role = UserRole.Staff;
        private Department _department = Department.OTHER;
        private string _phoneNumber = "";
        private string _position = "";
        private int _managerId = 0;
        private string _language = "vi-VN";
        private bool _isLoading = false;
        private string _errorMessage = "";

        public AddEditUserDialogViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            // Initialize commands
            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        /// <summary>
        /// Initialize the ViewModel for Add/Edit mode
        /// </summary>
        /// <param name="userToEdit">User to edit, null for add mode</param>
        public async Task Initialize(UserDto userToEdit = null)
        {
            _originalUser = userToEdit;
            _isEditMode = userToEdit != null;

            UserRoles = UserRoleExtensions.GetUserRoleItems();
            Departments = DepartmentExtensions.GetDepartmentItems();
            Managers =  new ObservableCollection<UserDto>();

            // Reset all fields
            Username = "";
            Email = "";
            FullName = "";
            Password = "";
            ConfirmPassword = "";
            Role = UserRole.Staff;
            Department = Department.OTHER;
            PhoneNumber = "";
            Position = "";
            ManagerId = 0;
            Language = "vi-VN";
            ErrorMessage = "";

            // Load user data if editing
            if (_isEditMode)
            {
                LoadUserData(_originalUser);
            }

            // Notify property changes
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(DialogTitle));
            OnPropertyChanged(nameof(CanSave));
        }

        #region Properties

        public bool IsEditMode => _isEditMode;
        public bool IsCreateMode => !_isEditMode;

        public string DialogTitle => IsEditMode ? "Sửa thông tin User" : "Thêm User mới";

        [Required(ErrorMessage = "Username không được trống")]
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ValidateProperty(value);
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        [Required(ErrorMessage = "Email không được trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ValidateProperty(value);
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        [Required(ErrorMessage = "Họ tên không được trống")]
        public string FullName
        {
            get => _fullName;
            set
            {
                if (SetProperty(ref _fullName, value))
                {
                    ValidateProperty(value);
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(PasswordsMatch));
                }
            }
        }

        public ObservableCollection<UserRoleItem> UserRoles 
        {
            get;
            set;
        }

        public UserRole Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        public ObservableCollection<DepartmentItem> Departments 
        {
            get;
            set;
        }

        public Department Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        public string Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public ObservableCollection<UserDto> Managers
        {
            get;
            set;
        }

        public int ManagerId
        {
            get => _managerId;
            set => SetProperty(ref _managerId, value);
        }

        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool PasswordsMatch => Password == ConfirmPassword;

        public bool CanSave
        {
            get
            {
                if (IsLoading) return false;
                if (string.IsNullOrWhiteSpace(Username)) return false;
                if (string.IsNullOrWhiteSpace(FullName)) return false;
                
                if (IsCreateMode)
                {
                    if (string.IsNullOrWhiteSpace(Password)) return false;
                    if (!PasswordsMatch) return false;
                }

                return true;
            }
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Command Implementations

        private async Task ExecuteSaveAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = "";

                if (IsCreateMode)
                {
                    await CreateUserAsync();
                }
                else
                {
                    await UpdateUserAsync();
                }

                // Close dialog with success result
                CloseDialog(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteSave() => CanSave;

        private void ExecuteCancel()
        {
            CloseDialog(false);
        }

        #endregion

        #region Helper Methods

        private void LoadUserData(UserDto user)
        {
            Username = user.Username;
            Email = user.Email;
            FullName = user.FullName;
            Role = user.Role;
            Department = user.Department;
            PhoneNumber = user.PhoneNumber ?? "";
            Position = user.Position ?? "";
            ManagerId = user.ManagerId;
            Language = user.Language ?? "vi-VN";
        }

        private async Task CreateUserAsync()
        {
            var request = new CreateUserRequest
            {
                Username = Username,
                Email = Email,
                FullName = FullName,
                Password = Password,
                Role = Role,
                Department = Department,
                PhoneNumber = PhoneNumber,
                Position = Position,
                ManagerId = ManagerId,
                Language = Language
            };

            var result = await _apiService.CreateUserAsync(request);
            if (result == null)
            {
                throw new Exception("Không thể tạo user. Vui lòng kiểm tra lại thông tin.");
            }
        }

        private async Task UpdateUserAsync()
        {
            var request = new UpdateUserRequest
            {
                Email = Email,
                FullName = FullName,
                Role = Role,
                Department = Department,
                PhoneNumber = PhoneNumber,
                Position = Position,
                ManagerId = ManagerId,
                Language = Language
            };

            var result = await _apiService.UpdateUserAsync(_originalUser.Id, request);
            if (result == null)
            {
                throw new Exception("Không thể cập nhật user. Vui lòng kiểm tra lại thông tin.");
            }
        }

        private void ValidateProperty(object value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            // Basic validation - can be enhanced with more complex validation
            ErrorMessage = "";
        }

        #endregion

        #region Events

        public event EventHandler<bool> CloseRequested;

        private void CloseDialog(bool result)
        {
            CloseRequested?.Invoke(this, result);
        }

        #endregion
    }
}