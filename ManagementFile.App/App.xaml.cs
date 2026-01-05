
using ManagementFile.App.Controls;
using ManagementFile.App.Controls.Projects;
using ManagementFile.App.DragablzUser;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.Services.TimeTracking;
using ManagementFile.App.ViewModels;
using ManagementFile.App.ViewModels.Advanced;
using ManagementFile.App.ViewModels.Controls;
using ManagementFile.App.ViewModels.Controls.Projects;
using ManagementFile.App.ViewModels.Dashboard;
using ManagementFile.App.ViewModels.LogInOut;
using ManagementFile.App.ViewModels.Project;
using ManagementFile.App.ViewModels.Reports;
using ManagementFile.App.ViewModels.Search;
using ManagementFile.App.ViewModels.TimeLogs;
using ManagementFile.App.Views;
using ManagementFile.App.Views.Advanced;
using ManagementFile.App.Views.Dashboard;
using ManagementFile.App.Views.LogInOut;
using ManagementFile.App.Views.Project;
using ManagementFile.App.Views.Reports;
using ManagementFile.App.Views.Search;
using ManagementFile.Contracts.DTOs.UserManagement;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ManagementFile.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IServiceProvider _serviceProvider;
        static App()
        {
            // Add assembly resolve handler to fix Newtonsoft.Json loading issues
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        /// <summary>
        /// Handle assembly resolve events - specifically for Newtonsoft.Json
        /// </summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                // Handle Newtonsoft.Json version conflicts
                if (args.Name.StartsWith("Newtonsoft.Json"))
                {
                    System.Diagnostics.Debug.WriteLine($"Resolving assembly: {args.Name}");

                    // Try to load from application directory first
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    var dllPath = Path.Combine(appDir, "Newtonsoft.Json.dll");

                    if (File.Exists(dllPath))
                    {
                        try
                        {
                            var assembly = Assembly.LoadFrom(dllPath);
                            System.Diagnostics.Debug.WriteLine($"✅ Resolved Newtonsoft.Json from: {dllPath}, Version: {assembly.GetName().Version}");
                            return assembly;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Failed to load from app dir: {ex.Message}");
                        }
                    }

                    // Try packages directory as fallback
                    var baseDir = Directory.GetParent(appDir)?.FullName;
                    if (baseDir != null)
                    {
                        var packageDirs = new[]
                        {
                            Path.Combine(baseDir, "packages", "Newtonsoft.Json.13.0.3", "lib", "net45", "Newtonsoft.Json.dll"),
                            Path.Combine(baseDir, "packages", "Newtonsoft.Json.13.0.3", "lib", "net40", "Newtonsoft.Json.dll"),
                            Path.Combine(baseDir, "packages", "Newtonsoft.Json.13.0.3", "lib", "net35", "Newtonsoft.Json.dll"),
                            Path.Combine(appDir, "..", "packages", "Newtonsoft.Json.13.0.3", "lib", "net45", "Newtonsoft.Json.dll")
                        };

                        foreach (var packagePath in packageDirs)
                        {
                            if (File.Exists(packagePath))
                            {
                                try
                                {
                                    var assembly = Assembly.LoadFrom(packagePath);
                                    var version = assembly.GetName().Version;
                                    System.Diagnostics.Debug.WriteLine($"✅ Resolved Newtonsoft.Json from packages: {packagePath}, Version: {version}");

                                    // Copy to app directory for future use
                                    try
                                    {
                                        File.Copy(packagePath, dllPath, true);
                                        System.Diagnostics.Debug.WriteLine($"📋 Copied DLL to app directory: {dllPath}");
                                    }
                                    catch
                                    {
                                        // Ignore copy errors
                                    }

                                    return assembly;
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"❌ Failed to load from {packagePath}: {ex.Message}");
                                }
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"❌ Could not resolve Newtonsoft.Json from any location");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 Assembly resolve error for {args.Name}: {ex.Message}");
            }

            return null;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                try
                {
                    var ex = ev.ExceptionObject as Exception;
                    File.AppendAllText(@"C:\Temp\Helper_Unhandled.log", $"{DateTime.UtcNow:u} Unhandled: {ex}\n\n");
                }
                catch { }
            };

            this.DispatcherUnhandledException += (s, ev) =>
            {
                try
                {
                    File.AppendAllText(@"C:\Temp\Helper_Dispatcher.log", $"{DateTime.UtcNow:u} Dispatcher: {ev.Exception}\n\n");
                }
                catch { }
                // Ev.Handled = true; // chỉ set true nếu muốn ngăn app đóng (tạm)
            };

            base.OnStartup(e);

            // Cấu hình Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Tạo và hiển thị MainWindow với DI
            var login = _serviceProvider.GetRequiredService<LoginView>();
            login.Show();

            // Log loaded assemblies for debugging
            System.Diagnostics.Debug.WriteLine("=== Application Startup - Assembly Loading ===");
            AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
            {
                if (args.LoadedAssembly.GetName().Name.Contains("Newtonsoft"))
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded: {args.LoadedAssembly.FullName}");
                    System.Diagnostics.Debug.WriteLine($"Location: {args.LoadedAssembly.Location}");
                }
            };
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Đăng ký ApiConfiguration trước
            services.AddSingleton<ApiConfiguration>();

            // Cấu hình HttpClient
            services.AddHttpClient<ApiService>("ApiClient", (serviceProvider, client) =>
            {
                // Lấy config từ DI container
                var config = serviceProvider.GetRequiredService<ApiConfiguration>();
                var baseUrl = config.BaseUrl;

                if (!baseUrl.EndsWith("/"))
                    baseUrl += "/";

                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("User-Agent", "ManagementFile/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = config.Timeout;

                System.Diagnostics.Debug.WriteLine($"🔧 HttpClient configured with: {baseUrl}");
            })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                        UseProxy = false,
                        MaxConnectionsPerServer = 10
                    })
            .AddPolicyHandler(GetRetryPolicy()); // Thêm retry policy

            // Đăng ký Services
            services.AddSingleton<ApiService>();
            services.AddSingleton<AdminService>();
            services.AddSingleton<AdvancedSearchService>();
            services.AddSingleton<BulkOperationsService>();

            // Use factory method for ClientNavigationService singleton
            services.AddSingleton<ClientNavigationService>(provider => ClientNavigationService.Instance);


            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<DataCache>();
            services.AddSingleton<EventBus>();
            services.AddSingleton<KeyboardShortcutService>();
            services.AddSingleton<MonitoringService>();
            services.AddSingleton<NavigationService>();
            services.AddSingleton<OptimizationService>();

            services.AddSingleton<IControlFactory, ControlFactory>();

            services.AddSingleton<ProjectApiService>();
            services.AddSingleton<ReportService>();
            services.AddSingleton<SecurityService>();
            services.AddSingleton<UserManagementService>();
            services.AddSingleton<TaskCommentService>();

            // Register ServiceManager with proper constructor injection
            services.AddSingleton<ServiceManager>();

            services.AddSingleton<TimeTrackingApiService>();

            RegisterViewModels(services);

            RegisterViews(services);

            // Có thể thêm các services khác
            // services.AddTransient<IFileService, FileService>();
            // services.AddTransient<INotificationService, NotificationService>();
        }

        private void RegisterViewModels(IServiceCollection services)
        {
            // Đăng ký ViewModels như Transient (tạo mới mỗi lần cần)
            services.AddTransient<AdvancedSearchViewModel>();
            services.AddTransient<AdvancedSettingsViewModel>();
            services.AddTransient<ProductionReadinessViewModel>();
            services.AddTransient<SmartDashboardViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<AddEditProjectDialogViewModel>();
            services.AddTransient<AddEditTaskDialogViewModel>();
            services.AddTransient<ReportsMainViewModel>();
            services.AddTransient<GlobalSearchViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TaskCommentDetailViewModel>();
            services.AddTransient<AddEditProjectMemberViewModel>();


            // Project
            services.AddSingleton<ProjectsControlViewModel>();
            services.AddTransient<ProjectTasksControlViewModel>();
            services.AddTransient<TaskCommentsControlViewModel>();
            services.AddTransient<ProjectMembersControlViewModel>();

            services.AddSingleton<ProjectManagentsDragablzViewViewModel>();

            services.AddTransient<UserMenuViewModel>();
            services.AddTransient<ChangePasswordViewModel>();

            services.AddTransient<WeeklyTimesheetViewModel>();
        }

        private void RegisterViews(IServiceCollection services)
        {
            // Đăng ký Windows như Transient
            services.AddTransient<AdvancedSearchView>();
            services.AddTransient<AdvancedSettingsView>();
            services.AddTransient<ProductionReadinessView>();
            services.AddTransient<SmartDashboardView>();
            services.AddTransient<LoginView>();
            services.AddTransient<AddEditProjectDialog>();
            services.AddTransient<AddEditTaskDialog>();
            services.AddTransient<ReportsMainView>();
            services.AddTransient<GlobalSearchView>();
            services.AddTransient<MainWindow>();
            services.AddTransient<TaskCommentDetailDialog>();
            services.AddTransient<AddEditProjectMemberDialog>();

            // Project
            services.AddTransient<ProjectManagentsDragablzView>();
            
            services.AddSingleton<ProjectsControl>();
            //services.AddTransient<ProjectTasksControl>();
            //services.AddTransient<TaskCommentsControl>();
            services.AddTransient<TabWindowNew>();

            services.AddTransient<UserMenuPopup>();
            services.AddTransient<ChangePasswordView>();
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .Or<TaskCanceledException>()
        .Or<IOException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var endpoint = context.ContainsKey("endpoint") ? context["endpoint"] : "unknown";
                System.Diagnostics.Debug.WriteLine($"🔄 Retry {retryCount} for {endpoint} after {timespan.TotalSeconds}s");
            });
        }
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Clean up ServiceManager first
                var serviceManager = _serviceProvider?.GetService<ServiceManager>();
                serviceManager?.Cleanup();

                System.Diagnostics.Debug.WriteLine("✅ Application shutdown completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error during application shutdown: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }

        public static UserDto GetCurrentUser()
        {
            var userService = GetRequiredService<UserManagementService>();
            return userService.CurrentUser;
        }

        public static UserModel GetCurrentUserModel()
        {             
            var userService = GetRequiredService<UserManagementService>();
            return userService.CurrentUserModel;
        }

        public static string GetVersionApp()
        {
            var version = "1.0.0";
            return version != null ? version.ToString() : "Unknown Version";
        }

        /// <summary>
        /// Lấy service từ DI container
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
                return null;

            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Lấy required service từ DI container
        /// </summary>
        public static T GetRequiredService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider chưa được khởi tạo");

            return _serviceProvider.GetRequiredService<T>();
        }

    }
}
