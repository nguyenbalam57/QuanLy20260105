using Microsoft.Extensions.Options;
using System.IO;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Configuration model cho BaseDirectories
    /// </summary>
    public class BaseDirectoriesOptions
    {
        public string BaseStoragePath { get; set; } = "";
        public string ProjectsPath { get; set; } = "Projects";
        public string UsersPath { get; set; } = "Users";
        public string TempPath { get; set; } = "Temp";
        public string BackupPath { get; set; } = "Backups";
        public string LogsPath { get; set; } = "Logs";
        public bool AutoCreateDirectories { get; set; } = true;
        public bool ValidatePhysicalPaths { get; set; } = true;
        public int CleanupTempFilesIntervalHours { get; set; } = 24;
        public int DefaultTempFileRetentionDays { get; set; } = 7;
        public bool EnableStorageAnalytics { get; set; } = true;
    }

    /// <summary>
    /// BaseDirectoryService cho API - quản lý tập trung các đường dẫn storage
    /// </summary>
    public interface IBaseDirectoryService
    {
        string BaseStoragePath { get; }
        string ProjectsBasePath { get; }
        string UsersBasePath { get; }
        string TempPath { get; }
        string BackupPath { get; }
        string LogsPath { get; }
        
        string GetProjectPath(string projectId);
        string GetProjectFolderPhysicalPath(string projectId, string folderPath);
        string GetProjectFilePhysicalPath(string projectId, string folderPath, string fileName);
        bool DoesProjectFolderExist(string projectId, string folderPath);
        DirectoryInfo GetProjectFolderInfo(string projectId, string folderPath);
        long GetTotalSizeInMB();
        Task<StorageAnalyticsModel> GetStorageAnalyticsAsync();
        void CleanupTempFiles(int olderThanDays = 7);
        bool UpdateBaseStoragePath(string newPath);
    }

    /// <summary>
    /// Storage analytics model
    /// </summary>
    public class StorageAnalyticsModel
    {
        public long TotalSizeMB { get; set; }
        public int TotalFiles { get; set; }
        public int TotalFolders { get; set; }
        public Dictionary<string, long> SizeByCategory { get; set; } = new();
        public Dictionary<string, int> FileCountByType { get; set; } = new();
        public DateTime LastAnalyzed { get; set; }
    }

    /// <summary>
    /// Implementation của IBaseDirectoryService
    /// </summary>
    public class BaseDirectoryService : IBaseDirectoryService
    {
        private readonly BaseDirectoriesOptions _options;
        private readonly ILogger<BaseDirectoryService> _logger;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);

        public BaseDirectoryService(IOptions<BaseDirectoriesOptions> options, ILogger<BaseDirectoryService> logger)
        {
            _options = options.Value;
            _logger = logger;
            
            InitializeDirectoriesAsync().GetAwaiter().GetResult();
        }

        #region Properties

        public string BaseStoragePath => _options.BaseStoragePath;

        public string ProjectsBasePath => Path.Combine(_options.BaseStoragePath, _options.ProjectsPath);

        public string UsersBasePath => Path.Combine(_options.BaseStoragePath, _options.UsersPath);

        public string TempPath => Path.Combine(_options.BaseStoragePath, _options.TempPath);

        public string BackupPath => Path.Combine(_options.BaseStoragePath, _options.BackupPath);

        public string LogsPath => Path.Combine(_options.BaseStoragePath, _options.LogsPath);

        #endregion

        #region Public Methods

        /// <summary>
        /// Lấy đường dẫn cho project cụ thể
        /// </summary>
        public string GetProjectPath(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentException("Project ID cannot be null or empty", nameof(projectId));

            var projectPath = Path.Combine(ProjectsBasePath, projectId);
            
            if (_options.AutoCreateDirectories)
                CreateDirectoryIfNotExists(projectPath);
                
            return projectPath;
        }

        /// <summary>
        /// Lấy đường dẫn vật lý đầy đủ cho ProjectFolder
        /// Kết hợp base directory với FolderPath của ProjectFolder
        /// 
        /// Ví dụ:
        /// - Base: C:\ManagementFiles\Projects\TCVF013
        /// - FolderPath: 01_TRAINNING\03_WorkProduct\02_ITST\BAITAP_ITST\02_WorkProduct\4_Environment\01_MINHTAM
        /// - Result: C:\ManagementFiles\Projects\TCVF013\01_TRAINNING\03_WorkProduct\02_ITST\BAITAP_ITST\02_WorkProduct\4_Environment\01_MINHTAM
        /// </summary>
        public string GetProjectFolderPhysicalPath(string projectId, string folderPath)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentException("Project ID cannot be null or empty", nameof(projectId));

            var projectBasePath = GetProjectPath(projectId);
            
            if (string.IsNullOrEmpty(folderPath))
                return projectBasePath;

            // Chuẩn hóa folderPath - thay thế "/" thành "\" cho Windows
            var normalizedPath = NormalizePath(folderPath);
            
            // Kết hợp base path với folder path
            var fullPhysicalPath = Path.Combine(projectBasePath, normalizedPath);
            
            if (_options.AutoCreateDirectories)
                CreateDirectoryIfNotExists(fullPhysicalPath);
            
            _logger.LogDebug("Generated physical path: {PhysicalPath} for Project: {ProjectId}, FolderPath: {FolderPath}", 
                fullPhysicalPath, projectId, folderPath);
            
            return fullPhysicalPath;
        }

        /// <summary>
        /// Lấy đường dẫn vật lý cho file trong ProjectFolder
        /// </summary>
        public string GetProjectFilePhysicalPath(string projectId, string folderPath, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            var folderPhysicalPath = GetProjectFolderPhysicalPath(projectId, folderPath);
            return Path.Combine(folderPhysicalPath, fileName);
        }

        /// <summary>
        /// Kiểm tra đường dẫn vật lý có tồn tại không
        /// </summary>
        public bool DoesProjectFolderExist(string projectId, string folderPath)
        {
            try
            {
                var physicalPath = GetProjectFolderPhysicalPath(projectId, folderPath);
                return Directory.Exists(physicalPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking directory existence for Project: {ProjectId}, FolderPath: {FolderPath}", 
                    projectId, folderPath);
                return false;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết về thư mục vật lý
        /// </summary>
        public DirectoryInfo GetProjectFolderInfo(string projectId, string folderPath)
        {
            var physicalPath = GetProjectFolderPhysicalPath(projectId, folderPath);
            return new DirectoryInfo(physicalPath);
        }

        /// <summary>
        /// Tính tổng dung lượng sử dụng (MB)
        /// </summary>
        public long GetTotalSizeInMB()
        {
            try
            {
                var dirInfo = new DirectoryInfo(BaseStoragePath);
                if (!dirInfo.Exists) return 0;
                
                return CalculateDirectorySize(dirInfo) / (1024 * 1024);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total storage size");
                return 0;
            }
        }

        /// <summary>
        /// Lấy storage analytics chi tiết
        /// </summary>
        public async Task<StorageAnalyticsModel> GetStorageAnalyticsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var analytics = new StorageAnalyticsModel
                    {
                        LastAnalyzed = DateTime.UtcNow
                    };

                    var baseDir = new DirectoryInfo(BaseStoragePath);
                    if (!baseDir.Exists)
                    {
                        return analytics;
                    }

                    // Analyze each category
                    var categories = new[]
                    {
                        ("Projects", ProjectsBasePath),
                        ("Users", UsersBasePath),
                        ("Temp", TempPath),
                        ("Backups", BackupPath),
                        ("Logs", LogsPath)
                    };

                    foreach (var (name, path) in categories)
                    {
                        var dirInfo = new DirectoryInfo(path);
                        if (dirInfo.Exists)
                        {
                            var size = CalculateDirectorySize(dirInfo) / (1024 * 1024);
                            analytics.SizeByCategory[name] = size;
                        }
                    }

                    // Count files and get file types
                    CountFilesRecursively(baseDir, analytics);

                    analytics.TotalSizeMB = analytics.SizeByCategory.Values.Sum();

                    return analytics;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating storage analytics");
                    return new StorageAnalyticsModel { LastAnalyzed = DateTime.UtcNow };
                }
            });
        }

        /// <summary>
        /// Dọn dẹp temporary files cũ
        /// </summary>
        public void CleanupTempFiles(int olderThanDays = 7)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-olderThanDays);
                var tempDir = new DirectoryInfo(TempPath);
                
                if (!tempDir.Exists) return;
                
                var deletedCount = 0;
                var deletedSize = 0L;
                
                foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (file.CreationTime < cutoffDate || file.LastWriteTime < cutoffDate)
                        {
                            deletedSize += file.Length;
                            file.Delete();
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {FilePath}", file.FullName);
                    }
                }
                
                _logger.LogInformation("Cleanup completed. Deleted {Count} temp files ({SizeMB} MB) older than {Days} days", 
                    deletedCount, deletedSize / (1024 * 1024), olderThanDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during temp files cleanup");
            }
        }

        /// <summary>
        /// Cập nhật base storage path (chỉ dành cho Admin)
        /// </summary>
        public bool UpdateBaseStoragePath(string newPath)
        {
            try
            {
                if (string.IsNullOrEmpty(newPath))
                    return false;

                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }

                _options.BaseStoragePath = newPath;
                InitializeDirectoriesAsync().GetAwaiter().GetResult();
                
                _logger.LogInformation("Base storage path updated to: {NewPath}", newPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update base storage path to: {NewPath}", newPath);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Khởi tạo các thư mục cơ bản async
        /// </summary>
        private async Task InitializeDirectoriesAsync()
        {
            await _initializationLock.WaitAsync();
            try
            {
                if (!_options.AutoCreateDirectories) return;

                await Task.Run(() =>
                {
                    var directories = new[]
                    {
                        BaseStoragePath,
                        ProjectsBasePath,
                        UsersBasePath,
                        TempPath,
                        BackupPath,
                        LogsPath
                    };

                    foreach (var directory in directories)
                    {
                        CreateDirectoryIfNotExists(directory);
                    }
                });

                _logger.LogInformation("Base directories initialized successfully. BasePath: {BasePath}", BaseStoragePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize base directories");
                throw;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        /// <summary>
        /// Tạo thư mục nếu chưa tồn tại
        /// </summary>
        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogDebug("Created directory: {Path}", path);
            }
        }

        /// <summary>
        /// Chuẩn hóa đường dẫn
        /// </summary>
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            // Thay thế separators và loại bỏ separators ở đầu/cuối
            var normalized = path.Replace('/', Path.DirectorySeparatorChar);
            normalized = normalized.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            return normalized;
        }

        /// <summary>
        /// Tính dung lượng thư mục đệ quy
        /// </summary>
        private long CalculateDirectorySize(DirectoryInfo directory)
        {
            long size = 0;

            try
            {
                // Cộng dung lượng files
                foreach (var file in directory.GetFiles())
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch { /* Ignore file access errors */ }
                }

                // Đệ quy cho subdirectories
                foreach (var subDir in directory.GetDirectories())
                {
                    try
                    {
                        size += CalculateDirectorySize(subDir);
                    }
                    catch { /* Ignore directory access errors */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating size for directory: {DirectoryPath}", directory.FullName);
            }

            return size;
        }

        /// <summary>
        /// Đếm files và phân tích file types
        /// </summary>
        private void CountFilesRecursively(DirectoryInfo directory, StorageAnalyticsModel analytics)
        {
            try
            {
                foreach (var file in directory.GetFiles())
                {
                    analytics.TotalFiles++;
                    
                    var extension = file.Extension.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(extension))
                    {
                        analytics.FileCountByType[extension] = analytics.FileCountByType.GetValueOrDefault(extension, 0) + 1;
                    }
                }

                foreach (var subDir in directory.GetDirectories())
                {
                    analytics.TotalFolders++;
                    CountFilesRecursively(subDir, analytics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error counting files in directory: {DirectoryPath}", directory.FullName);
            }
        }

        #endregion
    }
}