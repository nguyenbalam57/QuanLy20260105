using ManagementFile.API.Services;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Background service để thực hiện các tác vụ định kỳ cho file management
    /// </summary>
    public class FileManagementBackgroundService : BackgroundService
    {
        private readonly ILogger<FileManagementBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Chạy mỗi giờ

        public FileManagementBackgroundService(
            ILogger<FileManagementBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Management Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupTasks();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during background cleanup tasks");
                }

                // Chờ đến lần cleanup tiếp theo
                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("File Management Background Service stopped");
        }

        private async Task PerformCleanupTasks()
        {
            using var scope = _serviceProvider.CreateScope();
            
            var fileService = scope.ServiceProvider.GetRequiredService<IProjectFileService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IFilePermissionService>();
            var shareService = scope.ServiceProvider.GetRequiredService<IFileShareService>();

            _logger.LogInformation("Starting background cleanup tasks...");

            try
            {
                // 1. Cleanup overdue checkouts
                _logger.LogDebug("Cleaning up overdue file checkouts...");
                await fileService.CleanupOverdueCheckoutsAsync();

                // 2. Cleanup expired permissions
                _logger.LogDebug("Cleaning up expired file permissions...");
                await permissionService.CleanupExpiredPermissionsAsync();

                // 3. Cleanup expired shares
                _logger.LogDebug("Cleaning up expired file shares...");
                await shareService.CleanupExpiredSharesAsync();

                // 4. Additional cleanup tasks can be added here
                // - Delete orphaned physical files
                // - Generate missing thumbnails
                // - Archive old file versions
                // - Send notification for pending approvals

                _logger.LogInformation("Background cleanup tasks completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background cleanup tasks");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("File Management Background Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}