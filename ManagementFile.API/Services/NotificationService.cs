using ManagementFile.API.Data;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.DTOs.NotificationsAndCommunications;
using ManagementFile.Contracts.Requests.NotificationsAndCommunications;
using ManagementFile.Models.NotificationsAndCommunications;
using Microsoft.EntityFrameworkCore;
using ManagementFile.Contracts.DTOs.Common;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Service xử lý business logic cho Notifications
    /// </summary>
    public class NotificationService
    {
        private readonly ManagementFileDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ManagementFileDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách notifications với filter và pagination
        /// </summary>
        public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(
            NotificationFilterRequest filter, int currentUserId)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == filter.UserId)
                .AsQueryable();

            // Check permissions
            if (filter.UserId != currentUserId)
            {
                var user = await _context.Users.FindAsync(currentUserId);
                if (user?.Role != UserRole.Admin)
                {
                    throw new UnauthorizedAccessException("Bạn chỉ có thể xem thông báo của mình");
                }
            }

            // Apply filters
            if (filter.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == filter.IsRead.Value);
            }

            if (filter.Type.HasValue)
            {
                query = query.Where(n => n.Type == filter.Type.Value);
            }

            // Filter out expired notifications
            var now = DateTime.UtcNow;
            query = query.Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > now);

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);

            // Apply pagination
            var notifications = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var notificationDtos = notifications.Select(MapToNotificationDto).ToList();

            return new PagedResult<NotificationDto>
            {
                Items = notificationDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        /// <summary>
        /// Lấy notification theo ID
        /// </summary>
        public async Task<NotificationDto?> GetNotificationByIdAsync(int notificationId, int currentUserId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
                return null;

            // Check permissions
            if (notification.UserId != currentUserId)
            {
                var user = await _context.Users.FindAsync(currentUserId);
                if (user?.Role != UserRole.Admin)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xem thông báo này");
                }
            }

            // Automatically mark as read when viewed
            if (!notification.IsRead && notification.UserId == currentUserId)
            {
                notification.MarkAsRead();
                await _context.SaveChangesAsync();
            }

            return MapToNotificationDto(notification);
        }

        /// <summary>
        /// Tạo notification mới (System use)
        /// </summary>
        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, int currentUserId)
        {
            // Validate user exists
            var targetUser = await _context.Users.FindAsync(request.UserId);
            if (targetUser == null)
            {
                throw new ArgumentException("User không tồn tại");
            }

            var notification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                RelatedEntityType = request.RelatedEntityType ?? "",
                RelatedEntityId = request.RelatedEntityId,
                ActionUrl = request.ActionUrl ?? "",
                ExpiresAt = request.ExpiresAt,
                IsRead = false,
                CreatedBy = currentUserId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return MapToNotificationDto(notification);
        }

        /// <summary>
        /// Đánh dấu notification đã đọc
        /// </summary>
        public async Task<NotificationDto?> MarkAsReadAsync(int notificationId, int currentUserId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
                return null;

            // Check permissions
            if (notification.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền đánh dấu thông báo này");
            }

            if (!notification.IsRead)
            {
                notification.MarkAsRead();
                await _context.SaveChangesAsync();
            }

            return MapToNotificationDto(notification);
        }

        /// <summary>
        /// Đánh dấu tất cả notifications đã đọc
        /// </summary>
        public async Task<int> MarkAllAsReadAsync(int currentUserId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == currentUserId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.MarkAsRead();
            }

            await _context.SaveChangesAsync();
            return unreadNotifications.Count;
        }

        /// <summary>
        /// Xóa notification
        /// </summary>
        public async Task<bool> DeleteNotificationAsync(int notificationId, int currentUserId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
                return false;

            // Check permissions
            if (notification.UserId != currentUserId)
            {
                var user = await _context.Users.FindAsync(currentUserId);
                if (user?.Role != UserRole.Admin)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa thông báo này");
                }
            }

            notification.SoftDelete(currentUserId, "Deleted by user");
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Lấy số lượng notifications chưa đọc
        /// </summary>
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            var now = DateTime.UtcNow;
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && 
                          (!n.ExpiresAt.HasValue || n.ExpiresAt.Value > now));
        }

        /// <summary>
        /// Gửi notification cho user cụ thể
        /// </summary>
        public async Task<NotificationDto> SendNotificationAsync(SendNotificationRequest request, int currentUserId)
        {
            // Check permissions
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != UserRole.Admin && user?.Role != UserRole.Manager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền gửi thông báo");
            }

            var createRequest = new CreateNotificationRequest
            {
                UserId = request.UserId,
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                ActionUrl = request.ActionUrl,
                ExpiresAt = request.ExpiresAt
            };

            return await CreateNotificationAsync(createRequest, currentUserId);
        }

        /// <summary>
        /// Broadcast notification cho nhiều users
        /// </summary>
        public async Task<List<NotificationDto>> BroadcastNotificationAsync(
            BroadcastNotificationRequest request, int currentUserId)
        {
            // Check permissions
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Chỉ Admin mới có thể broadcast thông báo");
            }

            // Determine target users
            var targetUsers = new List<int>();

            if (request.UserIds?.Count > 0)
            {
                // Send to specific users
                targetUsers = request.UserIds;
            }
            else
            {
                // Build query for target users
                var userQuery = _context.Users.Where(u => u.IsActive);

                if (request.TargetRole.HasValue)
                {
                    userQuery = userQuery.Where(u => u.Role == request.TargetRole.Value);
                }

                if (request.TargetDepartment.HasValue)
                {
                    userQuery = userQuery.Where(u => u.Department == request.TargetDepartment.Value);
                }

                targetUsers = await userQuery.Select(u => u.Id).ToListAsync();
            }

            var notifications = new List<NotificationDto>();

            foreach (var userId in targetUsers)
            {
                var createRequest = new CreateNotificationRequest
                {
                    UserId = userId,
                    Title = request.Title,
                    Content = request.Content,
                    Type = request.Type,
                    ActionUrl = request.ActionUrl,
                    ExpiresAt = request.ExpiresAt
                };

                try
                {
                    var notification = await CreateNotificationAsync(createRequest, currentUserId);
                    notifications.Add(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating broadcast notification for user {UserId}", userId);
                    // Continue with other users
                }
            }

            return notifications;
        }

        /// <summary>
        /// Lấy notifications theo project
        /// </summary>
        public async Task<PagedResult<NotificationDto>> GetProjectNotificationsAsync(
            int projectId, int pageNumber, int pageSize, int currentUserId)
        {
            // Check project access
            await CheckProjectAccessAsync(projectId, currentUserId);

            var query = _context.Notifications
                .Where(n => n.UserId == currentUserId && 
                          n.RelatedEntityType == "Project" && 
                          n.RelatedEntityId == projectId);

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var notificationDtos = notifications.Select(MapToNotificationDto).ToList();

            return new PagedResult<NotificationDto>
            {
                Items = notificationDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Dọn dẹp notifications hết hạn
        /// </summary>
        public async Task<int> CleanupExpiredNotificationsAsync(int currentUserId)
        {
            // Check permissions
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Chỉ Admin mới có thể dọn dẹp thông báo");
            }

            var now = DateTime.UtcNow;
            var expiredNotifications = await _context.Notifications
                .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= now)
                .ToListAsync();

            foreach (var notification in expiredNotifications)
            {
                notification.SoftDelete(currentUserId, "Expired notification cleanup");
            }

            await _context.SaveChangesAsync();
            return expiredNotifications.Count;
        }

        #region System Notification Methods

        /// <summary>
        /// Send task assignment notification
        /// </summary>
        public async Task SendTaskAssignmentNotificationAsync(int taskId, int assignedUserId, int assignedBy)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return;

            var assignerUser = await _context.Users.FindAsync(assignedBy);
            var assignerName = assignerUser?.FullName ?? "System";

            await CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = assignedUserId,
                Title = "Bạn được giao nhiệm vụ mới",
                Content = $"{assignerName} đã giao cho bạn nhiệm vụ '{task.Title}' trong dự án '{task.Project?.ProjectName}'",
                Type = NotificationType.TaskAssigned,
                RelatedEntityType = "Task",
                RelatedEntityId = taskId,
                ActionUrl = $"/projects/{task.ProjectId}/tasks/{taskId}"
            }, assignedBy);
        }

        /// <summary>
        /// Send task completion notification
        /// </summary>
        public async Task SendTaskCompletionNotificationAsync(int taskId, int completedBy)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return;

            var completerUser = await _context.Users.FindAsync(completedBy);
            var completerName = completerUser?.FullName ?? "System";

            // Notify project manager
            if (task.Project?.ProjectManagerId >= 0 && task.Project.ProjectManagerId != completedBy)
            {
                await CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = task.Project.ProjectManagerId ?? 0,
                    Title = "Nhiệm vụ đã hoàn thành",
                    Content = $"{completerName} đã hoàn thành nhiệm vụ '{task.Title}' trong dự án '{task.Project.ProjectName}'",
                    Type = NotificationType.TaskCompleted,
                    RelatedEntityType = "Task",
                    RelatedEntityId = taskId,
                    ActionUrl = $"/projects/{task.ProjectId}/tasks/{taskId}"
                }, completedBy);
            }
        }

        /// <summary>
        /// Send task overdue notification
        /// </summary>
        public async Task SendTaskOverdueNotificationAsync(int taskId)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null || task.AssignedToId < 0) return;

            await CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = task.AssignedToId ?? -1,
                Title = "Nhiệm vụ quá hạn",
                Content = $"Nhiệm vụ '{task.Title}' trong dự án '{task.Project?.ProjectName}' đã quá hạn hoàn thành",
                Type = NotificationType.TaskOverdue,
                RelatedEntityType = "Task",
                RelatedEntityId = taskId,
                ActionUrl = $"/projects/{task.ProjectId}/tasks/{taskId}"
            }, -1);
        }

        /// <summary>
        /// Send project invitation notification
        /// </summary>
        public async Task SendProjectInvitationNotificationAsync(int projectId, int userId, int invitedBy)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return;

            var inviterUser = await _context.Users.FindAsync(invitedBy);
            var inviterName = inviterUser?.FullName ?? "System";

            await CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = userId,
                Title = "Mời tham gia dự án",
                Content = $"{inviterName} đã mời bạn tham gia dự án '{project.ProjectName}'",
                Type = NotificationType.ProjectInvitation,
                RelatedEntityType = "Project",
                RelatedEntityId = projectId,
                ActionUrl = $"/projects/{projectId}"
            }, invitedBy);
        }

        /// <summary>
        /// Send file upload notification
        /// </summary>
        public async Task SendFileUploadNotificationAsync(int fileId, int projectId, int uploadedBy)
        {
            var file = await _context.ProjectFiles.FindAsync(fileId);
            var project = await _context.Projects.FindAsync(projectId);
            
            if (file == null || project == null) return;

            var uploaderUser = await _context.Users.FindAsync(uploadedBy);
            var uploaderName = uploaderUser?.FullName ?? "System";

            // Notify project members
            var projectMembers = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId && pm.IsActive && pm.UserId != uploadedBy)
                .Select(pm => pm.UserId)
                .ToListAsync();

            foreach (var memberId in projectMembers)
            {
                await CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = memberId,
                    Title = "File mới được upload",
                    Content = $"{uploaderName} đã upload file '{file.FileName}' vào dự án '{project.ProjectName}'",
                    Type = NotificationType.FileUploaded,
                    RelatedEntityType = "ProjectFile",
                    RelatedEntityId = fileId,
                    ActionUrl = $"/projects/{projectId}/files/{fileId}"
                }, uploadedBy);
            }
        }

        #endregion

        #region Private Helper Methods

        private IQueryable<Notification> ApplySorting(IQueryable<Notification> query, string sortBy, string sortDirection)
        {
            var ascending = sortDirection?.ToLower() != "desc";

            return sortBy?.ToLower() switch
            {
                "title" => ascending ? query.OrderBy(n => n.Title) : query.OrderByDescending(n => n.Title),
                "type" => ascending ? query.OrderBy(n => n.Type) : query.OrderByDescending(n => n.Type),
                "isread" => ascending ? query.OrderBy(n => n.IsRead) : query.OrderByDescending(n => n.IsRead),
                "readat" => ascending ? query.OrderBy(n => n.ReadAt) : query.OrderByDescending(n => n.ReadAt),
                _ => ascending ? query.OrderBy(n => n.CreatedAt) : query.OrderByDescending(n => n.CreatedAt),
            };
        }

        private async Task CheckProjectAccessAsync(int projectId, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            var isMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId && pm.IsActive);

            if (!isMember)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập dự án này");
            }
        }

        private static NotificationDto MapToNotificationDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Content = notification.Content,
                Type = notification.Type, // Keep as enum, don't convert to string
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                RelatedEntityType = notification.RelatedEntityType,
                RelatedEntityId = notification.RelatedEntityId,
                ActionUrl = notification.ActionUrl,
                ExpiresAt = notification.ExpiresAt,
                IsExpired = notification.IsExpired,
                CreatedAt = notification.CreatedAt,
                CreatedBy = notification.CreatedBy
            };
        }

        #endregion
    }

}