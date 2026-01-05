using ManagementFile.AdminManagement.Services;
using ManagementFile.AdminManagement.ViewModels;
using ManagementFile.AdminManagement.Views;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ManagementFile.AdminManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly MainWindowViewModel _viewModel;
        private readonly DispatcherTimer _timer;

        public MainWindow(ApiService apiService, MainWindowViewModel viewModel)
        {
            InitializeComponent();
            
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Setup timer for real-time updates
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Handle window closing
            Closing += OnMainWindowClosing;

            // Load initial data
            Loaded += OnMainWindowLoaded;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _viewModel.UpdateCurrentTime();
        }

        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadInitialDataAsync();

            // Inject UserManagementView with DI
            InjectUserManagementView();
        }

        private void InjectUserManagementView()
        {
            try
            {
                // Find the UserManagementView placeholder in the UserManagement TabItem
                var tabControl = FindName("MainTabControl") as TabControl;
                if (tabControl != null)
                {
                    // Find the UserManagement TabItem (index 1)
                    if (tabControl.Items.Count > 1 && tabControl.Items[1] is TabItem userTabItem)
                    {
                        // Create UserManagementView using DI
                        var userManagementView = App.GetRequiredService<UserManagementView>();
                        userTabItem.Content = userManagementView;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to creating view without DI if needed
                System.Diagnostics.Debug.WriteLine($"Error injecting UserManagementView: {ex.Message}");
            }
        }

        private async void OnMainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer?.Stop();
            
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn thoát khỏi Admin Management?",
                "Xác nhận thoát",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }

            // Logout from API
            try
            {
                await _apiService.LogoutAsync();
            }
            catch
            {
                // Ignore logout errors when closing
            }

            Application.Current.Shutdown();
        }
    }
}