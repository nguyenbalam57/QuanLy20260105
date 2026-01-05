
using ManagementFile.API.Data;
using ManagementFile.API.Services;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs;
using ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// Controller quản lý Time Tracking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TimeTrackingController : ControllerBase
    {
        private readonly TimeTrackingService _timeTrackingService;
        private readonly ILogger<TimeTrackingController> _logger;
        private readonly ManagementFileDbContext _context;

        public TimeTrackingController(TimeTrackingService timeTrackingService, ILogger<TimeTrackingController> logger, ManagementFileDbContext context)
        {
            _timeTrackingService = timeTrackingService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách time logs với filter
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult<ApiResponse<PagedResult<TimeTrackingTaskTimeLogDto>>>> GetTimeLogs(
            [FromQuery] int taskId = -1,
            [FromQuery] int userId = -1,
            [FromQuery] int projectId = -1,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? isBillable = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "StartTime",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _timeTrackingService.GetTimeLogsAsync(new TimeLogFilterRequest
                {
                    TaskId = taskId,
                    UserId = userId,
                    ProjectId = projectId,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsBillable = isBillable,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                }, currentUserId);

                return Ok(ApiResponse<PagedResult<TimeTrackingTaskTimeLogDto>>.SuccessResult(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time logs");
                return StatusCode(500, ApiResponse<PagedResult<TimeTrackingTaskTimeLogDto>>.ErrorResult("Đã xảy ra lỗi khi lấy danh sách time logs"));
            }
        }

        /// <summary>
        /// Lấy time log theo ID
        /// </summary>
        [HttpGet("logs/{id}")]
        public async Task<ActionResult<ApiResponse<TimeTrackingTaskTimeLogDto>>> GetTimeLog(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timeLog = await _timeTrackingService.GetTimeLogByIdAsync(id, currentUserId);

                if (timeLog == null)
                {
                    return NotFound(ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Không tìm thấy time log"));
                }

                return Ok(ApiResponse<TimeTrackingTaskTimeLogDto>.SuccessResult(timeLog));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time log {TimeLogId}", id);
                return StatusCode(500, ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Đã xảy ra lỗi khi lấy thông tin time log"));
            }
        }

        /// <summary>
        /// Bắt đầu timer cho task
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<ApiResponse<TimeTrackingTaskTimeLogDto>>> StartTimer(StartTimerRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timeLog = await _timeTrackingService.StartTimerAsync(request, currentUserId);

                return Ok(ApiResponse<TimeTrackingTaskTimeLogDto>.SuccessResult(timeLog));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting timer for task {TaskId}", request.TaskId);
                return StatusCode(500, ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Đã xảy ra lỗi khi bắt đầu timer"));
            }
        }

        /// <summary>
        /// Dừng timer hiện tại
        /// </summary>
        [HttpPost("stop")]
        public async Task<ActionResult<ApiResponse<TimeTrackingTaskTimeLogDto>>> StopTimer(StopTimerRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timeLog = await _timeTrackingService.StopTimerAsync(request, currentUserId);

                if (timeLog == null)
                {
                    return NotFound(ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Không tìm thấy timer đang chạy"));
                }

                return Ok(ApiResponse<TimeTrackingTaskTimeLogDto>.SuccessResult(timeLog));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping timer");
                return StatusCode(500, ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Đã xảy ra lỗi khi dừng timer"));
            }
        }

        /// <summary>
        /// Lấy timer hiện tại đang chạy
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<ApiResponse<TimeTrackingTaskTimeLogDto>>> GetCurrentTimer()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timeLog = await _timeTrackingService.GetCurrentTimerAsync(currentUserId);

                return Ok(ApiResponse<TimeTrackingTaskTimeLogDto>.SuccessResult(timeLog));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current timer");
                return StatusCode(500, ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Đã xảy ra lỗi khi lấy timer hiện tại"));
            }
        }

        /// <summary>
        /// Thêm time log thủ công
        /// </summary>
        [HttpPost("logs")]
        public async Task<ActionResult<ApiResponse<TimeTrackingTaskTimeLogDto>>> CreateTimeLog(CreateTimeLogRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timeLog = await _timeTrackingService.CreateTimeLogAsync(request, currentUserId);

                return Ok(ApiResponse<TimeTrackingTaskTimeLogDto>.SuccessResult(timeLog));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time log");
                return StatusCode(500, ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Đã xảy ra lỗi khi tạo time log"));
            }
        }

        /// <summary>
        /// Cập nhật time log
        /// </summary>
        [HttpPut("logs/{id}")]
        public async Task<ActionResult<ApiResponse<TimeTrackingTaskTimeLogDto>>> UpdateTimeLog(int id, UpdateTimeLogRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timeLog = await _timeTrackingService.UpdateTimeLogAsync(id, request, currentUserId);

                if (timeLog == null)
                {
                    return NotFound(ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Không tìm thấy time log"));
                }

                return Ok(ApiResponse<TimeTrackingTaskTimeLogDto>.SuccessResult(timeLog));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time log {TimeLogId}", id);
                return StatusCode(500, ApiResponse<TimeTrackingTaskTimeLogDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật time log"));
            }
        }

        /// <summary>
        /// Xóa time log
        /// </summary>
        [HttpDelete("logs/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTimeLog(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var success = await _timeTrackingService.DeleteTimeLogAsync(id, currentUserId);

                if (!success)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Không tìm thấy time log"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time log {TimeLogId}", id);
                return StatusCode(500, ApiResponse<bool>.ErrorResult("Đã xảy ra lỗi khi xóa time log"));
            }
        }

        /// <summary>
        /// Lấy báo cáo thời gian làm việc
        /// </summary>
        [HttpGet("reports/summary")]
        public async Task<ActionResult<ApiResponse<TimeTrackingSummaryDto>>> GetTimeTrackingSummary(
            [FromQuery] int userId = -1,
            [FromQuery] int projectId = -1,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var summary = await _timeTrackingService.GetTimeTrackingSummaryAsync(
                    userId, projectId, startDate, endDate, currentUserId);

                return Ok(ApiResponse<TimeTrackingSummaryDto>.SuccessResult(summary));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time tracking summary");
                return StatusCode(500, ApiResponse<TimeTrackingSummaryDto>.ErrorResult("Đã xảy ra lỗi khi lấy báo cáo thời gian"));
            }
        }

        /// <summary>
        /// Lấy báo cáo chi tiết thời gian theo user
        /// </summary>
        [HttpGet("reports/user/{userId}")]
        public async Task<ActionResult<ApiResponse<UserTimeReportDto>>> GetUserTimeReport(
            int userId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var report = await _timeTrackingService.GetUserTimeReportAsync(userId, startDate, endDate, currentUserId);

                return Ok(ApiResponse<UserTimeReportDto>.SuccessResult(report));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user time report");
                return StatusCode(500, ApiResponse<UserTimeReportDto>.ErrorResult("Đã xảy ra lỗi khi lấy báo cáo thời gian người dùng"));
            }
        }

        /// <summary>
        /// Lấy báo cáo thời gian theo project
        /// </summary>
        [HttpGet("reports/project/{projectId}")]
        public async Task<ActionResult<ApiResponse<ProjectTimeReportDto>>> GetProjectTimeReport(
            int projectId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var report = await _timeTrackingService.GetProjectTimeReportAsync(projectId, startDate, endDate, currentUserId);

                return Ok(ApiResponse<ProjectTimeReportDto>.SuccessResult(report));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project time report");
                return StatusCode(500, ApiResponse<ProjectTimeReportDto>.ErrorResult("Đã xảy ra lỗi khi lấy báo cáo thời gian dự án"));
            }
        }

        /// <summary>
        /// Lấy weekly timesheet
        /// </summary>
        [HttpPost("weekly/get")]
        public async Task<ActionResult<ApiResponse<WeeklyTimesheetDto>>> GetWeeklyTimesheet(
            GetWeeklyTimesheetRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timesheet = await _timeTrackingService.GetWeeklyTimesheetAsync(request, currentUserId);

                return Ok(ApiResponse<WeeklyTimesheetDto>.SuccessResult(timesheet));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly timesheet");
                return StatusCode(500, ApiResponse<WeeklyTimesheetDto>.ErrorResult(
                    "Đã xảy ra lỗi khi lấy weekly timesheet"));
            }
        }

        /// <summary>
        /// Lưu weekly timesheet
        /// </summary>
        [HttpPost("weekly/save")]
        public async Task<ActionResult<ApiResponse<WeeklyTimesheetDto>>> SaveWeeklyTimesheet(
            SaveWeeklyTimesheetRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Validate trước khi lưu
                var validation = await _timeTrackingService.ValidateWeeklyTimesheetAsync(request, currentUserId);

                if (!validation.IsValid)
                {
                    return BadRequest(ApiResponse<WeeklyTimesheetDto>.ErrorResult(
                        "Timesheet không hợp lệ",
                        validation.Errors));
                }

                var timesheet = await _timeTrackingService.SaveWeeklyTimesheetAsync(request, currentUserId);

                return Ok(ApiResponse<WeeklyTimesheetDto>.SuccessResult(timesheet));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<WeeklyTimesheetDto>.ErrorResult(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving weekly timesheet");
                return StatusCode(500, ApiResponse<WeeklyTimesheetDto>.ErrorResult(
                    "Đã xảy ra lỗi khi lưu weekly timesheet"));
            }
        }

        /// <summary>
        /// Copy timesheet từ tuần khác
        /// </summary>
        [HttpPost("weekly/copy")]
        public async Task<ActionResult<ApiResponse<WeeklyTimesheetDto>>> CopyWeeklyTimesheet(
            CopyWeekTimesheetRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timesheet = await _timeTrackingService.CopyWeeklyTimesheetAsync(request, currentUserId);

                return Ok(ApiResponse<WeeklyTimesheetDto>.SuccessResult(timesheet));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<WeeklyTimesheetDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying weekly timesheet");
                return StatusCode(500, ApiResponse<WeeklyTimesheetDto>.ErrorResult(
                    "Đã xảy ra lỗi khi copy weekly timesheet"));
            }
        }

        /// <summary>
        /// Lấy danh sách tasks available cho timesheet
        /// </summary>
        [HttpGet("weekly/available-tasks")]
        public async Task<ActionResult<ApiResponse<List<TaskForTimesheetDto>>>> GetAvailableTasksForTimesheet(
            [FromQuery] int? projectId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var tasks = await _timeTrackingService.GetAvailableTasksForTimesheetAsync(currentUserId, projectId);

                return Ok(ApiResponse<List<TaskForTimesheetDto>>.SuccessResult(tasks));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available tasks");
                return StatusCode(500, ApiResponse<List<TaskForTimesheetDto>>.ErrorResult(
                    "Đã xảy ra lỗi khi lấy danh sách tasks"));
            }
        }

        /// <summary>
        /// Validate weekly timesheet
        /// </summary>
        [HttpPost("weekly/validate")]
        public async Task<ActionResult<ApiResponse<WeeklyTimesheetValidationResult>>> ValidateWeeklyTimesheet(
            SaveWeeklyTimesheetRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var validation = await _timeTrackingService.ValidateWeeklyTimesheetAsync(request, currentUserId);

                return Ok(ApiResponse<WeeklyTimesheetValidationResult>.SuccessResult(validation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating weekly timesheet");
                return StatusCode(500, ApiResponse<WeeklyTimesheetValidationResult>.ErrorResult(
                    "Đã xảy ra lỗi khi validate timesheet"));
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