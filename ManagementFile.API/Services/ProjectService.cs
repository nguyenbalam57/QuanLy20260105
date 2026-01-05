using Azure.Core;
using ManagementFile.API.Data;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Enums.Extensions;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectMembers;
using ManagementFile.Contracts.Requests.ProjectManagement.Projects;
using ManagementFile.Models.AuditAndLogging;
using ManagementFile.Models.ProjectManagement;
using ManagementFile.Models.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Service xử lý business logic cho Projects
    /// Enhanced with hierarchy support
    /// </summary>
    public class ProjectService
    {
        private readonly ManagementFileDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ManagementFileDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách projects với filter và pagination
        /// Enhanced with hierarchy support
        /// </summary>
        public async Task<PagedResult<ProjectDto>> GetProjectsAsync(ProjectFilterRequest filter, int currentUserId)
        {
            var query = _context.Projects.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(p => p.ProjectName.Contains(filter.SearchTerm) ||
                                        p.ProjectCode.Contains(filter.SearchTerm) ||
                                        p.Description.Contains(filter.SearchTerm));
            }

            if (filter.Status.HasValue && filter.Status != ProjectStatus.All)
            {
                query = query.Where(p => p.Status == filter.Status);
            }

            if (filter.ProjectManagerId.HasValue)
            {
                query = query.Where(p => p.ProjectManagerId == filter.ProjectManagerId);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == filter.IsActive.Value);
            }

            if (filter.ProjectParentId.HasValue && filter.ProjectParentId > 0)
            {
                query = query.Where(p => p.ProjectParentId == filter.ProjectParentId);
            }
            else
            {
                query = query.Where(p => p.ProjectParentId == null);
            }

            // Check user permissions - non-admin users only see projects they're members of
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != UserRole.Admin)
            {
                var userProjectIds = await _context.ProjectMembers
                    .Where(pm => pm.UserId == currentUserId && pm.IsActive)
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();

                query = query.Where(p => userProjectIds.Contains(p.Id));
            }

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);

            // Apply pagination
            var projects = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var projectDtos = await MapToProjectDtosAsync(projects);

            return new PagedResult<ProjectDto>
            {
                Items = projectDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        /// <summary>
        /// Lấy danh sách children của một project
        /// Hỗ trợ lazy loading cho hierarchy
        /// </summary>
        public async Task<PagedResult<ProjectDto>> GetProjectChildrenAsync(
            int parentProjectId,
            int pageNumber,
            int pageSize,
            int currentUserId)
        {
            // Verify parent project exists and user has access
            await CheckProjectAccessAsync(parentProjectId, currentUserId);

            var query = _context.Projects
                .Where(p => p.ProjectParentId == parentProjectId && p.IsActive);

            var totalCount = await query.CountAsync();

            // Apply pagination
            var children = await query
                .OrderBy(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var childDtos = await MapToProjectDtosAsync(children);

            return new PagedResult<ProjectDto>
            {
                Items = childDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Lấy toàn bộ hierarchy tree của một project
        /// Load tất cả descendants recursively
        /// </summary>
        public async Task<ProjectDto> GetProjectHierarchyAsync(int projectId, int currentUserId, int maxDepth = 3)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new KeyNotFoundException($"Project {projectId} not found");

            await CheckProjectAccessAsync(projectId, currentUserId);

            var projectDto = await MapToProjectDtoAsync(project);

            // Load hierarchy recursively
            await LoadChildrenRecursiveAsync(projectDto, currentUserId, 0, maxDepth);

            return projectDto;
        }

        /// <summary>
        /// Load children recursively cho một project
        /// </summary>
        private async Task LoadChildrenRecursiveAsync(
            ProjectDto parentDto,
            int currentUserId,
            int currentDepth,
            int maxDepth)
        {
            if (currentDepth >= maxDepth)
                return;

            var children = await _context.Projects
                .Where(p => p.ProjectParentId == parentDto.Id && p.IsActive)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            if (!children.Any())
                return;

            parentDto.Children = new List<ProjectDto>();

            foreach (var child in children)
            {
                var childDto = await MapToProjectDtoAsync(child);

                // Load metadata
                var childCount = await _context.Projects
                    .Where(p => p.ProjectParentId == child.Id && p.IsActive)
                    .CountAsync();
                childDto.TotalChildCount = childCount;

                // Load children recursively
                await LoadChildrenRecursiveAsync(childDto, currentUserId, currentDepth + 1, maxDepth);

                parentDto.Children.Add(childDto);
            }
        }

        /// <summary>
        /// Validate circular reference khi set parent
        /// Prevent infinite loops in hierarchy
        /// </summary>
        private async Task<bool> WouldCreateCircularReferenceAsync(int projectId, int? newParentId)
        {
            if (!newParentId.HasValue)
                return false;

            // Can't set self as parent
            if (projectId == newParentId.Value)
                return true;

            // Check if newParent is a descendant of current project
            var currentProjectId = newParentId.Value;
            var checkedIds = new HashSet<int> { projectId };

            while (currentProjectId > 0)
            {
                if (checkedIds.Contains(currentProjectId))
                    return true; // Circular reference detected

                checkedIds.Add(currentProjectId);

                var parent = await _context.Projects
                    .Where(p => p.Id == currentProjectId)
                    .Select(p => new { p.ProjectParentId })
                    .FirstOrDefaultAsync();

                if (parent?.ProjectParentId == null)
                    break;

                currentProjectId = parent.ProjectParentId.Value;
            }

            return false;
        }

        /// <summary>
        /// Move project to new parent
        /// </summary>
        public async Task<ProjectDto?> MoveProjectAsync(
            int projectId,
            int? newParentId,
            int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            // Validate circular reference
            if (await WouldCreateCircularReferenceAsync(projectId, newParentId))
            {
                throw new InvalidOperationException(
                    "Không thể di chuyển dự án: sẽ tạo ra circular reference trong hierarchy");
            }

            // Verify new parent exists and user has access
            if (newParentId.HasValue)
            {
                var newParent = await _context.Projects.FindAsync(newParentId.Value);
                if (newParent == null || !newParent.IsActive)
                {
                    throw new ArgumentException("Dự án cha không tồn tại hoặc không hoạt động");
                }

                await CheckProjectAccessAsync(newParentId.Value, currentUserId);
            }

            project.ProjectParentId = newParentId;
            project.MarkAsUpdated(currentUserId);

            await _context.SaveChangesAsync();

            return await MapToProjectDtoAsync(project);
        }

        /// <summary>
        /// Lấy project theo ID
        /// Enhanced with hierarchy info
        /// </summary>
        public async Task<ProjectDto?> GetProjectByIdAsync(int projectId, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            // Check permissions
            await CheckProjectAccessAsync(projectId, currentUserId);

            var proj = await MapToProjectDtoAsync(project);

            var countTasks = await _context.ProjectTasks
                    .Where(t => t.ProjectId == proj.Id && t.IsActive)
                    .CountAsync();
            var completedTasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == proj.Id && t.IsActive &&
                            t.Status == TaskStatuss.Completed)
                .CountAsync();

            var totalMembers = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == proj.Id && pm.IsActive)
                .CountAsync();

            // Load child count
            var childCount = await _context.Projects
                .Where(p => p.ProjectParentId == proj.Id && p.IsActive)
                .CountAsync();

            proj.TotalTasks = countTasks > 0 ? countTasks : 0;
            proj.CompletedTasks = completedTasks > 0 ? completedTasks : 0;
            proj.TotalMembers = totalMembers > 0 ? totalMembers : 0;
            proj.TotalChildCount = childCount;

            return proj;
        }

        /// <summary>
        /// Tạo project mới
        /// Enhanced with parent validation
        /// </summary>
        public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, int currentUserId)
        {
            // Generate task code
            var taskCount = await _context.Projects.CountAsync();
            var projectCode = $"PRJ-{taskCount + 1:D3}";

            // Validate project manager exists
            var projectManager = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.ProjectManagerId && u.IsActive);

            if (projectManager == null)
            {
                throw new ArgumentException("Người quản lý dự án không tồn tại");
            }

            // Validate parent project if specified
            if (request.ProjectParentId.HasValue)
            {
                var parentProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == request.ProjectParentId.Value && p.IsActive);

                if (parentProject == null)
                {
                    throw new ArgumentException("Dự án cha không tồn tại");
                }

                // Check access to parent project
                await CheckProjectAccessAsync(request.ProjectParentId.Value, currentUserId);
            }

            var project = new Project
            {
                ProjectParentId = request.ProjectParentId,
                ProjectCode = projectCode,
                ProjectName = request.ProjectName,
                Description = request.Description ?? "",
                Status = request.ProjectStatus,
                Priority = request.Priority,
                ProjectManagerId = request.ProjectManagerId,
                ClientId = request.ClientId ?? null,
                ClientName = request.ClientName ?? "",
                StartDate = request.StartDate,
                PlannedEndDate = request.PlannedEndDate,
                ActualEndDate = request.ActualEndDate,
                EstimatedHours = (int)(request.EstimatedHours * 60),
                CompletionPercentage = request.Progress ?? 0,
                IsActive = true,
                IsPublic = request.IsPublic,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            // Set tags if provided
            if (request.Tags?.Count > 0)
            {
                project.SetTags(request.Tags);
            }

            _context.Projects.Add(project);

            await _context.SaveChangesAsync();

            // Add project manager as member
            var projectMember = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = request.ProjectManagerId,
                ProjectRole = UserRole.Manager,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                AllocationPercentage = 100,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectMembers.Add(projectMember);

            if (request.ProjectManagerId != currentUserId)
            {
                // Add creator as member if different from project manager
                var projectMember1 = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = currentUserId,
                    ProjectRole = UserRole.Manager,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                    AllocationPercentage = 100,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProjectMembers.Add(projectMember1);
            }

            await _context.SaveChangesAsync();

            return await MapToProjectDtoAsync(project);
        }

        /// <summary>
        /// Cập nhật project
        /// </summary>
        public async Task<ProjectDto?> UpdateProjectAsync(int projectId, UpdateProjectRequest request, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            // Update basic info
            project.ProjectName = request.ProjectName;
            project.Description = request.Description ?? project.Description;
            project.Priority = request.Priority;
            project.Status = request.ProjectStatus;
            project.ClientId = request.ClientId >= 0 ? project.ClientId : (int?)null;
            project.ClientName = request.ClientName ?? project.ClientName;
            project.PlannedEndDate = request.PlannedEndDate ?? project.PlannedEndDate;
            project.ActualEndDate = request.ActualEndDate ?? project.ActualEndDate;
            project.EstimatedHours = (int)(request.EstimatedHours * 60);
            project.CompletionPercentage = request.Progress ?? project.CompletionPercentage;
            project.IsPublic = request.IsPublic;

            // Update tags
            if (request.Tags != null)
            {
                project.SetTags(request.Tags);
            }

            project.MarkAsUpdated(currentUserId);


            bool isLog = CompareProjectsString(project, request, out string oldString, out string newString);

            // Ghi lại thông tin khi thay đổi project
            var log = new AuditLog
            {
                UserId = currentUserId,
                EntityType = EntityTypeManager.Project,
                EntityId = projectId,
                Action = AuditAction.Update,
                OldValues = oldString,
                NewValues = newString,
            };
            log.MarkAsUpdated(currentUserId);

            await _context.SaveChangesAsync();
            return await MapToProjectDtoAsync(project);
        }

        /// <summary>
        /// Xóa project (soft delete)
        /// </summary>
        public async Task<bool> DeleteProjectAsync(int projectId, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return false;

            var projectParents = await _context.Projects
                .Where(p => p.ProjectParentId == projectId && p.IsActive && !p.IsDeleted)
                .ToListAsync();

            if (projectParents != null && projectParents.Count > 0)
            {
                throw new InvalidOperationException("Không thể xóa dự án vì có dự án con đang hoạt động. Vui lòng xóa hoặc di chuyển các dự án con trước.");
            }

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            // Cannot delete active projects
            if (project.Status == ProjectStatus.Active)
            {
                throw new InvalidOperationException("Không thể xóa dự án đang hoạt động. Vui lòng hoàn thành hoặc hủy dự án trước.");
            }

            project.SoftDelete(currentUserId, "Deleted by user");
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Bắt đầu project
        /// </summary>
        public async Task<ProjectDto?> StartProjectAsync(int projectId, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            project.StartProject(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectDtoAsync(project);
        }

        /// <summary>
        /// Hoàn thành project
        /// </summary>
        public async Task<ProjectDto?> CompleteProjectAsync(int projectId, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            project.CompleteProject(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectDtoAsync(project);
        }

        /// <summary>
        /// Pause project
        /// </summary>
        public async Task<ProjectDto?> PauseProjectAsync(int projectId, string reason, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            project.PauseProject(currentUserId, reason);
            await _context.SaveChangesAsync();

            return await MapToProjectDtoAsync(project);
        }

        /// <summary>
        /// Resume project
        /// </summary>
        public async Task<ProjectDto?> ResumeProjectAsync(int projectId, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            project.ResumeProject(currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectDtoAsync(project);
        }

        /// <summary>
        /// Cập nhật tiến độ project
        /// </summary>
        public async Task<ProjectDto?> UpdateProgressAsync(int projectId, UpdateProjectProgressRequest progres, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            // Cap nhat thoi gian thuc te
            project.ActualHours = progres.ActualHours;

            project.UpdateProgress(progres.CompletionPercentage, currentUserId);
            await _context.SaveChangesAsync();

            return await MapToProjectDtoAsync(project);
        }

        //Cập nhật trạng thái project
        public async Task<ProjectDto?> UpdateProjectStatusAsync(int projectId, UpdateProjectStatusRequest request, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            if(project.Status == request.Status)
            {
                return await MapToProjectDtoAsync(project);
            }

            project.Status = request.Status;
            await _context.SaveChangesAsync();

            return await MapToProjectDtoAsync(project);
        }

        /// <summary>
        /// Lấy thành viên của project
        /// </summary>
        public async Task<List<ProjectMemberDto>> GetProjectMembersAsync(int projectId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);

            var members = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == projectId && pm.IsActive)
                .ToListAsync();

            return members.Select(MapToProjectMemberDto).ToList();
        }

        /// <summary>
        /// Lấy thanh vien theo Id
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="memberId"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        public async Task<ProjectMemberDto?> GetProjectMemberByIdAsync(int projectId, int memberId, int currentUserId)
        {
            await CheckProjectAccessAsync(projectId, currentUserId);
            var member = await _context.ProjectMembers
                .Include(pm => pm.User)
                .FirstOrDefaultAsync(pm => pm.UserId == memberId && pm.ProjectId == projectId);
            if (member == null)
                return null;
            return MapToProjectMemberDto(member);
        }

        /// <summary>
        /// Thêm thành viên vào project
        /// </summary>
        public async Task<ProjectMemberDto> AddProjectMemberAsync(int projectId, CreateProjectMemberRequest request, int currentUserId)
        {
            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            // Check if user already a member
            // Note: allow re-adding inactive members
            //       Nếu đã tồn tại nhưng inactive thì có thể thêm lại
            var existingMember = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == request.UserId);

            // Nếu đã tồn tại trả về đã tồn tại,
            // nếu đã tồn tại nhưng iaActive false thì bật isActive lên
            if (existingMember != null && existingMember.IsActive)
            {
                throw new ArgumentException("Người dùng đã là thành viên của dự án");
            }
            else if (existingMember != null && !existingMember.IsActive)
            {
                // Khôi phục member đã inactive
                existingMember.RejoinProject(currentUserId);
                await _context.SaveChangesAsync();
                return MapToProjectMemberDto(existingMember);
            }

            // Validate user exists
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null || !user.IsActive)
            {
                throw new ArgumentException("Người dùng không tồn tại hoặc không hoạt động");
            }

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = request.UserId,
                ProjectRole = request.ProjectRole,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                AllocationPercentage = request.AllocationPercentage,
                HourlyRate = request.HourlyRate,
                Notes = request.Notes ?? "",
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

            // Reload with user info
            member = await _context.ProjectMembers
                .Include(pm => pm.User)
                .FirstAsync(pm => pm.Id == member.Id);

            return MapToProjectMemberDto(member);
        }

        /// <summary>
        /// Cập nhật thành viên project
        /// </summary>
        public async Task<ProjectMemberDto?> UpdateProjectMemberAsync(
            int projectId, int memberId, UpdateProjectMemberRequest request, int currentUserId)
        {
            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            var member = await _context.ProjectMembers
                .Include(pm => pm.User)
                .FirstOrDefaultAsync(pm => pm.Id == memberId && pm.ProjectId == projectId);

            // Check RowVersion của request với entity trong DB để tránh xung đột
            if (member == null || !member.RowVersion.SequenceEqual(request.RowVersion))
                return null;

            member.ProjectRole = request.ProjectRole;
            member.AllocationPercentage = request.AllocationPercentage;
            member.HourlyRate = request.HourlyRate;
            member.Notes = request.Notes ?? member.Notes;
            member.LeftAt = request.LeftAt ?? member.LeftAt;
            member.MarkAsUpdated(currentUserId);

            await _context.SaveChangesAsync();
            return MapToProjectMemberDto(member);
        }

        /// <summary>
        /// Xóa thành viên khỏi project
        /// </summary>
        public async Task<bool> RemoveProjectMemberAsync(int projectId, int memberId, int currentUserId)
        {
            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.UserId == memberId && pm.ProjectId == projectId && pm.IsActive == true);

            if (member == null)
                return false;

            member.LeaveProject(currentUserId);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Khoi phục thành viên đã rời project
        /// </summary>
        public async Task<ProjectMemberDto> RecoverProjectMemberAsync(int projectId, int memberId, int currentUserId)
        {
            await CheckProjectManagePermissionAsync(projectId, currentUserId);

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.Id == memberId && pm.ProjectId == projectId && pm.IsActive == false);
            if (member == null)
                return null;

            member.RejoinProject(currentUserId);
            await _context.SaveChangesAsync();
            return MapToProjectMemberDto(member);
        }

        /// <summary>
        /// Lấy summary của project
        /// </summary>
        public async Task<ProjectSummaryDto?> GetProjectSummaryAsync(int projectId, int currentUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            await CheckProjectAccessAsync(projectId, currentUserId);

            var totalTasks = await _context.ProjectTasks
                .CountAsync(pt => pt.ProjectId == projectId && pt.IsActive);

            var completedTasks = await _context.ProjectTasks
                .CountAsync(pt => pt.ProjectId == projectId && pt.IsActive &&
                           pt.Status == ManagementFile.Contracts.Enums.TaskStatuss.Completed);

            var totalMembers = await _context.ProjectMembers
                .CountAsync(pm => pm.ProjectId == projectId && pm.IsActive);

            var totalFiles = project.GetTotalFiles();
            var totalFileSize = project.GetTotalFileSize();

            return new ProjectSummaryDto
            {
                ProjectId = projectId,
                ProjectName = project.ProjectName,
                ProjectCode = project.ProjectCode,
                Status = project.Status,
                CompletionPercentage = project.CompletionPercentage,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                TotalMembers = totalMembers,
                TotalFiles = totalFiles,
                TotalFileSize = totalFileSize,
                EstimatedBudget = project.EstimatedBudget,
                ActualBudget = project.ActualBudget,
                EstimatedHours = Math.Round((decimal)(project.EstimatedHours / 60.0), 2),
                ActualHours = Math.Round((decimal)(project.ActualHours / 60.0), 2),
                StartDate = project.StartDate,
                PlannedEndDate = project.PlannedEndDate,
                ActualEndDate = project.ActualEndDate,
                IsOverdue = project.IsOverdue,
                DaysRemaining = project.PlannedEndDate.HasValue ?
                    (project.PlannedEndDate.Value - DateTime.UtcNow).Days : (int?)null
            };
        }

        #region Private Helper Methods

        private IQueryable<Project> ApplySorting(IQueryable<Project> query, string sortBy, string sortDirection)
        {
            var ascending = sortDirection?.ToLower() != "desc";

            return sortBy?.ToLower() switch
            {
                "projectname" => ascending ? query.OrderBy(p => p.ProjectName) : query.OrderByDescending(p => p.ProjectName),
                "projectcode" => ascending ? query.OrderBy(p => p.ProjectCode) : query.OrderByDescending(p => p.ProjectCode),
                "status" => ascending ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
                "priority" => ascending ? query.OrderBy(p => p.Priority) : query.OrderByDescending(p => p.Priority),
                "startdate" => ascending ? query.OrderBy(p => p.StartDate) : query.OrderByDescending(p => p.StartDate),
                "plannedenddate" => ascending ? query.OrderBy(p => p.PlannedEndDate) : query.OrderByDescending(p => p.PlannedEndDate),
                "updatedat" => ascending ? query.OrderBy(p => p.UpdatedAt) : query.OrderByDescending(p => p.UpdatedAt),
                _ => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
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

        private async Task CheckProjectManagePermissionAsync(int projectId, int currentUserId)
        {
            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role == UserRole.Admin)
                return;

            var project = await _context.Projects.FindAsync(projectId);
            if (project?.ProjectManagerId == currentUserId)
                return;

            var isManager = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId &&
                              pm.IsActive && pm.ProjectRole == UserRole.Manager);

            if (!isManager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền quản lý dự án này");
            }
        }

        private async Task<ProjectDto> MapToProjectDtoAsync(Project project)
        {
            var projectManager = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == project.ProjectManagerId);

            // ✅ Load parent project name if exists
            string parentProjectName = null;
            if (project.ProjectParentId.HasValue)
            {
                parentProjectName = await _context.Projects
                    .Where(p => p.Id == project.ProjectParentId.Value)
                    .Select(p => p.ProjectName)
                    .FirstOrDefaultAsync();
            }

            var countTasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == project.Id && t.IsActive)
                .CountAsync();
            var completedTasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == project.Id && t.IsActive &&
                            t.Status == TaskStatuss.Completed)
                .CountAsync();

            var totalMembers = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.Id && pm.IsActive)
                .CountAsync();

            var childCount = await _context.Projects
                .Where(p => p.ProjectParentId == project.Id && p.IsActive)
                .CountAsync();


            return new ProjectDto
            {
                Id = project.Id,
                ProjectCode = project.ProjectCode,
                ProjectName = project.ProjectName,
                Description = project.Description,
                Status = project.Status,
                Priority = project.Priority,
                ProjectManagerId = project.ProjectManagerId,
                ProjectManagerName = projectManager?.FullName ?? "",
                ClientId = project.ClientId,
                ClientName = project.ClientName,
                StartDate = project.StartDate,
                PlannedEndDate = project.PlannedEndDate,
                ActualEndDate = project.ActualEndDate,
                EstimatedBudget = project.EstimatedBudget,
                ActualBudget = project.ActualBudget,
                EstimatedHours = Math.Round((decimal)(project.EstimatedHours / 60.0), 2),
                ActualHours = Math.Round((decimal)(project.ActualHours / 60.0), 2),
                CompletionPercentage = project.CompletionPercentage,
                IsActive = project.IsActive,
                IsPublic = project.IsPublic,
                Tags = project.GetTags(),
                CreatedAt = project.CreatedAt,
                CreatedBy = project.CreatedBy,
                UpdatedAt = project.UpdatedAt,
                UpdatedBy = project.UpdatedBy,
                IsOverdue = project.IsOverdue,
                IsCompleted = project.IsCompleted,
                BudgetVariance = project.BudgetVariance,
                HourVariance = project.HourVariance,
                TotalTasks = countTasks > 0 ? countTasks : 0,
                CompletedTasks = completedTasks > 0 ? completedTasks : 0,
                TotalMembers = totalMembers > 0 ? totalMembers : 0,
                TotalChildCount = childCount,

                // ✅ Hierarchy properties
                ProjectParentId = project.ProjectParentId,
                ProjectParentName = parentProjectName,
            };
        }

        private async Task<List<ProjectDto>> MapToProjectDtosAsync(List<Project> projects)
        {
            var result = new List<ProjectDto>();
            foreach (var project in projects)
            {
                result.Add(await MapToProjectDtoAsync(project));
            }
            return result;
        }

        private static ProjectMemberDto MapToProjectMemberDto(ProjectMember member)
        {
            return new ProjectMemberDto
            {
                Id = member.Id,
                ProjectId = member.ProjectId,
                UserId = member.UserId,
                UserName = member.User?.Username ?? "",
                FullName = member.User?.FullName ?? "",
                Email = member.User?.Email ?? "",
                ProjectRole = member.ProjectRole,
                JoinedAt = member.JoinedAt,
                LeftAt = member.LeftAt,
                IsActive = member.IsActive,
                AllocationPercentage = member.AllocationPercentage,
                HourlyRate = member.HourlyRate,
                Notes = member.Notes,
                CreatedAt = member.CreatedAt,
                UpdatedAt = member.UpdatedAt,
                RowVersion = member.RowVersion
            };
        }

        /// <summary>
        /// So sanh thay doi cua Project
        /// </summary>
        /// <param name="oldProject"></param>
        /// <param name="newProject"></param>
        /// <param name="oldString"></param>
        /// <param name="newString"></param>
        private static bool CompareProjectsString(Project oldProject, UpdateProjectRequest newProject, out string oldString, out string newString)
        {
            bool res = false;

            var changesOld = new List<string>();
            var changesNew = new List<string>();

            // Update basic info
            if (oldProject.ProjectName != newProject.ProjectName)
            {
                res = true;
                changesOld.Add($"ProjectName: {oldProject.ProjectName}");
                changesNew.Add($"ProjectName: {newProject.ProjectName}");
            }

            if (oldProject.Description != newProject.Description)
            {
                res = true;
                changesOld.Add($"Description: {oldProject.Description}");
                changesNew.Add($"Description: {newProject.Description}");
            }

            if (oldProject.Priority != newProject.Priority)
            {
                res = true;
                changesOld.Add($"Priority: {oldProject.Priority.GetDisplayName()}");
                changesNew.Add($"Priority: {newProject.Priority.GetDisplayName()}");
            }

            if (oldProject.ClientId != newProject.ClientId)
            {
                res = true;
                changesOld.Add($"ClientId: {oldProject.ClientId}");
                changesNew.Add($"ClientId: {newProject.ClientId}");
            }

            if (oldProject.ClientName != newProject.ClientName)
            {
                res = true;
                changesOld.Add($"ClientName: {oldProject.ClientName}");
                changesNew.Add($"ClientName: {newProject.ClientName}");
            }

            if (oldProject.PlannedEndDate != newProject.PlannedEndDate)
            {
                res = true;
                changesOld.Add($"PlannedEndDate: {oldProject.PlannedEndDate}");
                changesNew.Add($"PlannedEndDate: {newProject.PlannedEndDate}");
            }

            if( oldProject.ActualEndDate != newProject.ActualEndDate)
            {
                res = true;
                changesOld.Add($"ActualEndDate: {oldProject.ActualEndDate}");
                changesNew.Add($"ActualEndDate: {newProject.ActualEndDate}");
            }

            if (oldProject.EstimatedHours != newProject.EstimatedHours)
            {
                res = true;
                changesOld.Add($"EstimatedHours: {oldProject.EstimatedHours}");
                changesNew.Add($"EstimatedHours: {newProject.EstimatedHours}");
            }

            if( oldProject.CompletionPercentage != newProject.Progress)
            {
                res = true;
                changesOld.Add($"Progress: {oldProject.CompletionPercentage}");
                changesNew.Add($"Progress: {newProject.Progress}");
            }

            if (oldProject.IsPublic != newProject.IsPublic)
            {
                res = true;
                changesOld.Add($"IsPublic: {oldProject.IsPublic.ToString()}");
                changesNew.Add($"IsPublic: {newProject.IsPublic.ToString()}");
            }

            if (res)
            {
                oldString = string.Join("; ", changesOld);
                newString = string.Join("; ", changesNew);
            }
            else
            {
                oldString = "Không làm gì!";
                newString = "Không làm gì!";
            }


            return res;
        }

        #endregion
    }


}