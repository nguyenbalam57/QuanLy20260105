using ManagementFile.AdminManagement.Commands;
using ManagementFile.AdminManagement.Services;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Requests.UserManagement;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagementFile.AdminManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho Change Password Dialog
    /// </summary>
    public class ChangePasswordDialogViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private UserDto _user;

        #region Fields
        private string _currentPassword = "";
        private string _newPassword = "";
        private string _confirmPassword = "";
        private bool _isLoading = false;
        private string _errorMessage = "";
        #endregion

        public ChangePasswordDialogViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            // Initialize commands
            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        /// <summary>
        /// Initialize the ViewModel for the specified user
        /// </summary>
        /// <param name="user">User to change password for</param>
        public void Initialize(UserDto user)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));

            // Reset all fields
            CurrentPassword = "";
            NewPassword = "";
            ConfirmPassword = "";
            ErrorMessage = "";

            // Notify property changes
            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(CanSave));
        }

        #region Properties

        public string UserDisplayName => _user?.FullName ?? "Unknown User";

        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                if (SetProperty(ref _currentPassword, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                if (SetProperty(ref _newPassword, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(PasswordsMatch));
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

        public bool PasswordsMatch => NewPassword == ConfirmPassword;

        public bool CanSave
        {
            get
            {
                if (IsLoading) return false;
                if (string.IsNullOrWhiteSpace(CurrentPassword)) return false;
                if (string.IsNullOrWhiteSpace(NewPassword)) return false;
                if (!PasswordsMatch) return false;
                if (NewPassword.Length < 6) return false;
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

                var request = new ChangePasswordRequest
                {
                    CurrentPassword = CurrentPassword,
                    NewPassword = NewPassword
                };

                var success = await _apiService.ChangePasswordAsync(_user.Id, request);
                
                if (success)
                {
                    CloseDialog(true);
                }
                else
                {
                    ErrorMessage = "Không thể đổi mật khẩu. Vui lòng kiểm tra lại mật khẩu hiện tại.";
                }
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

        #region Events

        public event EventHandler<bool> CloseRequested;

        private void CloseDialog(bool result)
        {
            CloseRequested?.Invoke(this, result);
        }

        #endregion
    }
}