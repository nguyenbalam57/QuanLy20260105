using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Quản lý tất cả services trong application và điều phối hoạt động giữa các phase
    /// Central service coordinator cho ManagementFile Enterprise Platform
    /// </summary>
    public sealed class ServiceManager
    {
        #region DI Dependencies

        private readonly AdminService _adminService;
        private readonly UserManagementService _userManagementService;
        private readonly ProjectApiService _projectApiService;
        private readonly IServiceProvider _serviceProvider;

        public ServiceManager(
            AdminService adminService,
            UserManagementService userManagementService,
            ProjectApiService projectApiService,
            IServiceProvider serviceProvider) 
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Khởi tạo các collections
            _services = new Dictionary<Type, object>();
            _initializedServices = new HashSet<Type>();
        }
        #endregion

        #region Private Fields
        private readonly Dictionary<Type, object> _services;
        private readonly HashSet<Type> _initializedServices;
        private bool _isInitialized = false;
        #endregion

        #region Service Registration & Management

        /// <summary>
        /// Đăng ký service vào ServiceManager từ DI container
        /// </summary>
        /// <typeparam name="T">Loại service</typeparam>
        public void RegisterServiceFromDI<T>() where T : class
        {
            var serviceType = typeof(T);

            if (_services.ContainsKey(serviceType))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Service {serviceType.Name} đã được đăng ký rồi.");
                return;
            }

            try
            {
                var serviceInstance = _serviceProvider.GetService<T>();
                if (serviceInstance != null)
                {
                    _services[serviceType] = serviceInstance;
                    System.Diagnostics.Debug.WriteLine($"✅ Đăng ký service từ DI: {serviceType.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Không tìm thấy service trong DI: {serviceType.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi đăng ký service {serviceType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy service từ DI container
        /// </summary>
        /// <typeparam name="T">Loại service cần lấy</typeparam>
        /// <returns>Instance của service</returns>
        public T GetService<T>() where T : class
        {
            var serviceType = typeof(T);

            // First try from registered services
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service as T;
            }

            // Fallback to DI container
            try
            {
                var serviceFromDI = _serviceProvider.GetService<T>();
                if (serviceFromDI != null)
                {
                    _services[serviceType] = serviceFromDI;
                    return serviceFromDI;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi lấy service từ DI {serviceType.Name}: {ex.Message}");
            }

            throw new InvalidOperationException($"Service {serviceType.Name} chưa được đăng ký hoặc không có trong DI container.");
        }

        /// <summary>
        /// Kiểm tra xem service có được đăng ký không
        /// </summary>
        /// <typeparam name="T">Loại service</typeparam>
        /// <returns>True nếu service đã được đăng ký</returns>
        public bool IsServiceRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        #endregion

        #region Service Initialization

        /// <summary>
        /// Khởi tạo tất cả services đã đăng ký từ các phases
        /// </summary>
        public async Task InitializeAllServicesAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 Bắt đầu khởi tạo tất cả services...");

                // Initialize services from DI container
                await InitializeServicesFromDIAsync();

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("✅ Hoàn thành khởi tạo tất cả services!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khởi tạo services: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize services from DI container
        /// </summary>
        private async Task InitializeServicesFromDIAsync()
        {
            try
            {
                // Phase 1 Services
                RegisterServiceFromDI<AdminService>();
                System.Diagnostics.Debug.WriteLine("📊 Đã khởi tạo AdminService");
                RegisterServiceFromDI<UserManagementService>();
                System.Diagnostics.Debug.WriteLine("👥 Đã khởi tạo UserManagementService");
                // Phase 2 Services
                RegisterServiceFromDI<ProjectApiService>();
                System.Diagnostics.Debug.WriteLine("📋 Đã khởi tạo ProjectApiService");

                // Phase 3 Services
                RegisterServiceFromDI<ClientNavigationService>();
                System.Diagnostics.Debug.WriteLine("👤 Đã khởi tạo ClientService");

                // Phase 4 Services
                RegisterServiceFromDI<ReportService>();
                System.Diagnostics.Debug.WriteLine("📊 Đã khởi tạo ReportService");

                // Phase 5 Services
                RegisterServiceFromDI<AdvancedSearchService>();
                RegisterServiceFromDI<KeyboardShortcutService>();
                RegisterServiceFromDI<BulkOperationsService>();
                RegisterServiceFromDI<ConfigurationService>();
                RegisterServiceFromDI<MonitoringService>();
                RegisterServiceFromDI<SecurityService>();
                RegisterServiceFromDI<OptimizationService>();
                System.Diagnostics.Debug.WriteLine("⚡ Đã khởi tạo OptimizationService");

                // Integration Services
                RegisterServiceFromDI<NavigationService>();
                System.Diagnostics.Debug.WriteLine("🧭 Đã khởi tạo NavigationService...");
                RegisterServiceFromDI<DataCache>();
                System.Diagnostics.Debug.WriteLine("💾 Đã khởi tạo DataCache...");
                RegisterServiceFromDI<EventBus>();
                System.Diagnostics.Debug.WriteLine("📡 Đã khởi tạo EventBus...");

                await Task.Delay(100); // Allow services to stabilize

                System.Diagnostics.Debug.WriteLine($"📊 Đã khởi tạo {_services.Count} services từ DI container");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khởi tạo services từ DI: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Service Health & Monitoring

        /// <summary>
        /// Kiểm tra sức khỏe của tất cả services
        /// </summary>
        public ServiceHealthReport GetServicesHealthReport()
        {
            var report = new ServiceHealthReport
            {
                TotalServices = _services.Count,
                HealthyServices = 0,
                UnhealthyServices = 0,
                ServiceStatus = new List<ServiceStatus>()
            };

            foreach (var service in _services)
            {
                try
                {
                    var status = new ServiceStatus
                    {
                        ServiceName = service.Key.Name,
                        IsHealthy = service.Value != null,
                        LastChecked = DateTime.Now
                    };

                    if (status.IsHealthy)
                    {
                        report.HealthyServices++;
                    }
                    else
                    {
                        report.UnhealthyServices++;
                    }

                    report.ServiceStatus.Add(status);
                }
                catch (Exception ex)
                {
                    report.ServiceStatus.Add(new ServiceStatus
                    {
                        ServiceName = service.Key.Name,
                        IsHealthy = false,
                        LastChecked = DateTime.Now,
                        ErrorMessage = ex.Message
                    });
                    report.UnhealthyServices++;
                }
            }

            return report;
        }

        /// <summary>
        /// Lấy danh sách tất cả services đã đăng ký
        /// </summary>
        public List<string> GetRegisteredServiceNames()
        {
            var serviceNames = new List<string>();
            foreach (var service in _services)
            {
                serviceNames.Add(service.Key.Name);
            }
            return serviceNames;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Dọn dẹp tất cả services khi application đóng
        /// </summary>
        public void Cleanup()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧹 Bắt đầu dọn dẹp services...");

                foreach (var service in _services.Values)
                {
                    // Chỉ dispose non-singleton services hoặc services tự dispose
                    if (service is IDisposable disposableService && !IsSingletonService(service))
                    {
                        try
                        {
                            disposableService.Dispose();
                            System.Diagnostics.Debug.WriteLine($"🗑️ Disposed service: {service.GetType().Name}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Error disposing {service.GetType().Name}: {ex.Message}");
                        }
                    }
                }

                _services.Clear();
                _initializedServices.Clear();
                _isInitialized = false;

                System.Diagnostics.Debug.WriteLine("✅ Hoàn thành dọn dẹp services!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi dọn dẹp services: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if service is a singleton (should not be disposed by ServiceManager)
        /// </summary>
        private bool IsSingletonService(object service)
        {
            var serviceType = service.GetType();

            // List of singleton services that should not be disposed here
            var singletonTypes = new[]
            {
            typeof(AdminService),
            typeof(ProjectApiService),
            typeof(ClientNavigationService),
            typeof(UserManagementService)
        };

            return singletonTypes.Contains(serviceType);
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Báo cáo sức khỏe của services
    /// </summary>
    public class ServiceHealthReport
    {
        public int TotalServices { get; set; }
        public int HealthyServices { get; set; }
        public int UnhealthyServices { get; set; }
        public List<ServiceStatus> ServiceStatus { get; set; } = new List<ServiceStatus>();
        
        public bool IsSystemHealthy => UnhealthyServices == 0;
        public double HealthPercentage => TotalServices > 0 ? (double)HealthyServices / TotalServices * 100 : 0;
    }

    /// <summary>
    /// Trạng thái của một service cụ thể
    /// </summary>
    public class ServiceStatus
    {
        public string ServiceName { get; set; } = "";
        public bool IsHealthy { get; set; }
        public DateTime LastChecked { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    #endregion
}