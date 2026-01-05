using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.LogInOut
{
    public class ChangePasswordViewModel
    {
        private readonly UserManagementService _userService;
        private readonly EventBus _eventBus;
        private readonly DataCache _dataCache;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private string _currentPassword;
        private string _newPassword;
        private string _confirmPassword;
        private string _errorMessage;
        private bool _isProcessing;

        public ChangePasswordViewModel(UserManagementService userService, EventBus eventBus, DataCache dataCache, MainWindowViewModel mainWindowViewModel)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            ChangePasswordCommand = new AsyncRelayCommand(
                ExecuteChangePassword,
                CanExecuteChangePassword);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value;
                OnPropertyChanged();
                ((AsyncRelayCommand)ChangePasswordCommand).RaiseCanExecuteChanged();
            }
        }

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                OnPropertyChanged();
                ((AsyncRelayCommand)ChangePasswordCommand).RaiseCanExecuteChanged();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
                ((AsyncRelayCommand)ChangePasswordCommand).RaiseCanExecuteChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
            }
        }

        public ICommand ChangePasswordCommand { get; }
        public ICommand CancelCommand { get; }

        // Event để đóng window
        public event Action<bool> CloseRequested;

        private bool CanExecuteChangePassword(object parameter)
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   !IsProcessing;
        }

        private async Task ExecuteChangePassword(object parameter)
        {
            ErrorMessage = string.Empty;

            // Validation
            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu mới không khớp";
                return;
            }

            if (NewPassword.Length < 6)
            {
                ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự";
                return;
            }

            if (CurrentPassword == NewPassword)
            {
                ErrorMessage = "Mật khẩu mới phải khác mật khẩu hiện tại";
                return;
            }

            IsProcessing = true;

            try
            {
                var success = await _userService.ChangePasswordAsync(
            CurrentPassword,
            NewPassword,
            ConfirmPassword);

                if (success)
                {
                    // Đóng ChangePasswordView
                    CloseRequested?.Invoke(true);

                    // Hiển thị notification với countdown
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        for (int i = 3; i >= 1; i--)
                        {
                            _eventBus.PublishNotification("Đổi mật khẩu thành công",
                                $"Đăng xuất sau {i} giây...", "Info");
                            await Task.Delay(1000);
                        }

                        // Clear session và đăng xuất
                        _userService.Logout();
                        _dataCache.Clear();

                        _eventBus.PublishNotification("Đăng xuất",
                            "Vui lòng đăng nhập lại với mật khẩu mới", "Info");

                        // Show login window
                        _mainWindowViewModel.LogoutCommand.Execute(null);
                    });
                }
                else
                {
                    ErrorMessage = "Mật khẩu hiện tại không đúng hoặc có lỗi xảy ra";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteCancel(object parameter)
        {
            CloseRequested?.Invoke(false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
