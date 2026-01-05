using ManagementFile.App.Models;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.UserManagement;
using ManagementFile.Contracts.Responses.UserManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service tích hợp với AdminController API
    /// Quản lý dashboard statistics, audit logs, user management
    /// </summary>
    public class AdminService
    {
        private readonly ApiService _apiService;
        private static readonly object _lock = new object();

        public AdminService(
            ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        #region Dashboard APIs

        /// <summary>
        /// Lấy dashboard overview stats
        /// </summary>
        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync()
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync("/api/admin/dashboard");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DashboardOverviewDto>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy dashboard overview: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy user statistics
        /// </summary>
        public async Task<UserStatsDto> GetUserStatsAsync()
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync("/api/admin/stats/users");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserStatsDto>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy user stats: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy project statistics
        /// </summary>
        public async Task<ProjectStatsDto> GetProjectStatsAsync()
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync("/api/admin/stats/projects");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ProjectStatsDto>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy project stats: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy file statistics
        /// </summary>
        public async Task<FileStatsDto> GetFileStatsAsync()
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync("/api/admin/stats/files");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<FileStatsDto>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy file stats: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy task statistics
        /// </summary>
        public async Task<TaskStatsDto> GetTaskStatsAsync()
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync("/api/admin/stats/tasks");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TaskStatsDto>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy task stats: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy system health status
        /// </summary>
        public async Task<SystemHealthDto> GetSystemHealthAsync()
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync("/api/admin/system/health");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SystemHealthDto>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy system health: {ex.Message}", ex);
            }
        }

        #endregion

        #region User Management APIs

        /// <summary>
        /// Lấy danh sách users với pagination
        /// </summary>
        public async Task<PaginatedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 20, string search = null)
        {
            try
            {
                // Build proper query string for GET request
                var queryParams = new List<string>
                {
                    $"pageNumber={page}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrEmpty(search))
                {
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(search)}");
                }

                // Construct proper URL with query parameters
                var url = $"/api/users?{string.Join("&", queryParams)}";

                var response = await _apiService.GetFromEndpointAsync(url);

                // Check response status
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Session expired. Please login again.");
                    }

                    throw new HttpRequestException($"API Error {response.StatusCode}: {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();

                // Parse API response properly
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<UserListResponse>>(json);

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    return new PaginatedResult<UserDto>
                    {
                        Data = apiResponse.Data.Users,
                        TotalCount = apiResponse.Data.TotalCount,
                        PageNumber = apiResponse.Data.PageNumber,
                        PageSize = apiResponse.Data.PageSize,
                        TotalPages = apiResponse.Data.TotalPages,
                        HasNextPage = apiResponse.Data.HasNextPage,
                        HasPreviousPage = apiResponse.Data.HasPreviousPage
                    };
                }

                throw new Exception(apiResponse?.Message ?? "Invalid API response format");
            }
            catch (UnauthorizedAccessException)
            {
                // Re-throw để caller có thể handle login redirect
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy danh sách users: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tạo user mới
        /// </summary>
        public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var response = await _apiService.PostToEndpointAsync("/api/users", request);
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserDto>(responseJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tạo user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cập nhật user
        /// </summary>
        public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            try
            {
                var response = await _apiService.PutToEndpointAsync($"/api/users/{userId}", request);
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserDto>(responseJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi cập nhật user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Xóa user (soft delete)
        /// </summary>
        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var response = await _apiService.DeleteFromEndpointAsync($"/api/users/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi xóa user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reset password user
        /// </summary>
        public async Task<bool> ResetUserPasswordAsync(string userId)
        {
            try
            {
                var response = await _apiService.PostToEndpointAsync($"/api/users/{userId}/reset-password", "");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi reset password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Kích hoạt/vô hiệu hóa user
        /// </summary>
        public async Task<bool> ToggleUserStatusAsync(string userId, bool isActive)
        {
            try
            {
                var url = isActive ? $"/api/users/{userId}/activate" : $"/api/users/{userId}/deactivate";
                var response = await _apiService.PostToEndpointAsync(url, "");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi thay đổi trạng thái user: {ex.Message}", ex);
            }
        }

        #endregion

        #region Audit Logs APIs

        /// <summary>
        /// Lấy audit logs với pagination
        /// </summary>
        public async Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 50, string userId = null, string action = null)
        {
            try
            {
                var url = $"/api/admin/audit-logs?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrEmpty(userId))
                    url += $"&userId={Uri.EscapeDataString(userId)}";
                if (!string.IsNullOrEmpty(action))
                    url += $"&action={Uri.EscapeDataString(action)}";

                var response = await _apiService.GetFromEndpointAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PaginatedResult<AuditLogDto>>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy audit logs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy recent activities
        /// </summary>
        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int limit = 10)
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync($"/api/admin/recent-activities?limit={limit}");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<RecentActivityDto>>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy recent activities: {ex.Message}", ex);
            }
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Lấy active sessions
        /// </summary>
        public async Task<List<UserSessionDto>> GetActiveSessionsAsync()
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync("/api/admin/sessions");
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<UserSessionDto>>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy active sessions: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Terminate session của user
        /// </summary>
        public async Task<bool> TerminateSessionAsync(string sessionId)
        {
            try
            {
                var response = await _apiService.DeleteFromEndpointAsync($"/api/admin/sessions/{sessionId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi terminate session: {ex.Message}", ex);
            }
        }

        #endregion

        #region System Management

        /// <summary>
        /// Clear cache hệ thống
        /// </summary>
        public async Task<bool> ClearSystemCacheAsync()
        {
            try
            {
                var response = await _apiService.PostToEndpointAsync("/api/admin/system/clear-cache", "");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi clear cache: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Backup database
        /// </summary>
        public async Task<bool> BackupDatabaseAsync()
        {
            try
            {
                var response = await _apiService.PostToEndpointAsync("/api/admin/system/backup", "");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi backup database: {ex.Message}", ex);
            }
        }

        #endregion
    }

    #region DTOs for Admin API

    /// <summary>
    /// Dashboard overview statistics
    /// </summary>
    public class DashboardOverviewDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int TotalFiles { get; set; }
        public long TotalStorageMB { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// User statistics
    /// </summary>
    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; }
        public Dictionary<string, int> UsersByDepartment { get; set; }
        public List<TopActiveUser> TopActiveUsers { get; set; }
    }

    /// <summary>
    /// Project statistics
    /// </summary>
    public class ProjectStatsDto
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public Dictionary<string, int> ProjectsByStatus { get; set; }
        public List<TopProject> TopProjects { get; set; }
    }

    /// <summary>
    /// File statistics
    /// </summary>
    public class FileStatsDto
    {
        public int TotalFiles { get; set; }
        public long TotalSizeMB { get; set; }
        public int FilesUploadedThisMonth { get; set; }
        public Dictionary<string, int> FilesByType { get; set; }
        public List<TopFileProject> TopFileProjects { get; set; }
    }

    /// <summary>
    /// Task statistics
    /// </summary>
    public class TaskStatsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public Dictionary<string, int> TasksByStatus { get; set; }
        public Dictionary<string, int> TasksByPriority { get; set; }
    }

    /// <summary>
    /// System health information
    /// </summary>
    public class SystemHealthDto
    {
        public bool IsHealthy { get; set; }
        public string DatabaseStatus { get; set; }
        public string StorageStatus { get; set; }
        public string ApiStatus { get; set; }
        public int ActiveConnections { get; set; }
        public long MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public DateTime LastCheckTime { get; set; }
        public List<string> Issues { get; set; }
    }

    /// <summary>
    /// Recent activity entry
    /// </summary>
    public class RecentActivityDto
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string UserName { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Audit log entry
    /// </summary>
    public class AuditLogDto
    {
        public string Id { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    /// <summary>
    /// User session information
    /// </summary>
    public class UserSessionDto
    {
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Create user request
    /// </summary>
    public class CreateUserRequest
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }
        public Department Department { get; set; }
    }

    /// <summary>
    /// Update user request
    /// </summary>
    public class UpdateUserRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public Department Department { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Paginated result wrapper
    /// </summary>
    public class PaginatedResult<T>
    {
        public List<T> Data { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Top active user
    /// </summary>
    public class TopActiveUser
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int ActivityCount { get; set; }
        public DateTime LastActivity { get; set; }
    }

    /// <summary>
    /// Top project by activity
    /// </summary>
    public class TopProject
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int ActivityCount { get; set; }
        public int FileCount { get; set; }
    }

    /// <summary>
    /// Top project by file count
    /// </summary>
    public class TopFileProject
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int FileCount { get; set; }
        public long SizeMB { get; set; }
    }

    #endregion
}