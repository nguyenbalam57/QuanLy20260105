using ManagementFile.API.Data;
using ManagementFile.Models.AuditAndLogging;
using ManagementFile.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using System.IO;
using ManagementFile.Contracts.DTOs.Admin;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.Requests.Admin;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Service xử lý admin dashboard và statistics
    /// </summary>
    public class AdminDashboardService
    {
        private readonly ManagementFileDbContext _context;
        private readonly ILogger<AdminDashboardService> _logger;

        public AdminDashboardService(ManagementFileDbContext context, ILogger<AdminDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy dashboard overview
        /// </summary>
        public async Task<AdminDashboardDto> GetDashboardOverviewAsync(int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            // Basic counts
            var totalUsers = await _context.Users.CountAsync(u => u.IsActive);
            var totalProjects = await _context.Projects.CountAsync(p => p.IsActive);
            var totalTasks = await _context.ProjectTasks.CountAsync(t => t.IsActive);
            var totalFiles = await _context.ProjectFiles.CountAsync(f => f.IsActive);

            // Active counts
            var activeProjects = await _context.Projects
                .CountAsync(p => p.IsActive && p.Status == ProjectStatus.Active);

            var overdueTasks = await _context.ProjectTasks
                .CountAsync(t => t.IsActive && t.DueDate.HasValue && t.DueDate.Value < now && 
                           t.Status != TaskStatuss.Completed);

            var pendingApprovals = await _context.ProjectFiles
                .CountAsync(f => f.IsActive && f.RequireApproval && f.ApprovalStatus == "Pending");

            // Recent activity counts (last 30 days)
            var newUsersThisMonth = await _context.Users
                .CountAsync(u => u.CreatedAt >= thirtyDaysAgo);

            var newProjectsThisMonth = await _context.Projects
                .CountAsync(p => p.CreatedAt >= thirtyDaysAgo);

            var completedTasksThisMonth = await _context.ProjectTasks
                .CountAsync(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= thirtyDaysAgo);

            var filesUploadedThisMonth = await _context.ProjectFiles
                .CountAsync(f => f.CreatedAt >= thirtyDaysAgo);

            // Storage statistics
            var totalFileSize = await _context.ProjectFiles
                .Where(f => f.IsActive)
                .SumAsync(f => f.CurrentFileSize);

            // Time tracking statistics
            var totalHoursThisMonth = await _context.TaskTimeLogs
                .Where(ttl => ttl.StartTime >= thirtyDaysAgo)
                .SumAsync(ttl => ttl.Duration);

            return new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                TotalProjects = totalProjects,
                TotalTasks = totalTasks,
                TotalFiles = totalFiles,
                ActiveProjects = activeProjects,
                OverdueTasks = overdueTasks,
                PendingApprovals = pendingApprovals,
                NewUsersThisMonth = newUsersThisMonth,
                NewProjectsThisMonth = newProjectsThisMonth,
                CompletedTasksThisMonth = completedTasksThisMonth,
                FilesUploadedThisMonth = filesUploadedThisMonth,
                TotalStorageUsed = totalFileSize,
                TotalHoursLoggedThisMonth = Math.Round((decimal)totalHoursThisMonth / 60, 2),
                LastUpdated = now
            };
        }

        /// <summary>
        /// Lấy thống kê users
        /// </summary>
        public async Task<UserStatisticsDto> GetUserStatisticsAsync(
            DateTime? startDate, DateTime? endDate, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= start && u.CreatedAt <= end);

            var usersByRole = await _context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var usersByDepartment = await _context.Users
                .GroupBy(u => u.Department)
                .Select(g => new { Department = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var userRegistrationTrend = await GetUserRegistrationTrendAsync(start, end);

            return new UserStatisticsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = totalUsers - activeUsers,
                NewUsers = newUsers,
                UsersByRole = usersByRole.ToDictionary(x => x.Role, x => x.Count),
                UsersByDepartment = usersByDepartment.ToDictionary(x => x.Department, x => x.Count),
                RegistrationTrend = userRegistrationTrend,
                PeriodStart = start,
                PeriodEnd = end
            };
        }

        /// <summary>
        /// Lấy thống kê projects
        /// </summary>
        public async Task<ProjectStatisticsDto> GetProjectStatisticsAsync(
            DateTime? startDate, DateTime? endDate, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var totalProjects = await _context.Projects.CountAsync();
            var activeProjects = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Active);
            var completedProjects = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Completed);
            var overdueProjects = await _context.Projects
                .CountAsync(p => p.PlannedEndDate.HasValue && p.PlannedEndDate.Value < DateTime.UtcNow && 
                           p.Status == ProjectStatus.Active);

            var newProjects = await _context.Projects
                .CountAsync(p => p.CreatedAt >= start && p.CreatedAt <= end);

            var projectsByStatus = await _context.Projects
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var projectsByPriority = await _context.Projects
                .GroupBy(p => p.Priority)
                .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var avgCompletionPercentage = await _context.Projects
                .Where(p => p.IsActive)
                .AverageAsync(p => (double?)p.CompletionPercentage) ?? 0;

            var totalBudget = await _context.Projects.SumAsync(p => p.EstimatedBudget);
            var actualBudget = await _context.Projects.SumAsync(p => p.ActualBudget);

            return new ProjectStatisticsDto
            {
                TotalProjects = totalProjects,
                ActiveProjects = activeProjects,
                CompletedProjects = completedProjects,
                OverdueProjects = overdueProjects,
                NewProjects = newProjects,
                ProjectsByStatus = projectsByStatus.ToDictionary(x => x.Status, x => x.Count),
                ProjectsByPriority = projectsByPriority.ToDictionary(x => x.Priority, x => x.Count),
                AverageCompletionPercentage = Math.Round((decimal)avgCompletionPercentage, 2),
                TotalEstimatedBudget = totalBudget,
                TotalActualBudget = actualBudget,
                BudgetVariance = actualBudget - totalBudget,
                PeriodStart = start,
                PeriodEnd = end
            };
        }

        /// <summary>
        /// Lấy thống kê tasks
        /// </summary>
        public async Task<TaskStatisticsDto> GetTaskStatisticsAsync(
            DateTime? startDate, DateTime? endDate, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var totalTasks = await _context.ProjectTasks.CountAsync();
            var completedTasks = await _context.ProjectTasks
                .CountAsync(t => t.Status == TaskStatuss.Completed);
            var overdueTasks = await _context.ProjectTasks
                .CountAsync(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && 
                           t.Status != TaskStatuss.Completed);

            var newTasks = await _context.ProjectTasks
                .CountAsync(t => t.CreatedAt >= start && t.CreatedAt <= end);

            var tasksByStatus = await _context.ProjectTasks
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var tasksByPriority = await _context.ProjectTasks
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var avgProgress = await _context.ProjectTasks
                .Where(t => t.IsActive)
                .AverageAsync(t => (double?)t.Progress) ?? 0;

            var completionRate = totalTasks > 0 ? Math.Round((decimal)completedTasks / totalTasks * 100, 2) : 0;

            return new TaskStatisticsDto
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                InProgressTasks = await _context.ProjectTasks
                    .CountAsync(t => t.Status == TaskStatuss.InProgress),
                OverdueTasks = overdueTasks,
                NewTasks = newTasks,
                TasksByStatus = tasksByStatus.ToDictionary(x => x.Status, x => x.Count),
                TasksByPriority = tasksByPriority.ToDictionary(x => x.Priority, x => x.Count),
                AverageProgress = Math.Round((decimal)avgProgress, 2),
                CompletionRate = completionRate,
                PeriodStart = start,
                PeriodEnd = end
            };
        }

        /// <summary>
        /// Lấy thống kê files
        /// </summary>
        public async Task<FileStatisticsDto> GetFileStatisticsAsync(
            DateTime? startDate, DateTime? endDate, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var totalFiles = await _context.ProjectFiles.CountAsync(f => f.IsActive);
            var totalSize = await _context.ProjectFiles
                .Where(f => f.IsActive)
                .SumAsync(f => f.CurrentFileSize);

            var newFiles = await _context.ProjectFiles
                .CountAsync(f => f.CreatedAt >= start && f.CreatedAt <= end);

            var filesByType = await _context.ProjectFiles
                .Where(f => f.IsActive)
                .GroupBy(f => f.FileType)
                .Select(g => new { Type = g.Key, Count = g.Count(), Size = g.Sum(f => f.CurrentFileSize) })
                .ToListAsync();

            var checkedOutFiles = await _context.ProjectFiles
                .CountAsync(f => f.IsActive && f.CheckoutBy >= 0);

            var pendingApprovals = await _context.ProjectFiles
                .CountAsync(f => f.IsActive && f.RequireApproval && f.ApprovalStatus == "Pending");

            return new FileStatisticsDto
            {
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                AverageFileSize = totalFiles > 0 ? totalSize / totalFiles : 0,
                NewFiles = newFiles,
                CheckedOutFiles = checkedOutFiles,
                PendingApprovals = pendingApprovals,
                FilesByType = filesByType.ToDictionary(x => x.Type, x => new FileTypeStats
                {
                    Count = x.Count,
                    TotalSize = x.Size
                }),
                PeriodStart = start,
                PeriodEnd = end
            };
        }

        /// <summary>
        /// Lấy thống kê time tracking
        /// </summary>
        public async Task<AdminTimeTrackingStatsDto> GetTimeTrackingStatisticsAsync(
            DateTime? startDate, DateTime? endDate, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var timeLogs = await _context.TaskTimeLogs
                .Where(ttl => ttl.StartTime >= start && ttl.StartTime <= end)
                .ToListAsync();

            var totalMinutes = timeLogs.Sum(ttl => ttl.Duration);
            var billableMinutes = timeLogs.Where(ttl => ttl.IsBillable).Sum(ttl => ttl.Duration);
            var totalRevenue = timeLogs.Where(ttl => ttl.IsBillable && ttl.HourlyRate.HasValue)
                .Sum(ttl => (decimal)ttl.Duration / 60 * ttl.HourlyRate.Value);

            var userStats = timeLogs.GroupBy(ttl => ttl.UserId)
                .Select(g => new UserTimeStats
                {
                    UserId = g.Key,
                    TotalHours = Math.Round((decimal)g.Sum(ttl => ttl.Duration) / 60, 2),
                    BillableHours = Math.Round((decimal)g.Where(ttl => ttl.IsBillable).Sum(ttl => ttl.Duration) / 60, 2),
                    LogCount = g.Count()
                })
                .OrderByDescending(s => s.TotalHours)
                .Take(10)
                .ToList();

            return new AdminTimeTrackingStatsDto
            {
                TotalHours = Math.Round((decimal)totalMinutes / 60, 2),
                BillableHours = Math.Round((decimal)billableMinutes / 60, 2),
                NonBillableHours = Math.Round((decimal)(totalMinutes - billableMinutes) / 60, 2),
                TotalRevenue = totalRevenue,
                TotalLogs = timeLogs.Count,
                ActiveUsers = timeLogs.Select(ttl => ttl.UserId).Distinct().Count(),
                TopUsersByHours = userStats,
                PeriodStart = start,
                PeriodEnd = end
            };
        }

        /// <summary>
        /// Lấy audit logs
        /// </summary>
        public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
            AuditLogFilterRequest filter, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var query = _context.AuditLogs.AsQueryable();

            // Apply filters
            if (filter.UserId >= 0)
            {
                query = query.Where(al => al.UserId == filter.UserId);
            }

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
            {
                query = query.Where(al => al.EntityType == filter.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(filter.Action))
            {
                query = query.Where(al => al.Action.ToString() == filter.Action);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt <= filter.EndDate.Value);
            }

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplyAuditLogSorting(query, filter.SortBy, filter.SortDirection);

            // Apply pagination
            var auditLogs = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var auditLogDtos = await MapToAuditLogDtosAsync(auditLogs);

            return new PagedResult<AuditLogDto>
            {
                Items = auditLogDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        /// <summary>
        /// Lấy active sessions
        /// </summary>
        public async Task<PagedResult<ActiveSessionDto>> GetActiveSessionsAsync(
            int pageNumber, int pageSize, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var query = _context.UserSessions
                .Where(us => us.IsActive && us.ExpiresAt > DateTime.UtcNow);

            var totalCount = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(us => us.LastActivityAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var sessionDtos = await MapToActiveSessionDtosAsync(sessions);

            return new PagedResult<ActiveSessionDto>
            {
                Items = sessionDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Terminate session
        /// </summary>
        public async Task<bool> TerminateSessionAsync(int sessionId, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(us => us.Id == sessionId);

            if (session == null)
                return false;

            session.IsActive = false;
            session.LogoutAt = DateTime.UtcNow;
            session.MarkAsUpdated(currentUserId);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Lấy system health
        /// </summary>
        public async Task<SystemHealthDto> GetSystemHealthAsync(int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var dbConnected = false;
            var dbResponseTime = TimeSpan.Zero;

            try
            {
                var start = DateTime.UtcNow;
                await _context.Database.CanConnectAsync();
                dbResponseTime = DateTime.UtcNow - start;
                dbConnected = true;
            }
            catch
            {
                dbConnected = false;
            }

            var activeUsers = await _context.UserSessions
                .CountAsync(us => us.IsActive && us.LastActivityAt > DateTime.UtcNow.AddMinutes(-5));

            var totalStorageUsed = await _context.ProjectFiles
                .Where(f => f.IsActive)
                .SumAsync(f => f.CurrentFileSize);

            return new SystemHealthDto
            {
                IsHealthy = dbConnected,
                DatabaseConnected = dbConnected,
                DatabaseResponseTime = dbResponseTime,
                ActiveUsers = activeUsers,
                TotalStorageUsed = totalStorageUsed,
                CheckedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// System cleanup
        /// </summary>
        public async Task<SystemCleanupResultDto> CleanupSystemAsync(
            SystemCleanupRequest request, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var result = new SystemCleanupResultDto();
            var cutoffDate = DateTime.UtcNow.AddDays(-request.RetentionDays);

            if (request.CleanupExpiredSessions)
            {
                var expiredSessions = await _context.UserSessions
                    .Where(us => !us.IsActive || us.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();

                _context.UserSessions.RemoveRange(expiredSessions);
                result.ExpiredSessionsRemoved = expiredSessions.Count;
            }

            if (request.CleanupExpiredNotifications)
            {
                var expiredNotifications = await _context.Notifications
                    .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var notification in expiredNotifications)
                {
                    notification.SoftDelete(currentUserId, "System cleanup");
                }
                result.ExpiredNotificationsRemoved = expiredNotifications.Count;
            }

            if (request.CleanupOldAuditLogs)
            {
                var oldAuditLogs = await _context.AuditLogs
                    .Where(al => al.CreatedAt < cutoffDate)
                    .ToListAsync();

                _context.AuditLogs.RemoveRange(oldAuditLogs);
                result.OldAuditLogsRemoved = oldAuditLogs.Count;
            }

            if (request.CleanupSoftDeletedRecords)
            {
                // This would need careful implementation to avoid data loss
                // For now, just count them
                result.SoftDeletedRecordsRemoved = 0;
            }

            await _context.SaveChangesAsync();
            result.CleanupDate = DateTime.UtcNow;

            return result;
        }

        /// <summary>
        /// Get top performers
        /// </summary>
        public async Task<List<TopPerformerDto>> GetTopPerformersAsync(
            DateTime? startDate, DateTime? endDate, int limit, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            // Get user performance based on completed tasks and logged hours
            var userPerformance = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    User = u,
                    CompletedTasks = _context.ProjectTasks
                        .Count(t => t.CompletedBy == u.Id && t.CompletedAt >= start && t.CompletedAt <= end),
                    LoggedHours = _context.TaskTimeLogs
                        .Where(ttl => ttl.UserId == u.Id && ttl.StartTime >= start && ttl.StartTime <= end)
                        .Sum(ttl => (int?)ttl.Duration) ?? 0
                })
                .Where(x => x.CompletedTasks > 0 || x.LoggedHours > 0)
                .OrderByDescending(x => x.CompletedTasks)
                .ThenByDescending(x => x.LoggedHours)
                .Take(limit)
                .ToListAsync();

            return userPerformance.Select((x, index) => new TopPerformerDto
            {
                Rank = index + 1,
                UserId = x.User.Id,
                UserName = x.User.FullName,
                Department = x.User.Department.ToString(),
                CompletedTasks = x.CompletedTasks,
                TotalHours = Math.Round((decimal)x.LoggedHours / 60, 2),
                Score = x.CompletedTasks * 10 + x.LoggedHours / 60 // Simple scoring formula
            }).ToList();
        }

        /// <summary>
        /// Get project health report
        /// </summary>
        public async Task<List<ProjectHealthDto>> GetProjectHealthReportAsync(int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            var projects = await _context.Projects
                .Where(p => p.IsActive)
                .ToListAsync();

            var projectHealth = new List<ProjectHealthDto>();

            foreach (var project in projects)
            {
                var totalTasks = await _context.ProjectTasks.CountAsync(t => t.ProjectId == project.Id && t.IsActive);
                var completedTasks = await _context.ProjectTasks
                    .CountAsync(t => t.ProjectId == project.Id && t.IsActive && 
                               t.Status == TaskStatuss.Completed);

                var overdueTasks = await _context.ProjectTasks
                    .CountAsync(t => t.ProjectId == project.Id && t.IsActive && 
                               t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow &&
                               t.Status != TaskStatuss.Completed);

                var taskCompletionRate = totalTasks > 0 ? Math.Round((decimal)completedTasks / totalTasks * 100, 2) : 0;

                var healthScore = CalculateProjectHealthScore(project, taskCompletionRate, overdueTasks, totalTasks);

                projectHealth.Add(new ProjectHealthDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.ProjectName,
                    Status = project.Status.ToString(),
                    CompletionPercentage = project.CompletionPercentage,
                    TaskCompletionRate = taskCompletionRate,
                    OverdueTasks = overdueTasks,
                    TotalTasks = totalTasks,
                    IsOverdue = project.IsOverdue,
                    BudgetVariance = project.BudgetVariance,
                    HealthScore = healthScore,
                    HealthStatus = GetHealthStatus(healthScore)
                });
            }

            return projectHealth.OrderBy(p => p.HealthScore).ToList();
        }

        /// <summary>
        /// Export data
        /// </summary>
        public async Task<string> ExportDataAsync(DataExportRequest request, int currentUserId)
        {
            await CheckAdminPermissionAsync(currentUserId);

            // This is a simplified implementation
            // In production, you would implement proper CSV/Excel generation
            var fileName = $"{request.DataType}_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.Format.ToLower()}";
            var filePath = Path.Combine("exports", fileName);

            // Create exports directory if it doesn't exist
            Directory.CreateDirectory("exports");

            switch (request.DataType.ToLower())
            {
                case "users":
                    await ExportUsersAsync(filePath, request);
                    break;
                case "projects":
                    await ExportProjectsAsync(filePath, request);
                    break;
                case "tasks":
                    await ExportTasksAsync(filePath, request);
                    break;
                case "files":
                    await ExportFilesAsync(filePath, request);
                    break;
                default:
                    throw new ArgumentException($"Unsupported data type: {request.DataType}");
            }

            return $"/api/admin/exports/{fileName}"; // Return download URL
        }

        #region Private Helper Methods

        private async Task CheckAdminPermissionAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.Role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Chỉ Admin mới có thể truy cập chức năng này");
            }
        }

        private async Task<Dictionary<DateTime, int>> GetUserRegistrationTrendAsync(DateTime start, DateTime end)
        {
            var trend = new Dictionary<DateTime, int>();
            var current = start.Date;

            while (current <= end.Date)
            {
                var nextDay = current.AddDays(1);
                var count = await _context.Users
                    .CountAsync(u => u.CreatedAt >= current && u.CreatedAt < nextDay);
                
                trend[current] = count;
                current = nextDay;
            }

            return trend;
        }

        private IQueryable<AuditLog> ApplyAuditLogSorting(IQueryable<AuditLog> query, string sortBy, string sortDirection)
        {
            var ascending = sortDirection?.ToLower() != "desc";

            return sortBy?.ToLower() switch
            {
                "userid" => ascending ? query.OrderBy(al => al.UserId) : query.OrderByDescending(al => al.UserId),
                "entitytype" => ascending ? query.OrderBy(al => al.EntityType) : query.OrderByDescending(al => al.EntityType),
                "action" => ascending ? query.OrderBy(al => al.Action) : query.OrderByDescending(al => al.Action),
                _ => ascending ? query.OrderBy(al => al.CreatedAt) : query.OrderByDescending(al => al.CreatedAt),
            };
        }

        private async Task<List<AuditLogDto>> MapToAuditLogDtosAsync(List<AuditLog> auditLogs)
        {
            var userIds = auditLogs.Select(al => al.UserId).Distinct().ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            return auditLogs.Select(al =>
            {
                var user = users.FirstOrDefault(u => u.Id == al.UserId);
                return new AuditLogDto
                {
                    Id = al.Id,
                    UserId = al.UserId,
                    UserName = user?.FullName ?? "",
                    EntityType = al.EntityType,
                    EntityId = al.EntityId,
                    Action = al.Action.ToString(),
                    OldValues = al.OldValues ?? "",
                    NewValues = al.NewValues ?? "",
                    IpAddress = al.IPAddress ?? "",
                    UserAgent = al.UserAgent ?? "",
                    CreatedAt = al.CreatedAt
                };
            }).ToList();
        }

        private async Task<List<ActiveSessionDto>> MapToActiveSessionDtosAsync(List<UserSession> sessions)
        {
            var userIds = sessions.Select(s => s.UserId).Distinct().ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            return sessions.Select(s =>
            {
                var user = users.FirstOrDefault(u => u.Id == s.UserId);
                return new ActiveSessionDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = user?.FullName ?? "",
                    FullName = user?.FullName ?? "",
                    SessionToken = s.SessionToken,
                    CreatedAt = s.CreatedAt,
                    LastActivityAt = s.LastActivityAt,
                    ExpiresAt = s.ExpiresAt,
                    IpAddress = s.IPAddress ?? "",
                    UserAgent = s.UserAgent ?? "",
                    IsActive = s.IsActive
                };
            }).ToList();
        }

        private static decimal CalculateProjectHealthScore(
            ManagementFile.Models.ProjectManagement.Project project, 
            decimal taskCompletionRate, 
            int overdueTasks, 
            int totalTasks)
        {
            var score = 100m;

            // Deduct points for being overdue
            if (project.PlannedEndDate.HasValue && project.PlannedEndDate.Value < DateTime.UtcNow && 
                project.Status == ProjectStatus.Active)
            {
                score -= 30;
            }

            // Deduct points for low task completion rate
            if (taskCompletionRate < 50)
            {
                score -= 20;
            }
            else if (taskCompletionRate < 80)
            {
                score -= 10;
            }

            // Deduct points for overdue tasks
            if (totalTasks > 0)
            {
                var overduePercentage = (decimal)overdueTasks / totalTasks * 100;
                if (overduePercentage > 20)
                {
                    score -= 25;
                }
                else if (overduePercentage > 10)
                {
                    score -= 15;
                }
            }

            // Deduct points for budget variance
            var budgetVariance = project.ActualBudget - project.EstimatedBudget;
            if (budgetVariance > project.EstimatedBudget * 0.2m)
            {
                score -= 15;
            }
            else if (budgetVariance > project.EstimatedBudget * 0.1m)
            {
                score -= 10;
            }

            return Math.Max(0, score);
        }

        private static string GetHealthStatus(decimal healthScore)
        {
            return healthScore switch
            {
                >= 80 => "Excellent",
                >= 60 => "Good",
                >= 40 => "Fair",
                >= 20 => "Poor",
                _ => "Critical"
            };
        }

        private async Task ExportUsersAsync(string filePath, DataExportRequest request)
        {
            var users = await _context.Users.ToListAsync();
            
            // Simple CSV implementation
            var csv = "Id,Username,FullName,Email,Role,Department,IsActive,CreatedAt\n";
            csv += string.Join("\n", users.Select(u => 
                $"{u.Id},{u.Username},{u.FullName},{u.Email},{u.Role},{u.Department},{u.IsActive},{u.CreatedAt:yyyy-MM-dd}"));

            await File.WriteAllTextAsync(filePath, csv);
        }

        private async Task ExportProjectsAsync(string filePath, DataExportRequest request)
        {
            var projects = await _context.Projects.ToListAsync();
            
            var csv = "Id,ProjectCode,ProjectName,Status,Priority,CompletionPercentage,CreatedAt\n";
            csv += string.Join("\n", projects.Select(p => 
                $"{p.Id},{p.ProjectCode},{p.ProjectName},{p.Status},{p.Priority},{p.CompletionPercentage},{p.CreatedAt:yyyy-MM-dd}"));

            await File.WriteAllTextAsync(filePath, csv);
        }

        private async Task ExportTasksAsync(string filePath, DataExportRequest request)
        {
            var tasks = await _context.ProjectTasks.ToListAsync();
            
            var csv = "Id,ProjectId,Title,Status,Priority,Progress,CreatedAt\n";
            csv += string.Join("\n", tasks.Select(t => 
                $"{t.Id},{t.ProjectId},{t.Title},{t.Status},{t.Priority},{t.Progress},{t.CreatedAt:yyyy-MM-dd}"));

            await File.WriteAllTextAsync(filePath, csv);
        }

        private async Task ExportFilesAsync(string filePath, DataExportRequest request)
        {
            var files = await _context.ProjectFiles.ToListAsync();
            
            var csv = "Id,ProjectId,FileName,FileType,CurrentFileSize,CreatedAt\n";
            csv += string.Join("\n", files.Select(f => 
                $"{f.Id},{f.ProjectId},{f.FileName},{f.FileType},{f.CurrentFileSize},{f.CreatedAt:yyyy-MM-dd}"));

            await File.WriteAllTextAsync(filePath, csv);
        }

        #endregion
    }
}