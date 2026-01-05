using ManagementFile.API.Data;
using ManagementFile.API.Services;
using ManagementFile.Contracts.DTOs.Admin;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// Controller quản lý Admin Dashboard và Statistics
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminDashboardService _adminService;
        private readonly ILogger<AdminController> _logger;
        private readonly ManagementFileDbContext _context;

        public AdminController(AdminDashboardService adminService, ILogger<AdminController> logger, ManagementFileDbContext context)
        {
            _adminService = adminService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Lấy dashboard overview cho admin
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminApiResponse<AdminDashboardDto>>> GetDashboard()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var dashboard = await _adminService.GetDashboardOverviewAsync(currentUserId);

                return Ok(AdminApiResponse<AdminDashboardDto>.SuccessResult(dashboard));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin dashboard");
                return StatusCode(500, AdminApiResponse<AdminDashboardDto>.ErrorResult("Đã xảy ra lỗi khi lấy dashboard"));
            }
        }

        /// <summary>
        /// Lấy thống kê users
        /// </summary>
        [HttpGet("statistics/users")]
        public async Task<ActionResult<AdminApiResponse<UserStatisticsDto>>> GetUserStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var stats = await _adminService.GetUserStatisticsAsync(startDate, endDate, currentUserId);

                return Ok(AdminApiResponse<UserStatisticsDto>.SuccessResult(stats));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return StatusCode(500, AdminApiResponse<UserStatisticsDto>.ErrorResult("Đã xảy ra lỗi khi lấy thống kê users"));
            }
        }

        /// <summary>
        /// Lấy thống kê projects
        /// </summary>
        [HttpGet("statistics/projects")]
        public async Task<ActionResult<AdminApiResponse<ProjectStatisticsDto>>> GetProjectStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var stats = await _adminService.GetProjectStatisticsAsync(startDate, endDate, currentUserId);

                return Ok(AdminApiResponse<ProjectStatisticsDto>.SuccessResult(stats));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project statistics");
                return StatusCode(500, AdminApiResponse<ProjectStatisticsDto>.ErrorResult("Đã xảy ra lỗi khi lấy thống kê projects"));
            }
        }

        /// <summary>
        /// Lấy thống kê tasks
        /// </summary>
        [HttpGet("statistics/tasks")]
        public async Task<ActionResult<AdminApiResponse<TaskStatisticsDto>>> GetTaskStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var stats = await _adminService.GetTaskStatisticsAsync(startDate, endDate, currentUserId);

                return Ok(AdminApiResponse<TaskStatisticsDto>.SuccessResult(stats));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task statistics");
                return StatusCode(500, AdminApiResponse<TaskStatisticsDto>.ErrorResult("Đã xảy ra lỗi khi lấy thống kê tasks"));
            }
        }

        /// <summary>
        /// Lấy thống kê files
        /// </summary>
        [HttpGet("statistics/files")]
        public async Task<ActionResult<AdminApiResponse<FileStatisticsDto>>> GetFileStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var stats = await _adminService.GetFileStatisticsAsync(startDate, endDate, currentUserId);

                return Ok(AdminApiResponse<FileStatisticsDto>.SuccessResult(stats));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file statistics");
                return StatusCode(500, AdminApiResponse<FileStatisticsDto>.ErrorResult("Đã xảy ra lỗi khi lấy thống kê files"));
            }
        }

        /// <summary>
        /// Lấy thống kê time tracking
        /// </summary>
        [HttpGet("statistics/time-tracking")]
        public async Task<ActionResult<AdminApiResponse<AdminTimeTrackingStatsDto>>> GetTimeTrackingStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var stats = await _adminService.GetTimeTrackingStatisticsAsync(startDate, endDate, currentUserId);

                return Ok(AdminApiResponse<AdminTimeTrackingStatsDto>.SuccessResult(stats));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time tracking statistics");
                return StatusCode(500, AdminApiResponse<AdminTimeTrackingStatsDto>.ErrorResult("Đã xảy ra lỗi khi lấy thống kê time tracking"));
            }
        }

        /// <summary>
        /// Lấy audit logs
        /// </summary>
        [HttpGet("audit-logs")]
        public async Task<ActionResult<AdminApiResponse<PagedResult<AuditLogDto>>>> GetAuditLogs(
            [FromQuery] int userId = -1,
            [FromQuery] string? entityType = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _adminService.GetAuditLogsAsync(new AuditLogFilterRequest
                {
                    UserId = userId,
                    EntityType = entityType,
                    Action = action,
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                }, currentUserId);

                return Ok(AdminApiResponse<PagedResult<AuditLogDto>>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return StatusCode(500, AdminApiResponse<PagedResult<AuditLogDto>>.ErrorResult("Đã xảy ra lỗi khi lấy audit logs"));
            }
        }

        /// <summary>
        /// Lấy user sessions đang active
        /// </summary>
        [HttpGet("active-sessions")]
        public async Task<ActionResult<AdminApiResponse<PagedResult<ActiveSessionDto>>>> GetActiveSessions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _adminService.GetActiveSessionsAsync(pageNumber, pageSize, currentUserId);

                return Ok(AdminApiResponse<PagedResult<ActiveSessionDto>>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions");
                return StatusCode(500, AdminApiResponse<PagedResult<ActiveSessionDto>>.ErrorResult("Đã xảy ra lỗi khi lấy active sessions"));
            }
        }

        /// <summary>
        /// Terminate user session
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<ActionResult<AdminApiResponse<bool>>> TerminateSession(int sessionId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var success = await _adminService.TerminateSessionAsync(sessionId, currentUserId);

                if (!success)
                {
                    return NotFound(AdminApiResponse<bool>.ErrorResult("Không tìm thấy session"));
                }

                return Ok(AdminApiResponse<bool>.SuccessResult(true));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
                return StatusCode(500, AdminApiResponse<bool>.ErrorResult("Đã xảy ra lỗi khi terminate session"));
            }
        }

        /// <summary>
        /// Lấy system health status
        /// </summary>
        [HttpGet("system-health")]
        public async Task<ActionResult<AdminApiResponse<SystemHealthDto>>> GetSystemHealth()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var health = await _adminService.GetSystemHealthAsync(currentUserId);

                return Ok(AdminApiResponse<SystemHealthDto>.SuccessResult(health));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return StatusCode(500, AdminApiResponse<SystemHealthDto>.ErrorResult("Đã xảy ra lỗi khi lấy system health"));
            }
        }

        /// <summary>
        /// Cleanup system data
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<ActionResult<AdminApiResponse<SystemCleanupResultDto>>> CleanupSystem(SystemCleanupRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _adminService.CleanupSystemAsync(request, currentUserId);

                return Ok(AdminApiResponse<SystemCleanupResultDto>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up system");
                return StatusCode(500, AdminApiResponse<SystemCleanupResultDto>.ErrorResult("Đã xảy ra lỗi khi cleanup system"));
            }
        }

        /// <summary>
        /// Lấy top performers report
        /// </summary>
        [HttpGet("reports/top-performers")]
        public async Task<ActionResult<AdminApiResponse<List<TopPerformerDto>>>> GetTopPerformers(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var performers = await _adminService.GetTopPerformersAsync(startDate, endDate, limit, currentUserId);

                return Ok(AdminApiResponse<List<TopPerformerDto>>.SuccessResult(performers));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top performers");
                return StatusCode(500, AdminApiResponse<List<TopPerformerDto>>.ErrorResult("Đã xảy ra lỗi khi lấy top performers"));
            }
        }

        /// <summary>
        /// Lấy project health report
        /// </summary>
        [HttpGet("reports/project-health")]
        public async Task<ActionResult<AdminApiResponse<List<ProjectHealthDto>>>> GetProjectHealthReport()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var report = await _adminService.GetProjectHealthReportAsync(currentUserId);

                return Ok(AdminApiResponse<List<ProjectHealthDto>>.SuccessResult(report));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project health report");
                return StatusCode(500, AdminApiResponse<List<ProjectHealthDto>>.ErrorResult("Đã xảy ra lỗi khi lấy project health report"));
            }
        }

        /// <summary>
        /// Export data (CSV/Excel)
        /// </summary>
        [HttpPost("export")]
        public async Task<ActionResult<AdminApiResponse<string>>> ExportData(DataExportRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var downloadUrl = await _adminService.ExportDataAsync(request, currentUserId);

                return Ok(AdminApiResponse<string>.SuccessResult(downloadUrl, "Export đã được tạo thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(AdminApiResponse<string>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                return StatusCode(500, AdminApiResponse<string>.ErrorResult("Đã xảy ra lỗi khi export data"));
            }
        }

        private int GetCurrentUserId()
        {
            var sessionToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(sessionToken))
                return -1;

            var session = _context.UserSessions
                .FirstOrDefault(s => s.SessionToken == sessionToken && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            return session.UserId;
        }
    }
}