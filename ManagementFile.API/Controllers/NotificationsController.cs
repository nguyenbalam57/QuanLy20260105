using ManagementFile.API.Data;
using ManagementFile.API.Services;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.NotificationsAndCommunications;
using ManagementFile.Contracts.Requests.NotificationsAndCommunications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// Controller quản lý Notifications
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;
        private readonly ManagementFileDbContext _context; // Giả sử bạn có DbContext để truy cập UserSessions 

        public NotificationsController(NotificationService notificationService, ILogger<NotificationsController> logger, ManagementFileDbContext context)
        {
            _notificationService = notificationService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Tạo notification (Admin/System use)
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateNotification(CreateNotificationRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var notification = await _notificationService.CreateNotificationAsync(request, currentUserId);

                return Ok(ApiResponse<NotificationDto>.SuccessResult(notification));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<NotificationDto>.ErrorResult(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, ApiResponse<NotificationDto>.ErrorResult("Đã xảy ra lỗi khi tạo thông báo"));
            }
        }

        /// <summary>
        /// Gửi notification cho user cụ thể (Admin/Manager only)
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> SendNotification(SendNotificationRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var notification = await _notificationService.SendNotificationAsync(request, currentUserId);

                return Ok(ApiResponse<NotificationDto>.SuccessResult(notification));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<NotificationDto>.ErrorResult(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return StatusCode(500, ApiResponse<NotificationDto>.ErrorResult("Đã xảy ra lỗi khi gửi thông báo"));
            }
        }

        /// <summary>
        /// Gửi notification broadcast cho tất cả users (Admin only)
        /// </summary>
        [HttpPost("broadcast")]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>>
            BroadcastNotification(BroadcastNotificationRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var notifications = await _notificationService.BroadcastNotificationAsync(request, currentUserId);

                return Ok(ApiResponse<List<NotificationDto>>.SuccessResult(notifications));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<List<NotificationDto>>.ErrorResult(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification");
                return StatusCode(500, ApiResponse<List<NotificationDto>>.ErrorResult("Đã xảy ra lỗi khi broadcast thông báo"));
            }
        }

        /// <summary>
        /// Lấy notifications với filter
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetNotifications(
            [FromQuery] int userId = -1,
            [FromQuery] string? type = null,
            [FromQuery] bool? isRead = null,
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
                var result = await _notificationService.GetNotificationsAsync(new NotificationFilterRequest
                {
                    UserId = userId,
                    IsRead = isRead,
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                }, currentUserId);

                return Ok(ApiResponse<PagedResult<NotificationDto>>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, ApiResponse<PagedResult<NotificationDto>>.ErrorResult("Đã xảy ra lỗi khi lấy danh sách thông báo"));
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