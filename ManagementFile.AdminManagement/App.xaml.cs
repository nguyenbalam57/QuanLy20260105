using ManagementFile.AdminManagement.Services;
using ManagementFile.AdminManagement.ViewModels;
using ManagementFile.AdminManagement.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace ManagementFile.AdminManagement
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Configure Dependency Injection
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // Create and show login window using DI
                var loginWindow = _serviceProvider.GetRequiredService<LoginView>();
                loginWindow.Show();

                // Set as main window
                MainWindow = loginWindow;

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động ứng dụng: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register ApiService as Singleton
            services.AddSingleton<ApiService>();

            // Register ViewModels as Transient (new instance each time)
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<AddEditUserDialogViewModel>();
            services.AddTransient<ChangePasswordDialogViewModel>();

            // Register Views as Transient
            services.AddTransient<LoginView>();
            services.AddTransient<MainWindow>();
            services.AddTransient<UserManagementView>();
            services.AddTransient<AddEditUserDialog>();
            services.AddTransient<ChangePasswordDialog>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Cleanup resources
                var apiService = _serviceProvider?.GetService<ApiService>();
                apiService?.Dispose();

                // Dispose service provider if it implements IDisposable
                (_serviceProvider as IDisposable)?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            base.OnExit(e);
        }

        /// <summary>
        /// Get service from DI container
        /// </summary>
        public static T GetService<T>() where T : class
        {
            return _serviceProvider?.GetService<T>();
        }

        /// <summary>
        /// Get required service from DI container
        /// </summary>
        public static T GetRequiredService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider chưa được khởi tạo");

            return _serviceProvider.GetRequiredService<T>();
        }
    }
}
