using ManagementFile.API.Data;
using ManagementFile.Models.TimeTracking;
using ManagementFile.Models.ProjectManagement;
using ManagementFile.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs;
using ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Service xử lý business logic cho Time Tracking
    /// </summary>
    public class TimeTrackingService
    {
        private readonly ManagementFileDbContext _context;
        private readonly ILogger<TimeTrackingService> _logger;

        public TimeTrackingService(ManagementFileDbContext context, ILogger<TimeTrackingService> logger)
        {
            _context = context;
            _logger = logger;
        }



        /// <summary>
        /// Lấy danh sách time logs với filter và pagination
        /// </summary>
        public async Task<PagedResult<TimeTrackingTaskTimeLogDto>> GetTimeLogsAsync(TimeLogFilterRequest filter, int currentUserId)
        {
            var query = _context.TaskTimeLogs.AsQueryable();

            // Apply filters
            if (filter.TaskId > 0)
            {
                query = query.Where(ttl => ttl.TaskId == filter.TaskId);
            }

            if (filter.UserId > 0)
            {
                query = query.Where(ttl => ttl.UserId == filter.UserId);
            }

            if (filter.ProjectId > 0)
            {
                var projectTaskIds = await _context.ProjectTasks
                    .Where(pt => pt.ProjectId == filter.ProjectId && pt.IsActive)
                    .Select(pt => pt.Id)
                    .ToListAsync();

                query = query.Where(ttl => projectTaskIds.Contains(ttl.TaskId));
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(ttl => ttl.StartTime >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(ttl => ttl.StartTime <= filter.EndDate.Value);
            }

            if (filter.IsBillable.HasValue)
            {
                query = query.Where(ttl => ttl.IsBillable == filter.IsBillable.Value);
            }

            // Check permissions - users can only see their own logs unless admin/manager
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != UserRole.Admin)
            {
                // If specific project, check if user is member
                if (filter.ProjectId > 0)
                {
                    var isMember = await _context.ProjectMembers
                        .AnyAsync(pm => pm.ProjectId == filter.ProjectId && pm.UserId == currentUserId && pm.IsActive);

                    if (!isMember)
                    {
                        throw new UnauthorizedAccessException("Bạn không có quyền xem time logs của dự án này");
                    }

                    // Non-managers can only see own logs
                    var isManager = await _context.ProjectMembers
                        .AnyAsync(pm => pm.ProjectId == filter.ProjectId && pm.UserId == currentUserId && 
                                  pm.IsActive && pm.ProjectRole == UserRole.Manager);

                    if (!isManager)
                    {
                        query = query.Where(ttl => ttl.UserId == currentUserId);
                    }
                }
                else
                {
                    // No specific project - only own logs
                    query = query.Where(ttl => ttl.UserId == currentUserId);
                }
            }

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);

            // Apply pagination
            var timeLogs = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var timeLogDtos = await MapToTimeTrackingTaskTimeLogDtosAsync(timeLogs);

            return new PagedResult<TimeTrackingTaskTimeLogDto>
            {
                Items = timeLogDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        /// <summary>
        /// Lấy time log theo ID
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto?> GetTimeLogByIdAsync(int timeLogId, int currentUserId)
        {
            var timeLog = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(ttl => ttl.Id == timeLogId);

            if (timeLog == null)
                return null;

            await CheckTimeLogAccessAsync(timeLog, currentUserId);
            return await MapToTimeTrackingTaskTimeLogDtoAsync(timeLog);
        }

        /// <summary>
        /// Bắt đầu timer cho task
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto> StartTimerAsync(StartTimerRequest request, int currentUserId)
        {
            // Validate task exists and user has access
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.IsActive);

            if (task == null)
            {
                throw new ArgumentException("Task không tồn tại hoặc không hoạt động");
            }

            await CheckTaskAccessAsync(task, currentUserId);

            // Check if user already has running timer
            var runningTimer = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(ttl => ttl.UserId == currentUserId && ttl.EndTime == null);

            if (runningTimer != null)
            {
                throw new InvalidOperationException("Bạn đang có timer khác đang chạy. Vui lòng dừng timer hiện tại trước.");
            }

            // Get user's hourly rate from project member info
            var projectMember = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == currentUserId && pm.IsActive);

            var timeLog = new TaskTimeLog
            {
                TaskId = request.TaskId,
                UserId = currentUserId,
                StartTime = DateTime.UtcNow,
                Description = request.Description,
                IsBillable = request.IsBillable,
                HourlyRate = projectMember?.HourlyRate,
                CreatedBy = currentUserId
            };

            _context.TaskTimeLogs.Add(timeLog);
            await _context.SaveChangesAsync();

            return await MapToTimeTrackingTaskTimeLogDtoAsync(timeLog);
        }

        /// <summary>
        /// Dừng timer
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto?> StopTimerAsync(StopTimerRequest request, int currentUserId)
        {
            TaskTimeLog? timeLog;

            if (request.TimeLogId > 0)
            {
                // Stop specific timer
                timeLog = await _context.TaskTimeLogs
                    .FirstOrDefaultAsync(ttl => ttl.Id == request.TimeLogId);
            }
            else
            {
                // Stop current running timer
                timeLog = await _context.TaskTimeLogs
                    .FirstOrDefaultAsync(ttl => ttl.UserId == currentUserId && ttl.EndTime == null);
            }

            if (timeLog == null)
                return null;

            await CheckTimeLogAccessAsync(timeLog, currentUserId);

            // Stop the timer
           // timeLog.StopTimer();
            
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                timeLog.Description = request.Description;
            }

            timeLog.MarkAsUpdated(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToTimeTrackingTaskTimeLogDtoAsync(timeLog);
        }

        /// <summary>
        /// Lấy timer hiện tại đang chạy
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto?> GetCurrentTimerAsync(int currentUserId)
        {
            var runningTimer = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(ttl => ttl.UserId == currentUserId && ttl.EndTime == null);

            if (runningTimer == null)
                return null;

            return await MapToTimeTrackingTaskTimeLogDtoAsync(runningTimer);
        }

        /// <summary>
        /// Tạo time log thủ công
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto> CreateTimeLogAsync(CreateTimeLogRequest request, int currentUserId)
        {
            // Validate task exists and user has access
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.IsActive);

            if (task == null)
            {
                throw new ArgumentException("Task không tồn tại hoặc không hoạt động");
            }

            await CheckTaskAccessAsync(task, currentUserId);

            // Validate time range
            if (request.StartTime >= request.EndTime)
            {
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");
            }

            if (request.EndTime > DateTime.UtcNow)
            {
                throw new ArgumentException("Thời gian kết thúc không thể ở tương lai");
            }

            // Calculate duration
            var duration = (int)(request.EndTime - request.StartTime).TotalMinutes;

            var timeLog = new TaskTimeLog
            {
                TaskId = request.TaskId,
                UserId = currentUserId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Duration = duration,
                Description = request.Description,
                IsBillable = request.IsBillable,
                HourlyRate = request.HourlyRate,
                CreatedBy = currentUserId
            };

            _context.TaskTimeLogs.Add(timeLog);
            await _context.SaveChangesAsync();

            return await MapToTimeTrackingTaskTimeLogDtoAsync(timeLog);
        }

        /// <summary>
        /// Cập nhật time log
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto?> UpdateTimeLogAsync(
            int timeLogId, UpdateTimeLogRequest request, int currentUserId)
        {
            var timeLog = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(ttl => ttl.Id == timeLogId);

            if (timeLog == null)
                return null;

            await CheckTimeLogEditPermissionAsync(timeLog, currentUserId);

            // Validate time range
            if (request.StartTime >= request.EndTime)
            {
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");
            }

            if (request.EndTime > DateTime.UtcNow)
            {
                throw new ArgumentException("Thời gian kết thúc không thể ở tương lai");
            }

            // Update properties
            timeLog.StartTime = request.StartTime;
            timeLog.EndTime = request.EndTime;
            timeLog.Duration = (int)(request.EndTime - request.StartTime).TotalMinutes;
            timeLog.Description = request.Description;
            timeLog.IsBillable = request.IsBillable;
            timeLog.HourlyRate = request.HourlyRate;
            timeLog.MarkAsUpdated(currentUserId);

            await _context.SaveChangesAsync();
            return await MapToTimeTrackingTaskTimeLogDtoAsync(timeLog);
        }

        /// <summary>
        /// Xóa time log
        /// </summary>
        public async Task<bool> DeleteTimeLogAsync(int timeLogId, int currentUserId)
        {
            var timeLog = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(ttl => ttl.Id == timeLogId);

            if (timeLog == null)
                return false;

            await CheckTimeLogEditPermissionAsync(timeLog, currentUserId);

            _context.TaskTimeLogs.Remove(timeLog);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Lấy báo cáo tổng hợp time tracking
        /// </summary>
        public async Task<TimeTrackingSummaryDto> GetTimeTrackingSummaryAsync(
            int userId, int projectId, DateTime? startDate, DateTime? endDate, int currentUserId)
        {
            var query = _context.TaskTimeLogs.AsQueryable();

            // Apply filters
            if (userId > 0)
            {
                query = query.Where(ttl => ttl.UserId == userId);
            }

            if (projectId > 0)
            {
                await CheckProjectAccessAsync(projectId, currentUserId);
                
                var projectTaskIds = await _context.ProjectTasks
                    .Where(pt => pt.ProjectId == projectId && pt.IsActive)
                    .Select(pt => pt.Id)
                    .ToListAsync();

                query = query.Where(ttl => projectTaskIds.Contains(ttl.TaskId));
            }

            if (startDate.HasValue)
            {
                query = query.Where(ttl => ttl.StartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(ttl => ttl.StartTime <= endDate.Value);
            }

            // Check permissions
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != UserRole.Admin)
            {
                if (projectId > 0)
                {
                    var isManager = await _context.ProjectMembers
                        .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId && 
                                  pm.IsActive && pm.ProjectRole == UserRole.Manager);

                    if (!isManager)
                    {
                        query = query.Where(ttl => ttl.UserId == currentUserId);
                    }
                }
                else
                {
                    query = query.Where(ttl => ttl.UserId == currentUserId);
                }
            }

            var timeLogs = await query.ToListAsync();

            var totalMinutes = timeLogs.Sum(ttl => ttl.Duration);
            var billableMinutes = timeLogs.Where(ttl => ttl.IsBillable).Sum(ttl => ttl.Duration);
            var totalRevenue = timeLogs.Where(ttl => ttl.IsBillable && ttl.HourlyRate.HasValue)
                .Sum(ttl => (decimal)ttl.Duration / 60 * ttl.HourlyRate.Value);

            return new TimeTrackingSummaryDto
            {
                TotalHours = Math.Round((decimal)totalMinutes / 60, 2),
                BillableHours = Math.Round((decimal)billableMinutes / 60, 2),
                NonBillableHours = Math.Round((decimal)(totalMinutes - billableMinutes) / 60, 2),
                TotalRevenue = totalRevenue,
                TotalLogs = timeLogs.Count,
                AverageHoursPerDay = timeLogs.Any() ? 
                    Math.Round((decimal)totalMinutes / 60 / Math.Max(1, (endDate?.Date ?? DateTime.Today).Subtract(startDate?.Date ?? DateTime.Today.AddDays(-30)).Days), 2) : 0
            };
        }

        /// <summary>
        /// Lấy báo cáo thời gian theo user
        /// </summary>
        public async Task<UserTimeReportDto> GetUserTimeReportAsync(
            int userId, DateTime? startDate, DateTime? endDate, int currentUserId)
        {
            // Check permissions
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != UserRole.Admin && currentUserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem báo cáo thời gian của user này");
            }

            var userInfo = await _context.Users.FindAsync(userId);
            if (userInfo == null)
            {
                throw new ArgumentException("User không tồn tại");
            }

            var query = _context.TaskTimeLogs.Where(ttl => ttl.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(ttl => ttl.StartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(ttl => ttl.StartTime <= endDate.Value);
            }

            var timeLogs = await query.Include(ttl => ttl.ProjectTask).ToListAsync();

            var projectSummaries = timeLogs
                .GroupBy(ttl => ttl.ProjectTask.ProjectId)
                .Select(g => new UserTimeReportProjectSummary
                {
                    ProjectId = g.Key,
                    ProjectName = g.First().ProjectTask.Project?.ProjectName ?? "",
                    TotalHours = Math.Round((decimal)g.Sum(ttl => ttl.Duration) / 60, 2),
                    BillableHours = Math.Round((decimal)g.Where(ttl => ttl.IsBillable).Sum(ttl => ttl.Duration) / 60, 2),
                    LogCount = g.Count()
                })
                .ToList();

            var totalMinutes = timeLogs.Sum(ttl => ttl.Duration);
            var billableMinutes = timeLogs.Where(ttl => ttl.IsBillable).Sum(ttl => ttl.Duration);

            return new UserTimeReportDto
            {
                UserId = userId,
                UserName = userInfo.FullName,
                //ReportStartDate = startDate ?? timeLogs.Min(ttl => ttl.StartTime),
                //ReportEndDate = endDate ?? timeLogs.Max(ttl => ttl.StartTime),
                TotalHours = Math.Round((decimal)totalMinutes / 60, 2),
                BillableHours = Math.Round((decimal)billableMinutes / 60, 2),
                NonBillableHours = Math.Round((decimal)(totalMinutes - billableMinutes) / 60, 2),
                TotalLogs = timeLogs.Count,
                ProjectSummaries = projectSummaries
            };
        }

        /// <summary>
        /// Lấy báo cáo thời gian theo project
        /// </summary>
        public async Task<ProjectTimeReportDto> GetProjectTimeReportAsync(
            int projectId, DateTime? startDate, DateTime? endDate, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                throw new ArgumentException("Project không tồn tại");
            }

            var projectTaskIds = await _context.ProjectTasks
                .Where(pt => pt.ProjectId == projectId && pt.IsActive)
                .Select(pt => pt.Id)
                .ToListAsync();

            var query = _context.TaskTimeLogs.Where(ttl => projectTaskIds.Contains(ttl.TaskId));

            if (startDate.HasValue)
            {
                query = query.Where(ttl => ttl.StartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(ttl => ttl.StartTime <= endDate.Value);
            }

            var timeLogs = await query
                .Include(ttl => ttl.ProjectTask)
                .ToListAsync();

            var userSummaries = timeLogs
                .GroupBy(ttl => ttl.UserId)
                .Select(g => new ProjectTimeReportUserSummary
                {
                    UserId = g.Key,
                    UserName = "", // Will be filled later
                    TotalHours = Math.Round((decimal)g.Sum(ttl => ttl.Duration) / 60, 2),
                    BillableHours = Math.Round((decimal)g.Where(ttl => ttl.IsBillable).Sum(ttl => ttl.Duration) / 60, 2),
                    LogCount = g.Count()
                })
                .ToList();

            // Fill user names
            var userIds = userSummaries.Select(us => us.UserId).ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            
            foreach (var userSummary in userSummaries)
            {
                var user = users.FirstOrDefault(u => u.Id == userSummary.UserId);
                userSummary.UserName = user?.FullName ?? "";
            }

            var totalMinutes = timeLogs.Sum(ttl => ttl.Duration);
            var billableMinutes = timeLogs.Where(ttl => ttl.IsBillable).Sum(ttl => ttl.Duration);

            return new ProjectTimeReportDto
            {
                ProjectId = projectId,
                ProjectName = project.ProjectName,
                //ReportStartDate = startDate ?? timeLogs.Min(ttl => ttl.StartTime).Date,
                //ReportEndDate = endDate ?? timeLogs.Max(ttl => ttl.StartTime).Date,
                TotalHours = Math.Round((decimal)totalMinutes / 60, 2),
                BillableHours = Math.Round((decimal)billableMinutes / 60, 2),
                NonBillableHours = Math.Round((decimal)(totalMinutes - billableMinutes) / 60, 2),
                TotalLogs = timeLogs.Count,
                EstimatedHours = project.EstimatedHours,
                ActualHours = project.ActualHours,
                UserSummaries = userSummaries
            };
        }

        /// <summary>
        /// Lấy weekly timesheet
        /// </summary>
        public async Task<WeeklyTimesheetDto> GetWeeklyTimesheetAsync(
            GetWeeklyTimesheetRequest request,
            int currentUserId)
        {
            var userId = request.UserId ?? currentUserId;

            // Check permissions
            if (userId != currentUserId)
            {
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser?.Role != Contracts.Enums.UserRole.Admin)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xem timesheet của user khác");
                }
            }

            var weekStart = GetMondayOfWeek(request.WeekStartDate);
            var weekEnd = weekStart.AddDays(7);

            // Get all time logs for the week
            var query = _context.TaskTimeLogs
                .Include(tl => tl.ProjectTask)
                .ThenInclude(pt => pt.Project)
                .Where(tl => tl.UserId == userId &&
                            tl.StartTime.HasValue &&
                            tl.StartTime.Value >= weekStart &&
                            tl.StartTime.Value < weekEnd);

            if (request.ProjectId.HasValue)
            {
                var projectTaskIds = await _context.ProjectTasks
                    .Where(pt => pt.ProjectId == request.ProjectId.Value && pt.IsActive)
                    .Select(pt => pt.Id)
                    .ToListAsync();

                query = query.Where(tl => projectTaskIds.Contains(tl.TaskId));
            }

            var timeLogs = await query.ToListAsync();

            // Group by task
            var taskGroups = timeLogs
                .GroupBy(tl => tl.TaskId)
                .Select(g => new WeeklyTimesheetTaskDto
                {
                    TaskId = g.Key,
                    TaskTitle = g.First().ProjectTask.Title,
                    TaskCode = g.First().ProjectTask.TaskCode,
                    ProjectId = g.First().ProjectTask.ProjectId,
                    ProjectName = g.First().ProjectTask.Project?.ProjectName ?? "",
                    HourlyRate = g.First().HourlyRate,

                    // Fill daily hours (0=Monday, 6=Sunday)
                    DailyEntries = Enumerable.Range(0, 7)
                        .Select(dayIndex =>
                        {
                            var date = weekStart.AddDays(dayIndex);
                            var dayLogs = g.Where(tl => tl.StartTime.HasValue &&
                                                       tl.StartTime.Value.Date == date.Date)
                                          .ToList();

                            var totalMinutes = dayLogs.Sum(dl => dl.Duration);
                            var notes = string.Join("; ", dayLogs.Select(dl => dl.Description).Where(d => !string.IsNullOrWhiteSpace(d)));

                            return new WeeklyDailyEntryDto
                            {
                                DayIndex = dayIndex,
                                Date = date,
                                Hours = totalMinutes / 60m,
                                Note = notes,
                                LogIds = dayLogs.Select(dl => dl.Id).ToList()
                            };
                        })
                        .ToList()
                })
                .ToList();

            return new WeeklyTimesheetDto
            {
                UserId = userId,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd.AddDays(-1),
                TaskEntries = taskGroups,
                TotalHours = taskGroups.SelectMany(t => t.DailyEntries).Sum(d => d.Hours),
            };
        }

        /// <summary>
        /// Lưu weekly timesheet (batch save)
        /// </summary>
        public async Task<WeeklyTimesheetDto> SaveWeeklyTimesheetAsync(
            SaveWeeklyTimesheetRequest request,
            int currentUserId)
        {
            var weekStart = GetMondayOfWeek(request.WeekStartDate);
            var weekEnd = weekStart.AddDays(7);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ✅ Track tasks và projects cần update
                var tasksToUpdate = new HashSet<int>();
                var projectsToUpdate = new HashSet<int>();

                // Get existing logs for this week
                var existingLogs = await _context.TaskTimeLogs
                    .Where(tl => tl.UserId == currentUserId &&
                                tl.StartTime.HasValue &&
                                tl.StartTime.Value >= weekStart &&
                                tl.StartTime.Value < weekEnd)
                    .ToListAsync();

                // Delete existing logs that are not in the new request
                var requestTaskIds = request.Entries.Select(e => e.TaskId).ToHashSet();
                var logsToDelete = existingLogs.Where(el => !requestTaskIds.Contains(el.TaskId)).ToList();

                if (logsToDelete.Any())
                {
                    // ✅ Track deleted tasks
                    foreach (var log in logsToDelete)
                    {
                        tasksToUpdate.Add(log.TaskId);
                    }

                    _context.TaskTimeLogs.RemoveRange(logsToDelete);
                    _logger.LogDebug("Deleted {Count} obsolete time logs", logsToDelete.Count);
                }

                // Process each task entry
                foreach (var entry in request.Entries)
                {
                    // ✅ Track this task
                    tasksToUpdate.Add(entry.TaskId);

                    // Validate task access
                    var task = await _context.ProjectTasks
                        .FirstOrDefaultAsync(t => t.Id == entry.TaskId && t.IsActive);

                    if (task == null)
                    {
                        throw new ArgumentException($"Task {entry.TaskId} không tồn tại hoặc không hoạt động");
                    }

                    if(projectsToUpdate.Contains(task.ProjectId) == false)
                    {
                        projectsToUpdate.Add(task.ProjectId);
                    }

                    await CheckTaskAccessAsync(task, currentUserId);

                    // Process each daily entry
                    foreach (var dailyEntry in entry.DailyEntries)
                    {
                        if (dailyEntry.Hours < 0)
                            continue;

                        var date = weekStart.AddDays(dailyEntry.DayIndex).Date;

                        // Find existing log for this task on this date
                        var existingLog = existingLogs.FirstOrDefault(el =>
                            el.TaskId == entry.TaskId &&
                            el.StartTime.HasValue &&
                            el.StartTime.Value.Date == date);

                        if (existingLog != null)
                        {
                            // Update existing log
                            existingLog.Duration = (int)(dailyEntry.Hours * 60);
                            existingLog.Description = dailyEntry.Note;
                            existingLog.IsBillable = true;
                            existingLog.HourlyRate = entry.HourlyRate;
                            existingLog.MarkAsUpdated(currentUserId);
                        }
                        else if (dailyEntry.Hours > 0)
                        {
                            // Create new log
                            var newLog = new TaskTimeLog
                            {
                                TaskId = entry.TaskId,
                                UserId = currentUserId,
                                StartTime = date.AddHours(9), // Default 9 AM
                                EndTime = date.AddHours(9).AddMinutes((int)(dailyEntry.Hours * 60)),
                                Duration = (int)(dailyEntry.Hours * 60),
                                Description = dailyEntry.Note,
                                IsBillable = true,
                                HourlyRate = entry.HourlyRate,
                                CreatedBy = currentUserId
                            };

                            _context.TaskTimeLogs.Add(newLog);
                        }
                    }

                    
                }

                // ✅ Save time logs trước
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Saved weekly timesheet.  User: {UserId}, Week: {WeekStart}, Tasks: {TaskCount}",
                    currentUserId, weekStart, tasksToUpdate.Count);

                // ✅ Update ActualHours cho tasks
                await UpdateTaskActualHoursAsync(tasksToUpdate, currentUserId);

                // ✅ Save projectTask trước
                await _context.SaveChangesAsync();

                // ✅ Update ActualHours cho projects
                foreach (var projectId in projectsToUpdate)
                {
                    await UpdateProjectActualHoursAsync(projectId, currentUserId);
                }

                // ✅ Save tất cả updates
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Weekly timesheet saved successfully. Updated {TaskCount} tasks, {ProjectCount} projects",
                    tasksToUpdate.Count, projectsToUpdate.Count);

                // Return updated timesheet
                return await GetWeeklyTimesheetAsync(
                    new GetWeeklyTimesheetRequest { WeekStartDate = weekStart },
                    currentUserId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex,
            "Error saving weekly timesheet. User: {UserId}, Week: {WeekStart}",
            currentUserId, weekStart);

                throw;
            }
        }

        /// <summary>
        /// Copy timesheet từ tuần khác
        /// </summary>
        public async Task<WeeklyTimesheetDto> CopyWeeklyTimesheetAsync(
            CopyWeekTimesheetRequest request,
            int currentUserId)
        {
            var sourceWeekStart = GetMondayOfWeek(request.SourceWeekStartDate);
            var targetWeekStart = GetMondayOfWeek(request.TargetWeekStartDate);

            // Get source week data
            var sourceTimesheet = await GetWeeklyTimesheetAsync(
                new GetWeeklyTimesheetRequest { WeekStartDate = sourceWeekStart },
                currentUserId);

            if (!sourceTimesheet.TaskEntries.Any())
            {
                throw new ArgumentException("Tuần nguồn không có dữ liệu để copy");
            }

            // Create save request from source data
            var saveRequest = new SaveWeeklyTimesheetRequest
            {
                WeekStartDate = targetWeekStart,
                SubmitForApproval = false,
                Entries = sourceTimesheet.TaskEntries.Select(te => new WeeklyTimesheetEntryRequest
                {
                    TaskId = te.TaskId,
                    HourlyRate = te.HourlyRate,
                    DailyEntries = te.DailyEntries
                        .Where(de => de.Hours > 0)
                        .Select(de => new DailyTimeEntryRequest
                        {
                            DayIndex = de.DayIndex,
                            Hours = de.Hours,
                            Note = request.IncludeNotes ? de.Note : ""
                        })
                        .ToList()
                })
                .ToList()
            };

            return await SaveWeeklyTimesheetAsync(saveRequest, currentUserId);
        }

        /// <summary>
        /// Lấy danh sách tasks available cho weekly timesheet
        /// </summary>
        public async Task<List<TaskForTimesheetDto>> GetAvailableTasksForTimesheetAsync(
            int currentUserId,
            int? projectId = null)
        {
            var query = _context.ProjectTasks
                .Include(pt => pt.Project)
                .Where(pt => pt.IsActive);

            // Filter by projects user is member of
            var userProjectIds = await _context.ProjectMembers
                .Where(pm => pm.UserId == currentUserId && pm.IsActive)
                .Select(pm => pm.ProjectId)
                .ToListAsync();

            query = query.Where(pt => userProjectIds.Contains(pt.ProjectId));

            if (projectId.HasValue)
            {
                query = query.Where(pt => pt.ProjectId == projectId.Value);
            }

            var tasks = await query
                .OrderBy(pt => pt.Project.ProjectName)
                .ThenBy(pt => pt.Title)
                .ToListAsync();

            return tasks.Select(t => new TaskForTimesheetDto
            {
                TaskId = t.Id,
                ParentTaskId = t.ParentTaskId,
                TaskTitle = t.Title,
                TaskCode = t.TaskCode,
                ProjectId = t.ProjectId,
                ProjectName = t.Project?.ProjectName ?? "",
                DefaultHourlyRate = GetDefaultHourlyRateForUser(currentUserId, t.ProjectId)
            })
            .ToList();
        }

        /// <summary>
        /// Validate weekly timesheet trước khi submit
        /// </summary>
        public async Task<WeeklyTimesheetValidationResult> ValidateWeeklyTimesheetAsync(
            SaveWeeklyTimesheetRequest request,
            int currentUserId)
        {
            var result = new WeeklyTimesheetValidationResult
            {
                IsValid = true,
                Warnings = new List<string>(),
                Errors = new List<string>()
            };

            var weekStart = GetMondayOfWeek(request.WeekStartDate);

            // Calculate daily totals
            var dailyTotals = new decimal[7];

            foreach (var entry in request.Entries)
            {
                foreach (var dailyEntry in entry.DailyEntries)
                {
                    if (dailyEntry.DayIndex >= 0 && dailyEntry.DayIndex < 7)
                    {
                        dailyTotals[dailyEntry.DayIndex] += dailyEntry.Hours;
                    }
                }
            }

            // Validate daily totals
            for (int i = 0; i < 7; i++)
            {
                var date = weekStart.AddDays(i);
                var total = dailyTotals[i];

                if (total > 24)
                {
                    result.Errors.Add($"Ngày {date:dd/MM/yyyy}: Tổng giờ ({total}h) vượt quá 24h");
                    result.IsValid = false;
                }
                else if (total > 12)
                {
                    result.Warnings.Add($"Ngày {date:dd/MM/yyyy}: Overtime cao ({total}h)");
                }
                else if (total > 8)
                {
                    result.Warnings.Add($"Ngày {date:dd/MM/yyyy}: Overtime ({total}h)");
                }
            }

            // Validate week total
            var weekTotal = dailyTotals.Sum();

            if (weekTotal > 60)
            {
                result.Warnings.Add($"Tổng giờ tuần ({weekTotal}h) vượt quá 60h");
            }
            else if (weekTotal < 20 && weekTotal > 0)
            {
                result.Warnings.Add($"Tổng giờ tuần ({weekTotal}h) thấp hơn mức chuẩn");
            }

            // Check weekend work
            if (dailyTotals[5] > 0 || dailyTotals[6] > 0)
            {
                result.Warnings.Add("Có làm việc cuối tuần");
            }

            // Check future dates
            var today = DateTime.Today;
            if (weekStart > today)
            {
                result.Errors.Add("Không thể nhập giờ cho tuần trong tương lai");
                result.IsValid = false;
            }

            return result;
        }

        #region ActualHours Update Methods

        /// <summary>
        /// Update ActualHours cho tasks sau khi save timesheet
        /// </summary>
        private async Task UpdateTaskActualHoursAsync(HashSet<int> taskIds, int currentUserId)
        {
            if (taskIds == null || !taskIds.Any())
                return;

            _logger.LogInformation("Updating ActualHours for {TaskCount} tasks", taskIds.Count);

            foreach (var taskId in taskIds)
            {
                try
                {
                    var task = await _context.ProjectTasks.FindAsync(taskId);
                    if (task == null)
                    {
                        _logger.LogWarning("Task {TaskId} not found for ActualHours update", taskId);
                        continue;
                    }

                    // ✅ Tính tổng giờ từ TẤT CẢ time logs của task này, tísnh abwnfg phút
                    var totalMinutes = await _context.TaskTimeLogs
                        .Where(tl => tl.TaskId == taskId)
                        .SumAsync(tl => (int?)tl.Duration) ?? 0;

                    // ✅ Update ActualHours
                    var oldActualHours = task.ActualHours;
                    task.ActualHours = totalMinutes;
                    task.MarkAsUpdated(currentUserId);

                    _logger.LogDebug(
                        "Task {TaskId} ActualHours updated: {OldHours}h → {NewHours}h (Estimated: {EstimatedHours}h, Progress: {Progress}%)",
                        taskId, oldActualHours, task.ActualHours, task.EstimatedHours, task.Progress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating ActualHours for task {TaskId}", taskId);
                    // Continue với tasks khác, không throw
                }
            }
        }

        /// <summary>
        /// Update ActualHours cho project (tổng từ tất cả tasks)
        /// </summary>
        private async Task UpdateProjectActualHoursAsync(int projectId, int currentUserId)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found for ActualHours update", projectId);
                    return;
                }

                // ✅ Tính tổng ActualHours từ tất cả active tasks trong project, tính bằng phút
                var totalActualHours = await _context.ProjectTasks
                    .Where(t => t.ProjectId == projectId && t.IsActive)
                    .SumAsync(t => (int?)t.ActualHours) ?? 0;

                var oldActualHours = project.ActualHours;
                project.ActualHours = totalActualHours;


                _logger.LogInformation(
                    "Project {ProjectId} ActualHours updated: {OldHours}h → {NewHours}h (Estimated: {EstimatedHours}h)",
                    projectId, oldActualHours, project.ActualHours, project.EstimatedHours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ActualHours for project {ProjectId}", projectId);
            }
        }

        #endregion

        #region Helper Methods

        private DateTime GetMondayOfWeek(DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7

            return date.Date.AddDays(-(dayOfWeek - 1));
        }

        private decimal GetDefaultHourlyRateForUser(int userId, int projectId)
        {
            var projectMember = _context.ProjectMembers
                .FirstOrDefault(pm => pm.UserId == userId &&
                                     pm.ProjectId == projectId &&
                                     pm.IsActive);

            return projectMember?.HourlyRate ?? 50000; // Default rate
        }

        #endregion

        #region Private Helper Methods

        private IQueryable<TaskTimeLog> ApplySorting(IQueryable<TaskTimeLog> query, string sortBy, string sortDirection)
        {
            var ascending = sortDirection?.ToLower() != "desc";

            return sortBy?.ToLower() switch
            {
                "userid" => ascending ? query.OrderBy(ttl => ttl.UserId) : query.OrderByDescending(ttl => ttl.UserId),
                "taskid" => ascending ? query.OrderBy(ttl => ttl.TaskId) : query.OrderByDescending(ttl => ttl.TaskId),
                "duration" => ascending ? query.OrderBy(ttl => ttl.Duration) : query.OrderByDescending(ttl => ttl.Duration),
                "isbillable" => ascending ? query.OrderBy(ttl => ttl.IsBillable) : query.OrderByDescending(ttl => ttl.IsBillable),
                "endtime" => ascending ? query.OrderBy(ttl => ttl.EndTime) : query.OrderByDescending(ttl => ttl.EndTime),
                _ => ascending ? query.OrderBy(ttl => ttl.StartTime) : query.OrderByDescending(ttl => ttl.StartTime),
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

        private async Task CheckTaskAccessAsync(ProjectTask task, int currentUserId)
        {
            await CheckProjectAccessAsync(task.ProjectId, currentUserId);

            // Additional task-specific checks can be added here
        }

        private async Task CheckTimeLogAccessAsync(TaskTimeLog timeLog, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            // Check if it's user's own log
            if (timeLog.UserId == currentUserId)
                return;

            // Check if user is project manager
            var task = await _context.ProjectTasks.FindAsync(timeLog.TaskId);
            if (task != null)
            {
                var project = await _context.Projects.FindAsync(task.ProjectId);
                if (project?.ProjectManagerId == currentUserId)
                    return;

                var isProjectManager = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == currentUserId && 
                              pm.IsActive && pm.ProjectRole == UserRole.Manager);

                if (isProjectManager)
                    return;
            }

            throw new UnauthorizedAccessException("Bạn không có quyền truy cập time log này");
        }

        private async Task CheckTimeLogEditPermissionAsync(TaskTimeLog timeLog, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            // Only owner can edit their own logs
            if (timeLog.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ có thể chỉnh sửa time log của mình");
            }
        }

        private async Task<TimeTrackingTaskTimeLogDto> MapToTimeTrackingTaskTimeLogDtoAsync(TaskTimeLog timeLog)
        {
            var task = await _context.ProjectTasks.FindAsync(timeLog.TaskId);
            var user = await _context.Users.FindAsync(timeLog.UserId);

            return new TimeTrackingTaskTimeLogDto
            {
                Id = timeLog.Id,
                TaskId = timeLog.TaskId,
                TaskTitle = task?.Title ?? "",
                TaskCode = task?.TaskCode ?? "",
                ProjectId = task.ProjectId,
                UserId = timeLog.UserId,
                UserName = user?.FullName ?? "",
                StartTime = timeLog.StartTime,
                EndTime = timeLog.EndTime,
                Duration = timeLog.Duration,
                Description = timeLog.Description,
                IsBillable = timeLog.IsBillable,
                HourlyRate = timeLog.HourlyRate,
                CreatedAt = timeLog.CreatedAt,
                CreatedBy = timeLog.CreatedBy,
                UpdatedAt = timeLog.UpdatedAt,
                UpdatedBy = timeLog.UpdatedBy,
                IsRunning = !timeLog.EndTime.HasValue
            };
        }

        private async Task<List<TimeTrackingTaskTimeLogDto>> MapToTimeTrackingTaskTimeLogDtosAsync(List<TaskTimeLog> timeLogs)
        {
            var result = new List<TimeTrackingTaskTimeLogDto>();
            foreach (var timeLog in timeLogs)
            {
                result.Add(await MapToTimeTrackingTaskTimeLogDtoAsync(timeLog));
            }
            return result;
        }

        #endregion
    }
 
}