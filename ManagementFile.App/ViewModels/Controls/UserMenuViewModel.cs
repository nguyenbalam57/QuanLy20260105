using ManagementFile.App.Services;
using ManagementFile.App.Views.LogInOut;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Controls
{
    public class UserMenuViewModel : BaseViewModel
    {
        private readonly ServiceManager _serviceManager;
        private readonly NavigationService _navigationService;
        private readonly DataCache _dataCache;
        private readonly EventBus _eventBus;
        private readonly UserManagementService _userService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        public ICommand ChangePasswordCommand { get; }
        public ICommand LogoutCommand { get; }


        public UserMenuViewModel(
            ServiceManager serviceManager,
            NavigationService navigationService,
            DataCache dataCache,
            EventBus eventBus,
            UserManagementService userManagementService,
            MainWindowViewModel mainWindowViewModel)
        {
            // Khởi tạo services
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _userService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        public string CurrentUserName => _userService.CurrentUser?.Username ?? "User";
        public string FullName => _userService.CurrentUser?.FullName ?? "Name";

        private void ExecuteChangePassword()
        {
            try
            {
                // Đóng popup trước khi mở dialog
                ClosePopupRequested?.Invoke();

                // Mở dialog đổi mật khẩu
                var changePasswordWindow = App.GetRequiredService<ChangePasswordView>();
                changePasswordWindow.Owner = Application.Current.MainWindow;
                changePasswordWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _eventBus.PublishNotification("Lỗi", $"Không thể mở cửa sổ đổi mật khẩu: {ex.Message}", "Error");
            }
        }

        private void ExecuteLogout()
        {
            try
            {
                // Confirm logout
                var result = System.Windows.MessageBox.Show(
                    "Bạn có chắc chắn muốn đăng xuất?",
                    "Xác nhận đăng xuất",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _mainWindowViewModel.LogoutCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                _eventBus.PublishNotification("Lỗi", $"Lỗi khi đăng xuất: {ex.Message}", "Error");
                // Khôi phục ShutdownMode nếu có lỗi
                System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
        }

        // Event để thông báo đóng popup
        public event Action ClosePopupRequested;
    }
}
