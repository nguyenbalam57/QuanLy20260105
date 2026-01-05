using ManagementFile.API.Data;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks;
using ManagementFile.Contracts.Requests.ProjectManagement.TaskComments;
using ManagementFile.Models.NotificationsAndCommunications;
using ManagementFile.Models.ProjectManagement;
using ManagementFile.Models.TimeTracking;
using ManagementFile.Models.UserManagement;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using static ManagementFile.API.Controllers.ProjectTasksController;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Service xử lý business logic cho Project Tasks
    /// </summary>
    public class ProjectTaskService
    {
        private readonly ManagementFileDbContext _context;
        private readonly ILogger<ProjectTaskService> _logger;

        public ProjectTaskService(ManagementFileDbContext context, ILogger<ProjectTaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Project Task Methods

        /// <summary>
        /// Lấy danh sách tasks với filter và pagination
        /// </summary>
        public async Task<PagedResult<ProjectTaskDto>> GetTasksAsync(TaskFilterRequest filter, int currentUserId)
        {
            var query = _context.ProjectTasks
                .Where(t => t.ProjectId == filter.ProjectId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(t => t.Title.Contains(filter.SearchTerm) ||
                                        t.Description.Contains(filter.SearchTerm) ||
                                        t.TaskCode.Contains(filter.SearchTerm));
            }

            if (filter.ReporterId.HasValue && filter.ReporterId.Value > 0)
            {
                query = query.Where(t => t.ReporterId == filter.ReporterId.Value);
            }

            if (filter.AssignedToId.HasValue && filter.AssignedToId.Value > 0)
            {
                query = query.Where(t => t.AssignedToId == filter.AssignedToId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(t => t.Status == filter.Status.Value);
            }

            if (filter.Priority.HasValue)
            {
                query = query.Where(t => t.Priority == filter.Priority.Value);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == filter.IsActive.Value);
            }

            if (filter.ParentTaskId.HasValue && filter.ParentTaskId > 0)
            {
                query = query.Where(t => t.ParentTaskId == filter.ParentTaskId);
            }
            else
            {
                query = query.Where(t => t.ParentTaskId == null);
            }

            // Check user permissions
            await CheckProjectAccessAsync(filter.ProjectId, currentUserId);

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);

            // Apply pagination
            var tasks = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var taskDtos = await MapToProjectTaskDtosAsync(tasks);


            return new PagedResult<ProjectTaskDto>
            {
                Items = taskDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        /// <summary>
        /// Lấy task theo ID
        /// </summary>
        public async Task<ProjectTaskDto?> GetTaskByIdAsync(int projectId, int taskId, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return null;

            await CheckProjectAccessAsync(projectId, currentUserId);

            var taskDto = await MapToProjectTaskDtoAsync(task);

            return taskDto;
        }

        /// <summary>
        /// Tạo task mới
        /// </summary>
        public async Task<ProjectTaskDto> CreateTaskAsync(int projectId, CreateTaskRequest request, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            // Validate parent task if specified
            if (request.ParentTaskId.HasValue && request.ParentTaskId.Value > 0)
            {
                var parentTask = await _context.ProjectTasks
                    .FirstOrDefaultAsync(t => t.Id == request.ParentTaskId.Value && t.ProjectId == projectId);

                if (parentTask == null)
                {
                    throw new ArgumentException("Parent task không tồn tại");
                }
            }

            // Validate assigned user if specified
            if (request.AssignedToId.HasValue && request.AssignedToId.Value > 0)
            {
                var isUserInProject = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == request.AssignedToId.Value && pm.IsActive);

                if (!isUserInProject)
                {
                    throw new ArgumentException("User được assign không phải thành viên của dự án");
                }
            }

            // Generate task code
            var taskCount = await _context.ProjectTasks
                .CountAsync(t => t.ProjectId == projectId);
            var taskCode = $"T-{taskCount + 1:D3}";

            var task = new ProjectTask
            {
                ProjectId = projectId,
                ParentTaskId = request.ParentTaskId, 
                TaskCode = taskCode,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                TaskType = request.TaskType,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                EstimatedHours = (int)(request.EstimatedHours * 60),
                AssignedToId = request.AssignedToId, 
                ReporterId = request.ReporterId ?? currentUserId,
                IsActive = true,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            // Set tags if provided
            if (request.Tags?.Count > 0)
            {
                task.SetTags(request.Tags);
            }

            if (request.Metadata?.Count > 0)
            {
                task.SetMetadata(request.Metadata);
            }

            if (request.AssignedToIds?.Count > 0)
            {
                task.SetAssignedToIds(request.AssignedToIds);
            }

            _context.ProjectTasks.Add(task);

            // Gưi thông báo đến cho người được giao việc
            if (task.AssignedToId.HasValue && task.AssignedToId > 0)
            {
                // Tạo gửi thông báo
                var notification = new Notification
                {
                    UserId = task.AssignedToId ?? 0,
                    Title = $"Thông báo ID Task : {task.TaskCode}",
                    Content = $"{task.Title}",
                    Type = NotificationType.TaskAssigned,
                    RelatedEntityType = EntityTypeManager.ProjectTask,
                    RelatedEntityId = task.Id,
                    ExpiresAt = task.DueDate ?? null,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Notifications.Add(notification);
            }

            if (task.GetAssignedToIds().Count > 0)
            {
                foreach (var id in task.GetAssignedToIds())
                {
                    if (id == task.AssignedToId)
                        continue;

                    var notification = new Notification
                    {
                        UserId = id,
                        Title = $"Thông báo ID Task : {task.TaskCode}",
                        Content = $"{task.Title}",
                        Type = NotificationType.TaskAssigned,
                        RelatedEntityType = EntityTypeManager.ProjectTask,
                        RelatedEntityId = task.Id,
                        ExpiresAt = task.DueDate ?? null,
                        CreatedBy = currentUserId,
                        CreatedAt = DateTime.UtcNow,
                    };

                    _context.Notifications.Add(notification);
                }
            }

            if (task.ReporterId.HasValue && task.ReporterId > 0 && task.ReporterId != currentUserId)
            {
                // Tạo gửi thông báo
                var notification = new Notification
                {
                    UserId = task.ReporterId ?? 0,
                    Title = $"Thông báo ID Task : {task.TaskCode}",
                    Content = $"{task.Title}",
                    Type = NotificationType.TaskReporter,
                    RelatedEntityType = EntityTypeManager.ProjectTask,
                    RelatedEntityId = task.Id,
                    ExpiresAt = task.DueDate ?? null,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Notifications.Add(notification);
            }


            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        /// <summary>
        /// Cập nhật task
        /// </summary>
        public async Task<ProjectTaskDto?> UpdateTaskAsync(
            int projectId, int taskId, UpdateTaskRequest request, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);


            if (task == null)
                return null;

            //Kiểm tra version để tránh ghi đè
            if (task.Version != request.Version)
            {
                throw new ArgumentException("Task đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            await CheckTaskEditPermissionAsync(task, currentUserId);

            // Validate assigned user if specified
            if (request.AssignedToId > 0)
            {
                var isUserInProject = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == request.AssignedToId && pm.IsActive);

                if (!isUserInProject)
                {
                    throw new ArgumentException("User được assign không phải thành viên của dự án");
                }
            }

            // Update basic info
            task.Title = request.Title;
            task.Description = request.Description;
            task.Priority = request.Priority;
            task.AssignedToId = request.AssignedToId > 0 ? request.AssignedToId : null;
            task.ReporterId = request.ReporterId > 0 ? request.ReporterId : null;
            task.EstimatedHours = (int)(request.EstimatedHours * 60);
            task.StartDate = request.StartDate;
            task.DueDate = request.DueDate;
            task.TaskType = request.TaskType;
            task.Status = request.Status;
            task.Progress = request.Progress;


            // Update tags
            if (request.Tags != null)
            {
                task.SetTags(request.Tags);
            }

            // Update metadata
            if (request.Metadata != null)
            {
                task.SetMetadata(request.Metadata);
            }

            if (request.AssignedToIds?.Count > 0)
            {
                task.SetAssignedToIds(request.AssignedToIds);
            }

            task.MarkAsUpdated(currentUserId);

            // Gưi thông báo đến cho người được giao việc
            if (task.AssignedToId.HasValue && task.AssignedToId > 0)
            {
                // Tạo gửi thông báo
                var notification = new Notification
                {
                    UserId = task.AssignedToId ?? 0,
                    Title = $"Thông báo ID Task : {task.TaskCode}",
                    Content = $"{task.Title}",
                    Type = NotificationType.TaskUpdated,
                    RelatedEntityType = EntityTypeManager.ProjectTask,
                    RelatedEntityId = task.Id,
                    ExpiresAt = task.DueDate ?? null,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId,
                };

                _context.Notifications.Add(notification);
            }

            if (task.GetAssignedToIds().Count > 0)
            {
                foreach (var id in task.GetAssignedToIds())
                {
                    if (id == task.AssignedToId)
                        continue;

                    var notification = new Notification
                    {
                        UserId = id,
                        Title = $"Thông báo ID Task : {task.TaskCode}",
                        Content = $"{task.Title}",
                        Type = NotificationType.TaskUpdated,
                        RelatedEntityType = EntityTypeManager.ProjectTask,
                        RelatedEntityId = task.Id,
                        ExpiresAt = task.DueDate ?? null,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUserId,
                    };

                    _context.Notifications.Add(notification);
                }
            }

            if (task.ReporterId.HasValue && task.ReporterId > 0 && task.ReporterId != currentUserId)
            {
                // Tạo gửi thông báo
                var notification = new Notification
                {
                    UserId = task.ReporterId ?? 0,
                    Title = $"Thông báo ID Task : {task.TaskCode}",
                    Content = $"{task.Title}",
                    Type = NotificationType.TaskUpdated,
                    RelatedEntityType = EntityTypeManager.ProjectTask,
                    RelatedEntityId = task.Id,
                    ExpiresAt = task.DueDate ?? null,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId,
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        /// <summary>
        /// Xóa task (soft delete)
        /// </summary>
        public async Task<bool> DeleteTaskAsync(int projectId, int taskId, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return false;

            var taskParents = await _context.ProjectTasks
                .Where(t => t.ParentTaskId == projectId && t.IsActive && !t.IsDeleted)
                .ToListAsync();

            if(taskParents != null && taskParents.Count > 0)
            {
                throw new InvalidOperationException("Không thể xóa task cha khi còn tồn tại task con. Vui lòng xóa hoặc di chuyển các task con trước.");
            }

            await CheckTaskEditPermissionAsync(task, currentUserId);

            // Check if task has subtasks
            var hasSubTasks = await _context.ProjectTasks
                .AnyAsync(t => t.ParentTaskId == taskId && t.IsActive);

            if (hasSubTasks)
            {
                throw new InvalidOperationException("Không thể xóa task có subtask. Vui lòng xóa subtask trước.");
            }

            task.SoftDelete(currentUserId, "Deleted by user");
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Bắt đầu task
        /// </summary>
        public async Task<ProjectTaskDto?> StartTaskAsync(int projectId, int taskId, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return null;

            await CheckTaskExecutePermissionAsync(task, currentUserId);

            task.StartTask(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        /// <summary>
        /// Hoàn thành task
        /// </summary>
        public async Task<ProjectTaskDto?> CompleteTaskAsync(int projectId, int taskId, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return null;

            await CheckTaskExecutePermissionAsync(task, currentUserId);

            task.CompleteTask(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        /// <summary>
        /// Cập nhật tiến độ task
        /// </summary>
        public async Task<ProjectTaskDto?> UpdateProgressAsync(
            int projectId, int taskId, TaskProgressUpdateRequest reques, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return null;

            await CheckTaskExecutePermissionAsync(task, currentUserId);

            task.ActualHours = reques.ActualHours;

            task.UpdateProgress(reques.Progress, currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        /// <summary>
        /// Assign task cho user
        /// </summary>
        public async Task<ProjectTaskDto?> AssignTaskAsync(
            int projectId, int taskId, int assignedToId, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return null;

            await CheckTaskEditPermissionAsync(task, currentUserId);

            // Validate assigned user
            var isUserInProject = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == assignedToId && pm.IsActive);

            if (!isUserInProject)
            {
                throw new ArgumentException("User được assign không phải thành viên của dự án");
            }

            task.AssignedToId = assignedToId;
            task.MarkAsUpdated(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        /// <summary>
        /// Block task
        /// </summary>
        public async Task<ProjectTaskDto?> BlockTaskAsync(
            int projectId, int taskId, string blockReason, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return null;

            await CheckTaskEditPermissionAsync(task, currentUserId);

            task.IsBlocked = true;
            task.BlockReason = blockReason;
            task.MarkAsUpdated(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        /// <summary>
        /// Unblock task
        /// </summary>
        public async Task<ProjectTaskDto?> UnblockTaskAsync(int projectId, int taskId, int currentUserId)
        {
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task == null)
                return null;

            await CheckTaskEditPermissionAsync(task, currentUserId);

            task.IsBlocked = false;
            task.BlockReason = "";
            task.MarkAsUpdated(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        #endregion

        #region Task Comments - Extended Service Methods

        /// <summary>
        /// Lấy comments với filtering và pagination
        /// </summary>
        public async Task<PagedResult<TaskCommentDto>> GetTaskCommentsPagedAsync(
            int projectId, GetTaskCommentsRequest request, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var query = _context.TaskComments
                .Where(tc => tc.TaskId == request.TaskId && !tc.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (request.ParentTaskCommentId.HasValue && request.ParentTaskCommentId > 0)
            {
                query = query.Where(tc => tc.ParentCommentId == request.ParentTaskCommentId.Value);
            }

            if (request.CommentTypes?.Count > 0)
            {
                query = query.Where(tc => request.CommentTypes.Contains(tc.CommentType));
            }

            if (request.CommentStatuses?.Count > 0)
            {
                query = query.Where(tc => request.CommentStatuses.Contains(tc.CommentStatus));
            }

            if (request.Priorities?.Count > 0)
            {
                query = query.Where(tc => request.Priorities.Contains(tc.Priority));
            }

            if (request.ReviewerId.HasValue)
            {
                query = query.Where(tc => tc.ReviewerId == request.ReviewerId);
            }

            if (request.AssignedToId.HasValue)
            {
                query = query.Where(tc => tc.AssignedToId == request.AssignedToId);
            }

            if (request.CreatedBy.HasValue)
            {
                query = query.Where(tc => tc.CreatedBy == request.CreatedBy);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(tc => tc.CreatedAt >= request.FromDate);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(tc => tc.CreatedAt <= request.ToDate);
            }

            if (request.IsResolved.HasValue)
            {
                if (request.IsResolved.Value)
                    query = query.Where(tc => tc.ResolvedAt.HasValue);
                else
                    query = query.Where(tc => !tc.ResolvedAt.HasValue);
            }

            if (request.IsVerified.HasValue)
            {
                query = query.Where(tc => tc.IsVerified == request.IsVerified);
            }

            if (request.IsBlocking.HasValue)
            {
                query = query.Where(tc => tc.IsBlocking == request.IsBlocking);
            }

            if (request.RequiresDiscussion.HasValue)
            {
                query = query.Where(tc => tc.RequiresDiscussion == request.RequiresDiscussion);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchContent))
            {
                query = query.Where(tc => tc.Content.Contains(request.SearchContent));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchIssueTitle))
            {
                query = query.Where(tc => tc.IssueTitle.Contains(request.SearchIssueTitle));
            }

            // Apply include options
            if (!request.IncludeReplies)
            {
                query = query.Where(tc => tc.ParentCommentId == null);
            }

            var totalCount = await query.CountAsync();


            // Apply sorting
            query = ApplyCommentSorting(query, request.SortBy, request.SortDirection);

            // Apply pagination
            var comments = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var commentDtos = await MapToTaskCommentDtosAsync(comments);

            return new PagedResult<TaskCommentDto>
            {
                Items = commentDtos,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }


        /// <summary>
        /// Lấy comment theo ID
        /// </summary>
        public async Task<TaskCommentDto?> GetTaskCommentByIdAsync(
            int projectId, int taskId, int commentId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            var dto = await MapToTaskCommentDtoAsync(comment);
            dto.TotalReplyCount = await _context.TaskComments
                .Where(tc => tc.TaskId == taskId && !tc.IsDeleted && tc.ParentCommentId == dto.Id)
                .CountAsync();

            return dto;
        }

        /// <summary>
        /// Tạo comment mới cho task
        /// Enhanced version with full validation and business logic
        /// </summary>
        public async Task<TaskCommentDto> CreateTaskCommentAsync(
            int projectId, int taskId, CreateTaskCommentRequest request, int currentUserId)
        {
            // Validate project access
            await CheckProjectAccessAsync(projectId, currentUserId);

            // Validate task exists and belongs to project
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId && t.IsActive);

            if (task == null)
            {
                throw new ArgumentException("Task không tồn tại hoặc không thuộc dự án này");
            }

            // Validate parent comment if specified
            if (request.ParentCommentId.HasValue)
            {
                var parentComment = await _context.TaskComments
                    .FirstOrDefaultAsync(tc => tc.Id == request.ParentCommentId.Value &&
                                              tc.TaskId == taskId &&
                                              !tc.IsDeleted);

                if (parentComment == null)
                {
                    throw new ArgumentException("Parent comment không tồn tại");
                }

                // Check nested level - prevent deep nesting
                var currentLevel = await GetCommentNestingLevelAsync(parentComment.Id);
                if (currentLevel >= 5) // Max 5 levels
                {
                    throw new ArgumentException("Không thể tạo reply quá 5 cấp");
                }
            }

            // Validate assigned users if specified
            if (request.AssignedToId.HasValue && request.AssignedToId.Value > 0)
            {
                var isUserInProject = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == projectId &&
                                   pm.UserId == request.AssignedToId.Value &&
                                   pm.IsActive);

                if (!isUserInProject)
                {
                    throw new ArgumentException("User được assign không phải thành viên của dự án");
                }
            }

            // Validate reviewer if specified
            if (request.ReviewerId.HasValue && request.ReviewerId.Value > 0)
            {
                var isReviewerInProject = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == projectId &&
                                   pm.UserId == request.ReviewerId.Value &&
                                   pm.IsActive);

                if (!isReviewerInProject)
                {
                    throw new ArgumentException("Reviewer không phải thành viên của dự án");
                }
            }

            // Validate mentioned users
            if (request.MentionedUsers?.Count > 0)
            {
                var projectMemberUserIds = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == projectId && pm.IsActive)
                    .Select(pm => pm.UserId)
                    .ToListAsync();

                var projectMemberUsernames = await _context.Users
                    .Where(u => projectMemberUserIds.Contains(u.Id))
                    .Select(u => u.Username)
                    .ToListAsync();

                var invalidMentions = request.MentionedUsers
                    .Where(username => !projectMemberUsernames.Contains(username))
                    .ToList();

                if (invalidMentions.Any())
                {
                    throw new ArgumentException($"Các user được mention không phải thành viên dự án: {string.Join(", ", invalidMentions)}");
                }
            }

            // Create comment entity
            var comment = new TaskComment
            {
                TaskId = taskId,
                Content = request.Content.Trim(),
                ParentCommentId = request.ParentCommentId,
                CommentType = request.CommentType,
                Priority = request.Priority,
                ReviewerId = request.ReviewerId,
                AssignedToId = request.AssignedToId,
                IssueTitle = request.IssueTitle?.Trim() ?? "",
                SuggestedFix = request.SuggestedFix?.Trim() ?? "",
                RelatedModule = request.RelatedModule?.Trim() ?? "",
                EstimatedFixTime = request.EstimatedFixTime,
                DueDate = request.DueDate,
                IsBlocking = request.IsBlocking,
                RequiresDiscussion = request.RequiresDiscussion,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            // Set JSON fields using helper methods
            if (request.RelatedFiles?.Count > 0)
            {
                comment.SetRelatedFiles(request.RelatedFiles);
            }

            if (request.RelatedScreenshots?.Count > 0)
            {
                comment.SetRelatedScreenshots(request.RelatedScreenshots);
            }

            if (request.RelatedDocuments?.Count > 0)
            {
                comment.SetRelatedDocuments(request.RelatedDocuments);
            }

            if (request.Attachments?.Count > 0)
            {
                comment.SetAttachments(request.Attachments);
            }

            if (request.MentionedUsers?.Count > 0)
            {
                comment.SetMentionedUsers(request.MentionedUsers);
            }

            if (request.Tags?.Count > 0)
            {
                comment.SetTags(request.Tags);
            }

            // Set metadata if provided
            if (request.Metadata?.Count > 0)
            {
                try
                {
                    comment.Metadata = System.Text.Json.JsonSerializer.Serialize(request.Metadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to serialize comment metadata: {@Metadata}", request.Metadata);
                    comment.Metadata = "{}";
                }
            }

            // Auto-assign logic
            if (request.AutoAssignToCreator && !request.AssignedToId.HasValue)
            {
                comment.AssignedToId = currentUserId;
                comment.CommentStatus = TaskStatuss.InProgress;
            }

            // Auto-determine priority based on comment type if not specified
            if (request.Priority != TaskPriority.Critical && request.Priority != TaskPriority.Emergency)
            {
                if (CommentTypeExtensions.TryParseCommentType(request.CommentType.ToString(), out CommentType commentType))
                {
                    comment.Priority = commentType.GetDefaultPriority();
                }
            }

            // Set blocking flag based on comment type if not explicitly set
            if (!request.IsBlocking && CommentTypeExtensions.TryParseCommentType(request.CommentType.ToString(), out CommentType blockingCheckType))
            {
                comment.IsBlocking = blockingCheckType.CanBeBlocking();
            }

            // Add to context
            _context.TaskComments.Add(comment);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Comment {CommentId} created successfully for task {TaskId}", comment.Id, taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save comment for task {TaskId}", taskId);
                throw new InvalidOperationException("Không thể lưu comment. Vui lòng thử lại.");
            }

            // Send notifications if enabled
            if (request.SendNotification)
            {
                try
                {
                    await SendCommentNotificationsAsync(comment, projectId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notifications for comment {CommentId}", comment.Id);
                    // Don't fail the whole operation if notification fails
                }
            }

            // Create system comment for important comment types
            //if (ShouldCreateSystemComment(comment))
            //{
            //    try
            //    {
            //        await CreateSystemCommentAsync(comment, currentUserId);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogWarning(ex, "Failed to create system comment for {CommentId}", comment.Id);
            //    }
            //}

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// Cập nhật comment
        /// </summary>
        public async Task<TaskCommentDto?> UpdateTaskCommentAsync(
            int projectId, int taskId, int commentId, UpdateTaskCommentRequest request, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);


            if (comment == null)
                return null;

            // Check permission to edit comment
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            // Version check for concurrency
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            // Update fields
            comment.Content = request.Content;
            comment.CommentType = request.CommentType;
            comment.CommentStatus = request.CommentStatus;
            comment.Priority = request.Priority;
            comment.ReviewerId = request.ReviewerId;
            comment.AssignedToId = request.AssignedToId;
            comment.IssueTitle = request.IssueTitle;
            comment.SuggestedFix = request.SuggestedFix;
            comment.RelatedModule = request.RelatedModule;
            comment.EstimatedFixTime = request.EstimatedFixTime;
            comment.ActualFixTime = request.ActualFixTime;
            comment.DueDate = request.DueDate;
            comment.IsBlocking = request.IsBlocking;
            comment.RequiresDiscussion = request.RequiresDiscussion;
            comment.IsAgreed = request.IsAgreed;

            // Update JSON fields
            comment.SetRelatedFiles(request.RelatedFiles);
            comment.SetRelatedScreenshots(request.RelatedScreenshots);
            comment.SetRelatedDocuments(request.RelatedDocuments);
            comment.SetAttachments(request.Attachments);
            comment.SetMentionedUsers(request.MentionedUsers);
            comment.SetTags(request.Tags);

            // Update resolution fields if provided
            if (!string.IsNullOrWhiteSpace(request.ResolutionNotes))
            {
                comment.ResolutionNotes = request.ResolutionNotes;
                comment.ResolutionCommitId = request.ResolutionCommitId;
            }

            // Update verification fields if provided
            if (!string.IsNullOrWhiteSpace(request.VerificationNotes))
            {
                comment.VerificationNotes = request.VerificationNotes;
                comment.IsVerified = request.IsVerified;
            }

            comment.MarkAsUpdated(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// Xóa comment (soft delete)
        /// </summary>
        public async Task<bool> DeleteTaskCommentAsync(int projectId, int taskId, int commentId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return false;

            // Check permission to delete comment
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            comment.SoftDelete(currentUserId, "Deleted by user");
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Resolve comment
        /// </summary>
        public async Task<TaskCommentDto?> ResolveTaskCommentAsync(
            int projectId, int taskId, int commentId, ResolveTaskCommentRequest request, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentResolvePermissionAsync(comment, currentUserId);

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.MarkAsResolved(currentUserId, request.ResolutionNotes, request.ResolutionCommitId);
            comment.ActualFixTime = request.ActualFixTime;

            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// Verify comment
        /// </summary>
        public async Task<TaskCommentDto?> VerifyTaskCommentAsync(
            int projectId, int taskId, int commentId, VerifyTaskCommentRequest request, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentVerifyPermissionAsync(comment, currentUserId);

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.MarkAsVerified(currentUserId, request.VerificationNotes);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// Agree/Disagree với comment
        /// </summary>
        public async Task<TaskCommentDto?> AgreeTaskCommentAsync(
            int projectId, int taskId, int commentId, AgreeTaskCommentRequest request, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.IsAgreed = request.IsAgreed;
            if (request.IsAgreed)
            {
                comment.AgreedBy = currentUserId;
                comment.AgreedAt = DateTime.UtcNow;
            }
            else
            {
                comment.AgreedBy = null;
                comment.AgreedAt = null;
            }

            comment.MarkAsUpdated(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// Assign comment cho user
        /// </summary>
        public async Task<TaskCommentDto?> AssignTaskCommentAsync(
            int projectId, int taskId, int commentId, AssignTaskCommentRequest request, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            // Validate assigned user
            var isUserInProject = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == request.AssignedToId && pm.IsActive);

            if (!isUserInProject)
            {
                throw new ArgumentException("User được assign không phải thành viên của dự án");
            }

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.AssignTo(request.AssignedToId);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        public async Task<TaskCommentDto?> ReviewTaskCommentAsync(
            int projectId, int taskId, int commentId, ReviewTaskCommentRequest request, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            // Validate assigned user
            var isUserInProject = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == request.ReviewerId && pm.IsActive);

            if (!isUserInProject)
            {
                throw new ArgumentException("User được review không phải thành viên của dự án");
            }

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.AssignTo(request.ReviewerId);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="taskId"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<TaskCommentDto?> PriorityTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId,
            PriorityTaskCommentRequest request,
            int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.Prioritize(request.Priority);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="taskId"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<TaskCommentDto?> BlockingTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId,
            ToggleBlockingTaskCommentRequest request,
            int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.IsBlockingFlag(request.IsBlocking);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="taskId"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<TaskCommentDto?> DiscussionTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId,
            ToggleDiscussionTaskCommentRequest request,
            int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.RequiresDiscussionFlag(request.RequiresDiscussion);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="taskId"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<TaskCommentDto?> StatusTaskCommentAsync(
    int projectId,
    int taskId,
    int commentId,
    StatusTaskCommentRequest request,
    int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.UpdateStatus(request.Status);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="taskId"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<TaskCommentDto?> CommentTypeTaskCommentAsync(
    int projectId,
    int taskId,
    int commentId,
    CommentTypeTaskCommentRequest request,
    int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comment = await _context.TaskComments
                .FirstOrDefaultAsync(tc => tc.Id == commentId && tc.TaskId == taskId && !tc.IsDeleted);

            if (comment == null)
                return null;

            // Check permission
            await CheckCommentEditPermissionAsync(comment, currentUserId);

            // Version check
            if (comment.Version != request.Version)
            {
                throw new ArgumentException("Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            comment.UpdateType(request.CommentType);
            await _context.SaveChangesAsync();

            return await MapToTaskCommentDtoAsync(comment);
        }

        /// <summary>
        /// Lấy replies của comment
        /// </summary>
        public async Task<List<TaskCommentDto>> GetCommentRepliesAsync(
            int projectId, int taskId, int commentId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var replies = await _context.TaskComments
                .Where(tc => tc.TaskId == taskId && tc.ParentCommentId == commentId && !tc.IsDeleted)
                .OrderBy(tc => tc.CreatedAt)
                .ToListAsync();

            return await MapToTaskCommentDtosAsync(replies);
        }

        /// <summary>
        /// Lấy thống kê comments
        /// </summary>
        public async Task<TaskCommentStats> GetTaskCommentStatsAsync(int projectId, int taskId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var comments = await _context.TaskComments
                .Where(tc => tc.TaskId == taskId && !tc.IsDeleted)
                .ToListAsync();

            var stats = new TaskCommentStats
            {
                TaskId = taskId,
                TotalComments = comments.Count,
                PendingComments = comments.Count(c => c.CommentStatus == TaskStatuss.Todo),
                ResolvedComments = comments.Count(c => c.CommentStatus == TaskStatuss.Completed),
                BlockingComments = comments.Count(c => c.IsBlocking),
                VerifiedComments = comments.Count(c => c.IsVerified),
                TotalEstimatedTime = comments.Sum(c => c.EstimatedFixTime),
                TotalActualTime = comments.Sum(c => c.ActualFixTime),
                OverdueComments = comments.Count(c => c.IsOverdue())
            };

            // Group by comment type
            stats.CommentsByType = comments
                .GroupBy(c => c.CommentType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by status
            stats.CommentsByStatus = comments
                .GroupBy(c => c.CommentStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by priority
            stats.CommentsByPriority = comments
                .GroupBy(c => c.Priority)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        #endregion

        /// <summary>
        /// Lấy time logs của task
        /// </summary>
        public async Task<List<TaskTimeLogDto>> GetTaskTimeLogsAsync(int projectId, int taskId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var timeLogs = await _context.TaskTimeLogs
                .Where(ttl => ttl.TaskId == taskId)
                .OrderByDescending(ttl => ttl.StartTime)
                .ToListAsync();

            var result = new List<TaskTimeLogDto>();
            foreach (var timeLog in timeLogs)
            {
                result.Add(await MapToTaskTimeLogDtoAsync(timeLog));
            }
            return result;
        }



        #region Hierarchy Support Methods

        /// <summary>
        /// Lấy subtasks
        /// </summary>
        public async Task<List<ProjectTaskDto>> GetSubTasksAsync(int projectId, int parentTaskId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var subtasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == projectId && t.ParentTaskId == parentTaskId && t.IsActive)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            return await MapToProjectTaskDtosAsync(subtasks);
        }

        /// <summary>
        /// Lấy subtasks với pagination (cho lazy loading)
        /// Similar to GetProjectChildrenAsync in ProjectService
        /// </summary>
        public async Task<PagedResult<ProjectTaskDto>> GetSubTasksPagedAsync(
            int projectId, int parentTaskId, int page, int pageSize, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            _logger.LogInformation("Loading subtasks for parent task {ParentTaskId} (Page {Page}, Size {PageSize})",
                parentTaskId, page, pageSize);

            var query = _context.ProjectTasks
                .Where(t => t.ProjectId == projectId &&
                           t.ParentTaskId == parentTaskId &&
                           t.IsActive)
                .OrderBy(t => t.CreatedAt);

            var totalCount = await query.CountAsync();

            var subtasks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var taskDtos = await MapToProjectTaskDtosAsync(subtasks);

            _logger.LogInformation("Loaded {Count} subtasks for parent task {ParentTaskId}",
                taskDtos.Count, parentTaskId);

            return new PagedResult<ProjectTaskDto>
            {
                Items = taskDtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Lấy full hierarchy tree cho một task với max depth
        /// Similar to GetProjectHierarchyAsync in ProjectService
        /// </summary>
        public async Task<ProjectTaskDto?> GetTaskHierarchyAsync(
            int projectId, int taskId, int currentUserId, int maxDepth = 3)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            _logger.LogInformation("Loading task hierarchy for task {TaskId} (MaxDepth: {MaxDepth})",
                taskId, maxDepth);

            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId && t.IsActive);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found in project {ProjectId}", taskId, projectId);
                return null;
            }

            var taskDto = await MapToProjectTaskDtoAsync(task);

            // Load children recursively up to maxDepth
            await LoadTaskChildrenRecursiveAsync(taskDto, projectId, currentDepth: 0, maxDepth);

            _logger.LogInformation("Loaded task hierarchy for task {TaskId} with {ChildCount} direct children",
                taskId, taskDto.SubTasks?.Count ?? 0);

            return taskDto;
        }

        /// <summary>
        /// Helper method để load children recursively
        /// </summary>
        private async Task LoadTaskChildrenRecursiveAsync(
            ProjectTaskDto parentTask, int projectId, int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth || !parentTask.HasSubTasks)
            {
                return;
            }

            var children = await _context.ProjectTasks
                .Where(t => t.ProjectId == projectId &&
                           t.ParentTaskId == parentTask.Id &&
                           t.IsActive)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            if (children.Count == 0)
            {
                return;
            }

            parentTask.SubTasks = new List<ProjectTaskDto>();

            foreach (var child in children)
            {
                var childDto = await MapToProjectTaskDtoAsync(child);
                childDto.HierarchyLevel = currentDepth + 1;

                // Load grandchildren recursively
                await LoadTaskChildrenRecursiveAsync(childDto, projectId, currentDepth + 1, maxDepth);

                parentTask.SubTasks.Add(childDto);
            }

            // Update hierarchy properties
            parentTask.TotalChildCount = children.Count;
            parentTask.HasSubTasks = children.Count > 0;
        }

        /// <summary>
        /// Di chuyển task sang parent mới
        /// Similar to MoveProjectAsync in ProjectService
        /// </summary>
        public async Task<ProjectTaskDto?> MoveTaskToParentAsync(
            int projectId, int taskId, int? newParentTaskId, int currentUserId, string reason = "")
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId && t.IsActive);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found in project {ProjectId}", taskId, projectId);
                return null;
            }

            // Check permission
            await CheckTaskEditPermissionAsync(task, currentUserId);

            // Validate new parent if specified
            if (newParentTaskId.HasValue && newParentTaskId.Value > 0)
            {
                var newParent = await _context.ProjectTasks
                    .FirstOrDefaultAsync(t => t.Id == newParentTaskId.Value &&
                                             t.ProjectId == projectId &&
                                             t.IsActive);

                if (newParent == null)
                {
                    throw new ArgumentException("New parent task không tồn tại");
                }

                // Prevent circular reference - check if newParent is descendant of current task
                if (await IsTaskDescendantOfAsync(newParentTaskId.Value, taskId))
                {
                    throw new InvalidOperationException(
                        "Không thể di chuyển task vào subtask của chính nó (circular reference)");
                }

                // Check max depth
                var newDepth = await CalculateTaskDepthAsync(newParentTaskId.Value) + 1;
                var currentSubtreeDepth = await CalculateSubtreeDepthAsync(taskId);

                if (newDepth + currentSubtreeDepth > 5) // Max 5 levels
                {
                    throw new InvalidOperationException(
                        $"Di chuyển task sẽ vượt quá độ sâu tối đa (5 levels). " +
                        $"Current depth: {newDepth}, Subtree depth: {currentSubtreeDepth}");
                }
            }

            // Update parent
            var oldParentId = task.ParentTaskId;
            task.ParentTaskId = newParentTaskId;
            task.MarkAsUpdated(currentUserId);

            _logger.LogInformation(
                "Moving task {TaskId} from parent {OldParent} to parent {NewParent}. Reason: {Reason}",
                taskId, oldParentId, newParentTaskId, reason);

            await _context.SaveChangesAsync();

            return await MapToProjectTaskDtoAsync(task);
        }

        /// <summary>
        /// Check if targetTask is a descendant of potentialAncestorTask
        /// Prevent circular reference khi move task
        /// </summary>
        private async Task<bool> IsTaskDescendantOfAsync(int targetTaskId, int potentialAncestorTaskId)
        {
            var currentTaskId = targetTaskId;
            var visited = new HashSet<int>(); // Prevent infinite loop
            var maxIterations = 10; // Safety limit
            var iteration = 0;

            while (currentTaskId > 0 && iteration < maxIterations)
            {
                if (currentTaskId == potentialAncestorTaskId)
                {
                    return true; // Found ancestor
                }

                if (visited.Contains(currentTaskId))
                {
                    _logger.LogWarning("Circular reference detected in task hierarchy at task {TaskId}", currentTaskId);
                    return true; // Circular reference detected, treat as descendant
                }

                visited.Add(currentTaskId);

                var parentTaskId = await _context.ProjectTasks
                    .Where(t => t.Id == currentTaskId && t.IsActive)
                    .Select(t => t.ParentTaskId)
                    .FirstOrDefaultAsync();

                if (!parentTaskId.HasValue || parentTaskId.Value <= 0)
                {
                    break; // Reached root
                }

                currentTaskId = parentTaskId.Value;
                iteration++;
            }

            return false; // Not a descendant
        }

        /// <summary>
        /// Calculate depth of a task from root (0 = root task)
        /// </summary>
        private async Task<int> CalculateTaskDepthAsync(int taskId)
        {
            var depth = 0;
            var currentTaskId = taskId;
            var visited = new HashSet<int>();
            var maxIterations = 10;
            var iteration = 0;

            while (currentTaskId > 0 && iteration < maxIterations)
            {
                if (visited.Contains(currentTaskId))
                {
                    _logger.LogWarning("Circular reference detected while calculating depth for task {TaskId}", taskId);
                    break;
                }

                visited.Add(currentTaskId);

                var parentTaskId = await _context.ProjectTasks
                    .Where(t => t.Id == currentTaskId && t.IsActive)
                    .Select(t => t.ParentTaskId)
                    .FirstOrDefaultAsync();

                if (!parentTaskId.HasValue || parentTaskId.Value <= 0)
                {
                    break; // Reached root
                }

                depth++;
                currentTaskId = parentTaskId.Value;
                iteration++;
            }

            return depth;
        }

        /// <summary>
        /// Calculate max depth of subtree rooted at taskId
        /// </summary>
        private async Task<int> CalculateSubtreeDepthAsync(int taskId)
        {
            var children = await _context.ProjectTasks
                .Where(t => t.ParentTaskId == taskId && t.IsActive)
                .Select(t => t.Id)
                .ToListAsync();

            if (children.Count == 0)
            {
                return 0; // Leaf node
            }

            var maxChildDepth = 0;
            foreach (var childId in children)
            {
                var childDepth = await CalculateSubtreeDepthAsync(childId);
                maxChildDepth = Math.Max(maxChildDepth, childDepth);
            }

            return maxChildDepth + 1;
        }

        /// <summary>
        /// Get root tasks (tasks without parent) với pagination
        /// </summary>
        public async Task<PagedResult<ProjectTaskDto>> GetRootTasksAsync(
            int projectId, int page, int pageSize, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            _logger.LogInformation("Loading root tasks for project {ProjectId} (Page {Page}, Size {PageSize})",
                projectId, page, pageSize);

            var query = _context.ProjectTasks
                .Where(t => t.ProjectId == projectId &&
                           (!t.ParentTaskId.HasValue || t.ParentTaskId == 0) &&
                           t.IsActive)
                .OrderBy(t => t.CreatedAt);

            var totalCount = await query.CountAsync();

            var rootTasks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var taskDtos = await MapToProjectTaskDtosAsync(rootTasks);

            // Set hierarchy level = 0 cho root tasks
            foreach (var dto in taskDtos)
            {
                dto.HierarchyLevel = 0;
                dto.IsRootTask = true;
            }

            _logger.LogInformation("Loaded {Count} root tasks for project {ProjectId}",
                taskDtos.Count, projectId);

            return new PagedResult<ProjectTaskDto>
            {
                Items = taskDtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Get task path from root to current task (breadcrumb)
        /// </summary>
        public async Task<List<ProjectTaskDto>> GetTaskPathAsync(
            int projectId, int taskId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var path = new List<ProjectTaskDto>();
            var currentTaskId = taskId;
            var visited = new HashSet<int>();
            var maxIterations = 10;
            var iteration = 0;

            while (currentTaskId > 0 && iteration < maxIterations)
            {
                if (visited.Contains(currentTaskId))
                {
                    _logger.LogWarning("Circular reference detected in task path for task {TaskId}", taskId);
                    break;
                }

                visited.Add(currentTaskId);

                var task = await _context.ProjectTasks
                    .FirstOrDefaultAsync(t => t.Id == currentTaskId &&
                                             t.ProjectId == projectId &&
                                             t.IsActive);

                if (task == null)
                {
                    break;
                }

                var taskDto = await MapToProjectTaskDtoAsync(task);
                path.Insert(0, taskDto); // Insert at beginning to maintain root -> leaf order

                if (!task.ParentTaskId.HasValue || task.ParentTaskId.Value <= 0)
                {
                    break; // Reached root
                }

                currentTaskId = task.ParentTaskId.Value;
                iteration++;
            }

            // Set hierarchy levels
            for (int i = 0; i < path.Count; i++)
            {
                path[i].HierarchyLevel = i;
            }

            return path;
        }

        /// <summary>
        /// Bulk move tasks to new parent
        /// </summary>
        public async Task<int> BulkMoveTasksAsync(
            int projectId, List<int> taskIds, int? newParentTaskId, int currentUserId, string reason = "")
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            if (taskIds == null || taskIds.Count == 0)
            {
                throw new ArgumentException("TaskIds không được để trống");
            }

            if (taskIds.Count > 100)
            {
                throw new ArgumentException("Không thể di chuyển quá 100 tasks cùng lúc");
            }

            _logger.LogInformation(
                "Bulk moving {Count} tasks to parent {ParentId} in project {ProjectId}",
                taskIds.Count, newParentTaskId, projectId);

            var movedCount = 0;
            var errors = new List<string>();

            foreach (var taskId in taskIds)
            {
                try
                {
                    var result = await MoveTaskToParentAsync(projectId, taskId, newParentTaskId, currentUserId, reason);
                    if (result != null)
                    {
                        movedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to move task {TaskId}: {Message}", taskId, ex.Message);
                    errors.Add($"Task {taskId}: {ex.Message}");
                }
            }

            if (errors.Count > 0 && errors.Count == taskIds.Count)
            {
                throw new InvalidOperationException(
                    $"Không thể di chuyển bất kỳ task nào. Errors: {string.Join("; ", errors)}");
            }

            _logger.LogInformation("Bulk move completed: {MovedCount}/{TotalCount} tasks moved successfully",
                movedCount, taskIds.Count);

            return movedCount;
        }

        #endregion

        #region Private Helper Methods

        private IQueryable<ProjectTask> ApplySorting(IQueryable<ProjectTask> query, string sortBy, string sortDirection)
        {
            var ascending = sortDirection?.ToLower() != "desc";

            return sortBy?.ToLower() switch
            {
                "title" => ascending ? query.OrderBy(t => t.Title) : query.OrderByDescending(t => t.Title),
                "status" => ascending ? query.OrderBy(t => t.Status) : query.OrderByDescending(t => t.Status),
                "priority" => ascending ? query.OrderBy(t => t.Priority) : query.OrderByDescending(t => t.Priority),
                "assignedtoid" => ascending ? query.OrderBy(t => t.AssignedToId) : query.OrderByDescending(t => t.AssignedToId),
                "duedate" => ascending ? query.OrderBy(t => t.DueDate) : query.OrderByDescending(t => t.DueDate),
                "progress" => ascending ? query.OrderBy(t => t.Progress) : query.OrderByDescending(t => t.Progress),
                "updatedat" => ascending ? query.OrderBy(t => t.UpdatedAt) : query.OrderByDescending(t => t.UpdatedAt),
                _ => ascending ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt),
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

        private async Task CheckTaskEditPermissionAsync(ProjectTask task, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            // Project manager can edit all tasks
            var project = await _context.Projects.FindAsync(task.ProjectId);
            if (project?.ProjectManagerId == currentUserId)
                return;

            // Task reporter can edit
            if (task.ReporterId == currentUserId)
                return;

            // Assigned user can edit some properties
            if (task.AssignedToId == currentUserId)
                return;

            if(task.AssignedToIds.Contains(currentUserId.ToString()))
                return;

            // Check if user is project manager member
            var isProjectManager = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == currentUserId &&
                              pm.IsActive && pm.ProjectRole == UserRole.Manager);

            if (!isProjectManager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa task này");
            }
        }

        private async Task CheckTaskExecutePermissionAsync(ProjectTask task, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            // Assigned user can execute
            if (task.AssignedToId == currentUserId)
                return;

            // Project manager can execute
            var project = await _context.Projects.FindAsync(task.ProjectId);
            if (project?.ProjectManagerId == currentUserId)
                return;

            // Check if user is project manager member
            var isProjectManager = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == currentUserId &&
                              pm.IsActive && pm.ProjectRole == UserRole.Manager);

            if (!isProjectManager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thực thi task này");
            }
        }

        private async Task<List<ProjectTaskDto>> MapToProjectTaskDtosAsync(List<ProjectTask> tasks)
        {
            var result = new List<ProjectTaskDto>();
            foreach (var task in tasks)
            {
                result.Add(await MapToProjectTaskDtoAsync(task));
            }
            return result;
        }

        private async Task<ProjectTaskDto> MapToProjectTaskDtoAsync(ProjectTask task)
        {
            // Get assigned user info
            var assignedUser = task.AssignedToId.HasValue && task.AssignedToId.Value > 0 ?
                await _context.Users.FindAsync(task.AssignedToId.Value) : null;

            // Get reporter info
            var reporter = task.ReporterId > 0 ?
                await _context.Users.FindAsync(task.ReporterId) : null;

            // Count subtasks
            var subtaskCount = await _context.ProjectTasks
                .CountAsync(t => t.ParentTaskId == task.Id && t.IsActive);

            // Count comments
            var commentCount = await _context.TaskComments
                .CountAsync(tc => tc.TaskId == task.Id && !tc.IsDeleted);

            var comSub = await _context.ProjectTasks
                .CountAsync(t => t.ParentTaskId == task.Id && t.IsActive && t.Status == TaskStatuss.Completed);

            var completedCommentCount = await _context.TaskComments
                .CountAsync(tc => tc.TaskId == task.Id && !tc.IsDeleted && tc.CommentStatus == TaskStatuss.Completed);

            var totalTimeCommentActualHours = await _context.TaskComments
                .Where(tc => tc.TaskId == task.Id && !tc.IsDeleted)
                .SumAsync(tc => (decimal?)tc.ActualFixTime) ?? 0;

            // Get AssignedToIds
            var assignedUsers = new List<string>();
            foreach (var userId in task.GetAssignedToIds())
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                    assignedUsers.Add(user.FullName);
            }

            return new ProjectTaskDto
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                ParentTaskId = task.ParentTaskId,
                TaskCode = task.TaskCode,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                AssignedToId = task.AssignedToId,
                AssignedToName = assignedUser?.FullName ?? "",
                ReporterId = task.ReporterId,
                ReporterName = reporter?.FullName ?? "",
                AssignedToIds = task.GetAssignedToIds(),
                AssignedToNames = assignedUsers,
                EstimatedHours = (decimal)Math.Round((task.EstimatedHours / 60.0), 2),
                ActualHours = (decimal)Math.Round((task.ActualHours / 60.0), 2),
                Progress = task.Progress,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                CompletedBy = task.CompletedBy,
                IsBlocked = task.IsBlocked,
                BlockReason = task.BlockReason,
                IsActive = task.IsActive,
                TaskType = task.TaskType,
                Tags = task.GetTags(),
                CreatedAt = task.CreatedAt,
                CreatedBy = task.CreatedBy,
                UpdatedAt = task.UpdatedAt,
                UpdatedBy = task.UpdatedBy,
                IsOverdue = task.IsOverdue,
                IsCompleted = task.IsCompleted,
                IsSubTask = task.ParentTaskId.HasValue,
                TotalChildCount = subtaskCount,
                CommentCount = commentCount,
                SubTasksCompleted = comSub,
                HasSubTasks = subtaskCount > 0,
                CompletedCommentCount = completedCommentCount,
                TotalTimeCommentActualHours = totalTimeCommentActualHours,

                HierarchyLevel = 0, // Default, can be set later
                Version = task.Version
            };
        }

        

        private async Task<TaskTimeLogDto> MapToTaskTimeLogDtoAsync(TaskTimeLog timeLog)
        {
            // Get user info
            var user = timeLog.UserId > 0 ?
                await _context.Users.FindAsync(timeLog.UserId) : null;

            return new TaskTimeLogDto
            {
                Id = timeLog.Id,
                TaskId = timeLog.TaskId,
                UserId = timeLog.UserId,
                UserName = user?.FullName ?? "", // Load UserName from Users table
                StartTime = timeLog.StartTime,
                EndTime = timeLog.EndTime,
                Duration = timeLog.Duration,
                Description = timeLog.Description,
                IsBillable = timeLog.IsBillable,
                HourlyRate = timeLog.HourlyRate,
                CreatedAt = timeLog.CreatedAt,
                CreatedBy = timeLog.CreatedBy
            };
        }

        #region Private Helper Methods for Comments

        /// <summary>
        /// Lấy nesting level của comment để tránh quá sâu
        /// </summary>
        private async Task<int> GetCommentNestingLevelAsync(int commentId)
        {
            var level = 0;
            var currentCommentId = commentId;

            while (currentCommentId > 0 && level < 10) // Safety limit
            {
                var parentComment = await _context.TaskComments
                    .Where(tc => tc.Id == currentCommentId)
                    .Select(tc => tc.ParentCommentId)
                    .FirstOrDefaultAsync();

                if (parentComment.HasValue)
                {
                    level++;
                    currentCommentId = parentComment.Value;
                }
                else
                {
                    break;
                }
            }

            return level;
        }

        /// <summary>
        /// Gửi notifications cho comment mới
        /// </summary>
        private async Task SendCommentNotificationsAsync(TaskComment comment, int projectId)
        {
            var notificationUserIds = new HashSet<int?>();

            // Add assignee
            if (comment.AssignedToId.HasValue)
            {
                notificationUserIds.Add(comment.AssignedToId.Value);
            }

            // Add reviewer
            if (comment.ReviewerId.HasValue)
            {
                notificationUserIds.Add(comment.ReviewerId.Value);
            }

            // Add mentioned users
            var mentionedUsers = comment.GetMentionedUsers();
            if (mentionedUsers.Count > 0)
            {
                var mentionedUserIds = await _context.Users
                    .Where(u => mentionedUsers.Contains(u.Username))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in mentionedUserIds)
                {
                    notificationUserIds.Add(userId);
                }
            }

            // Add task assignee and reporter
            var task = await _context.ProjectTasks.FindAsync(comment.TaskId);
            if (task != null)
            {
                if (task.AssignedToId.HasValue)
                    notificationUserIds.Add(task.AssignedToId.Value);

                if (task.ReporterId > 0)
                    notificationUserIds.Add(task.ReporterId);
            }

            // Add project manager
            var project = await _context.Projects.FindAsync(projectId);
            if (project?.ProjectManagerId > 0)
            {
                notificationUserIds.Add(project.ProjectManagerId);
            }

            // Remove comment creator from notifications
            notificationUserIds.Remove(comment.CreatedBy);

            // Create notifications (implement based on your notification system)
            foreach (var userId in notificationUserIds)
            {
                // TODO: Implement notification creation
                _logger.LogDebug("Should send notification to user {UserId} for comment {CommentId}", userId, comment.Id);
            }
        }

        /// <summary>
        /// Kiểm tra có nên tạo system comment không
        /// </summary>
        private bool ShouldCreateSystemComment(TaskComment comment)
        {
            return comment.CommentType switch
            {
                CommentType.IssueReport => true,
                CommentType.ChangeRequest => true,
                CommentType.Approval => true,
                CommentType.Rejection => true,
                _ => false
            };
        }

        /// <summary>
        /// Tạo system comment cho tracking
        /// </summary>
        private async Task CreateSystemCommentAsync(TaskComment originalComment, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            var userName = user?.FullName ?? "Unknown User";

            var systemContent = originalComment.CommentType switch
            {
                CommentType.IssueReport => $"🐛 {userName} đã báo cáo vấn đề: {originalComment.IssueTitle}",
                CommentType.ChangeRequest => $"🔄 {userName} đã yêu cầu thay đổi",
                CommentType.Approval => $"✅ {userName} đã phê duyệt",
                CommentType.Rejection => $"❌ {userName} đã từ chối",
                _ => $"💬 {userName} đã thêm comment loại {originalComment.CommentType}"
            };

            var systemComment = new TaskComment
            {
                TaskId = originalComment.TaskId,
                Content = systemContent,
                CommentType = CommentType.StatusUpdate,
                Priority = TaskPriority.Low,
                CreatedBy = -1 // System user ID
            };

            _context.TaskComments.Add(systemComment);
            await _context.SaveChangesAsync();
        }

        private IQueryable<TaskComment> ApplyCommentSorting(IQueryable<TaskComment> query, string sortBy, string sortDirection)
        {
            var ascending = sortDirection?.ToLower() != "desc";

            return sortBy?.ToLower() switch
            {
                "commenttype" => ascending ? query.OrderBy(tc => tc.CommentType) : query.OrderByDescending(tc => tc.CommentType),
                "priority" => ascending ? query.OrderBy(tc => tc.Priority) : query.OrderByDescending(tc => tc.Priority),
                "status" => ascending ? query.OrderBy(tc => tc.CommentStatus) : query.OrderByDescending(tc => tc.CommentStatus),
                "duedate" => ascending ? query.OrderBy(tc => tc.DueDate) : query.OrderByDescending(tc => tc.DueDate),
                "updatedat" => ascending ? query.OrderBy(tc => tc.UpdatedAt) : query.OrderByDescending(tc => tc.UpdatedAt),
                _ => ascending ? query.OrderBy(tc => tc.CreatedAt) : query.OrderByDescending(tc => tc.CreatedAt),
            };
        }

        private async Task CheckCommentEditPermissionAsync(TaskComment comment, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            // Comment creator can edit
            if (comment.CreatedBy == currentUserId)
                return;

            // Assigned user can edit
            if (comment.AssignedToId == currentUserId)
                return;

            // Reviewer can edit
            if (comment.ReviewerId == currentUserId)
                return;

            // Project manager can edit
            var task = await _context.ProjectTasks.FindAsync(comment.TaskId);
            if (task != null)
            {
                var project = await _context.Projects.FindAsync(task.ProjectId);
                if (project?.ProjectManagerId == currentUserId)
                    return;
            }

            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa comment này");
        }

        private async Task CheckCommentResolvePermissionAsync(TaskComment comment, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            // Assigned user can resolve
            if (comment.AssignedToId == currentUserId)
                return;

            // Project manager can resolve
            var task = await _context.ProjectTasks.FindAsync(comment.TaskId);
            if (task != null)
            {
                var project = await _context.Projects.FindAsync(task.ProjectId);
                if (project?.ProjectManagerId == currentUserId)
                    return;
            }

            throw new UnauthorizedAccessException("Bạn không có quyền resolve comment này");
        }

        private async Task CheckCommentVerifyPermissionAsync(TaskComment comment, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            // Reviewer can verify
            if (comment.ReviewerId == currentUserId)
                return;

            // Project manager can verify
            var task = await _context.ProjectTasks.FindAsync(comment.TaskId);
            if (task != null)
            {
                var project = await _context.Projects.FindAsync(task.ProjectId);
                if (project?.ProjectManagerId == currentUserId)
                    return;
            }

            throw new UnauthorizedAccessException("Bạn không có quyền verify comment này");
        }

        private async Task<TaskCommentDto> MapToTaskCommentDtoAsync(TaskComment comment)
        {
            // Get user info
            var createdByUser = comment.CreatedBy > 0 ? await _context.Users.FindAsync(comment.CreatedBy) : null;
            var updatedByUser = comment.UpdatedBy > 0 ? await _context.Users.FindAsync(comment.UpdatedBy) : null;
            var assignedUser = comment.AssignedToId.HasValue && comment.AssignedToId > 0 ? await _context.Users.FindAsync(comment.AssignedToId) : null;
            var reviewer = comment.ReviewerId.HasValue && comment.ReviewerId > 0 ? await _context.Users.FindAsync(comment.ReviewerId) : null;
            var resolvedByUser = comment.ResolvedBy.HasValue && comment.ResolvedBy > 0 ? await _context.Users.FindAsync(comment.ResolvedBy) : null;
            var verifiedByUser = comment.VerifiedBy.HasValue && comment.VerifiedBy > 0 ? await _context.Users.FindAsync(comment.VerifiedBy) : null;
            var agreedByUser = comment.AgreedBy.HasValue && comment.AgreedBy > 0 ? await _context.Users.FindAsync(comment.AgreedBy) : null;

            // Count replies
            var replyCount = await _context.TaskComments
                .CountAsync(tc => tc.ParentCommentId == comment.Id && !tc.IsDeleted);

            return new TaskCommentDto
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CommentType = comment.CommentType,
                CommentStatus = comment.CommentStatus,
                Priority = comment.Priority,
                ReviewerId = comment.ReviewerId,
                ReviewerName = reviewer?.FullName ?? "",
                AssignedToId = comment.AssignedToId,
                AssignedToName = assignedUser?.FullName ?? "",
                IssueTitle = comment.IssueTitle,
                SuggestedFix = comment.SuggestedFix,
                RelatedModule = comment.RelatedModule,
                RelatedFiles = comment.GetRelatedFiles(),
                RelatedScreenshots = comment.GetRelatedScreenshots(),
                RelatedDocuments = comment.GetRelatedDocuments(),
                Attachments = comment.GetAttachments(),
                MentionedUsers = comment.GetMentionedUsers(),
                ResolvedAt = comment.ResolvedAt,
                ResolvedBy = comment.ResolvedBy,
                ResolvedByName = resolvedByUser?.FullName ?? "",
                ResolutionNotes = comment.ResolutionNotes,
                ResolutionCommitId = comment.ResolutionCommitId,
                VerifiedAt = comment.VerifiedAt,
                VerifiedBy = comment.VerifiedBy,
                VerifiedByName = verifiedByUser?.FullName ?? "",
                VerificationNotes = comment.VerificationNotes,
                IsVerified = comment.IsVerified,
                EstimatedFixTime = comment.EstimatedFixTime,
                ActualFixTime = comment.ActualFixTime,
                DueDate = comment.DueDate,
                IsBlocking = comment.IsBlocking,
                RequiresDiscussion = comment.RequiresDiscussion,
                IsAgreed = comment.IsAgreed,
                AgreedBy = comment.AgreedBy,
                AgreedByName = agreedByUser?.FullName ?? "",
                AgreedAt = comment.AgreedAt,
                Tags = comment.GetTags(),
                CreatedAt = comment.CreatedAt,
                CreatedBy = comment.CreatedBy,
                CreatedByName = createdByUser?.FullName ?? "",
                UpdatedAt = comment.UpdatedAt,
                UpdatedBy = comment.UpdatedBy,
                UpdatedByName = updatedByUser?.FullName ?? "",
                TotalReplyCount = replyCount,
                Version = comment.Version
            };
        }

        private async Task<List<TaskCommentDto>> MapToTaskCommentDtosAsync(List<TaskComment> comments)
        {
            var result = new List<TaskCommentDto>();
            foreach (var comment in comments)
            {
                result.Add(await MapToTaskCommentDtoAsync(comment));
            }
            return result;
        }

        #endregion

        #endregion
    }

}