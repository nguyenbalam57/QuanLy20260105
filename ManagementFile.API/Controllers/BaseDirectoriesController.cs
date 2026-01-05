using ManagementFile.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// Controller quản lý Base Directories - chỉ dành cho Admin
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu authentication
    public class BaseDirectoriesController : ControllerBase
    {
        private readonly IBaseDirectoryService _baseDirectoryService;
        private readonly ILogger<BaseDirectoriesController> _logger;

        public BaseDirectoriesController(IBaseDirectoryService baseDirectoryService, ILogger<BaseDirectoriesController> logger)
        {
            _baseDirectoryService = baseDirectoryService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin cấu hình base directories hiện tại
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfiguration()
        {
            try
            {
                var config = new
                {
                    BaseStoragePath = _baseDirectoryService.BaseStoragePath,
                    ProjectsBasePath = _baseDirectoryService.ProjectsBasePath,
                    UsersBasePath = _baseDirectoryService.UsersBasePath,
                    TempPath = _baseDirectoryService.TempPath,
                    BackupPath = _baseDirectoryService.BackupPath,
                    LogsPath = _baseDirectoryService.LogsPath,
                    TotalSizeMB = _baseDirectoryService.GetTotalSizeInMB(),
                    CurrentTime = DateTime.UtcNow
                };

                return Ok(new { Success = true, Data = config });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting base directories configuration");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy đường dẫn vật lý cho ProjectFolder (kết hợp base + FolderPath)
        /// Ví dụ: GET /api/BaseDirectories/project/TCVF013/folder-path?folderPath=01_TRAINNING\03_WorkProduct
        /// </summary>
        [HttpGet("project/{projectId}/folder-path")]
        public IActionResult GetProjectFolderPhysicalPath(string projectId, [FromQuery] string folderPath = "")
        {
            try
            {
                var physicalPath = _baseDirectoryService.GetProjectFolderPhysicalPath(projectId, folderPath);
                var exists = _baseDirectoryService.DoesProjectFolderExist(projectId, folderPath);
                
                var result = new
                {
                    ProjectId = projectId,
                    FolderPath = folderPath,
                    PhysicalPath = physicalPath,
                    Exists = exists,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogDebug("Generated physical path for Project: {ProjectId}, FolderPath: {FolderPath} -> {PhysicalPath}", 
                    projectId, folderPath, physicalPath);

                return Ok(new { Success = true, Data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project folder physical path for Project: {ProjectId}, FolderPath: {FolderPath}", 
                    projectId, folderPath);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy đường dẫn vật lý cho file trong ProjectFolder
        /// </summary>
        [HttpGet("project/{projectId}/file-path")]
        public IActionResult GetProjectFilePhysicalPath(string projectId, [FromQuery] string folderPath = "", [FromQuery] string fileName = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return BadRequest(new { Success = false, Message = "File name is required" });
                }

                var physicalPath = _baseDirectoryService.GetProjectFilePhysicalPath(projectId, folderPath, fileName);
                var folderExists = _baseDirectoryService.DoesProjectFolderExist(projectId, folderPath);
                var fileExists = System.IO.File.Exists(physicalPath);
                
                var result = new
                {
                    ProjectId = projectId,
                    FolderPath = folderPath,
                    FileName = fileName,
                    PhysicalPath = physicalPath,
                    FolderExists = folderExists,
                    FileExists = fileExists,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project file physical path for Project: {ProjectId}, FolderPath: {FolderPath}, FileName: {FileName}", 
                    projectId, folderPath, fileName);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết về thư mục project
        /// </summary>
        [HttpGet("project/{projectId}/folder-info")]
        public IActionResult GetProjectFolderInfo(string projectId, [FromQuery] string folderPath = "")
        {
            try
            {
                var dirInfo = _baseDirectoryService.GetProjectFolderInfo(projectId, folderPath);
                
                var result = new
                {
                    ProjectId = projectId,
                    FolderPath = folderPath,
                    PhysicalPath = dirInfo.FullName,
                    Exists = dirInfo.Exists,
                    CreatedTime = dirInfo.Exists ? dirInfo.CreationTime : (DateTime?)null,
                    LastWriteTime = dirInfo.Exists ? dirInfo.LastWriteTime : (DateTime?)null,
                    FileCount = dirInfo.Exists ? dirInfo.GetFiles().Length : 0,
                    SubFolderCount = dirInfo.Exists ? dirInfo.GetDirectories().Length : 0,
                    SizeMB = dirInfo.Exists ? CalculateDirectorySizeMB(dirInfo) : 0,
                    AnalyzedAt = DateTime.UtcNow
                };

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project folder info for Project: {ProjectId}, FolderPath: {FolderPath}", 
                    projectId, folderPath);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy storage analytics chi tiết
        /// </summary>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetStorageAnalytics()
        {
            try
            {
                var analytics = await _baseDirectoryService.GetStorageAnalyticsAsync();
                return Ok(new { Success = true, Data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage analytics");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Cập nhật base storage path (chỉ Admin)
        /// </summary>
        [HttpPut("config/base-path")]
        [Authorize(Roles = "Admin")] // Chỉ Admin mới được update
        public IActionResult UpdateBaseStoragePath([FromBody] UpdateBasePathRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.NewPath))
                {
                    return BadRequest(new { Success = false, Message = "New path is required" });
                }

                var oldPath = _baseDirectoryService.BaseStoragePath;
                var success = _baseDirectoryService.UpdateBaseStoragePath(request.NewPath);
                
                if (success)
                {
                    _logger.LogInformation("Base storage path updated by admin from {OldPath} to {NewPath}", oldPath, request.NewPath);
                    return Ok(new { 
                        Success = true, 
                        Message = "Base storage path updated successfully",
                        Data = new { OldPath = oldPath, NewPath = request.NewPath }
                    });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Failed to update base storage path" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating base storage path to: {NewPath}", request?.NewPath);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Dọn dẹp temporary files (chỉ Admin/Manager)
        /// </summary>
        [HttpPost("cleanup/temp-files")]
        [Authorize(Roles = "Admin,Manager")] // Admin hoặc Manager có thể cleanup
        public IActionResult CleanupTempFiles([FromBody] CleanupTempFilesRequest request)
        {
            try
            {
                var olderThanDays = request?.OlderThanDays ?? 7;
                
                if (olderThanDays < 1 || olderThanDays > 365)
                {
                    return BadRequest(new { Success = false, Message = "OlderThanDays must be between 1 and 365" });
                }

                var tempPath = _baseDirectoryService.TempPath;
                var beforeSize = Directory.Exists(tempPath) ? CalculateDirectorySizeMB(new DirectoryInfo(tempPath)) : 0;

                _baseDirectoryService.CleanupTempFiles(olderThanDays);
                
                var afterSize = Directory.Exists(tempPath) ? CalculateDirectorySizeMB(new DirectoryInfo(tempPath)) : 0;
                var cleanedSizeMB = beforeSize - afterSize;

                _logger.LogInformation("Temp files cleanup completed by user. Cleaned {SizeMB} MB with {Days} days threshold", 
                    cleanedSizeMB, olderThanDays);

                return Ok(new { 
                    Success = true, 
                    Message = $"Temp files cleanup completed for files older than {olderThanDays} days",
                    Data = new 
                    { 
                        OlderThanDays = olderThanDays,
                        CleanedSizeMB = cleanedSizeMB,
                        BeforeSizeMB = beforeSize,
                        AfterSizeMB = afterSize,
                        CleanedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during temp files cleanup");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy storage statistics
        /// </summary>
        [HttpGet("statistics")]
        public IActionResult GetStorageStatistics()
        {
            try
            {
                var stats = new
                {
                    TotalSizeMB = _baseDirectoryService.GetTotalSizeInMB(),
                    BaseStoragePath = _baseDirectoryService.BaseStoragePath,
                    Paths = new
                    {
                        Projects = _baseDirectoryService.ProjectsBasePath,
                        Users = _baseDirectoryService.UsersBasePath,
                        Temp = _baseDirectoryService.TempPath,
                        Backup = _baseDirectoryService.BackupPath,
                        Logs = _baseDirectoryService.LogsPath
                    },
                    PathExistence = new
                    {
                        BaseExists = Directory.Exists(_baseDirectoryService.BaseStoragePath),
                        ProjectsExists = Directory.Exists(_baseDirectoryService.ProjectsBasePath),
                        UsersExists = Directory.Exists(_baseDirectoryService.UsersBasePath),
                        TempExists = Directory.Exists(_baseDirectoryService.TempPath),
                        BackupExists = Directory.Exists(_baseDirectoryService.BackupPath),
                        LogsExists = Directory.Exists(_baseDirectoryService.LogsPath)
                    },
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(new { Success = true, Data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage statistics");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Tính dung lượng thư mục (MB)
        /// </summary>
        private long CalculateDirectorySizeMB(DirectoryInfo directory)
        {
            try
            {
                if (!directory.Exists) return 0;

                long size = 0;
                foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
                {
                    try { size += file.Length; } catch { /* Ignore */ }
                }
                return size / (1024 * 1024);
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }

    #region Request Models

    /// <summary>
    /// Request model để update base path
    /// </summary>
    public class UpdateBasePathRequest
    {
        public string NewPath { get; set; } = "";
    }

    /// <summary>
    /// Request model để cleanup temp files
    /// </summary>
    public class CleanupTempFilesRequest
    {
        public int OlderThanDays { get; set; } = 7;
    }

    #endregion
}