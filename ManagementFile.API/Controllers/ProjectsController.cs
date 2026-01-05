using ManagementFile.API.Data;
using ManagementFile.API.Services;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectMembers;
using ManagementFile.Contracts.Requests.ProjectManagement.Projects;
using ManagementFile.Models.ProjectManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// Controller quản lý Projects
    /// Enhanced with hierarchy support
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;
        private readonly ManagementFileDbContext _context;

        public ProjectsController(ProjectService projectService, ILogger<ProjectsController> logger, ManagementFileDbContext context)
        {
            _projectService = projectService;
            _logger = logger;
            _context = context;
        }

        #region Basic CRUD Operations

        /// <summary>
        /// Lấy danh sách projects với filter và pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<ProjectDto>>>> GetProjects(
            [FromQuery] ProjectFilterRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _projectService.GetProjectsAsync(request, currentUserId);

                return Ok(ApiResponse<PagedResult<ProjectDto>>.SuccessResult(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects");
                return StatusCode(500, ApiResponse<PagedResult<ProjectDto>>.ErrorResult("Đã xảy ra lỗi khi lấy danh sách dự án"));
            }
        }

        /// <summary>
        /// Lấy thông tin project theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProject(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.GetProjectByIdAsync(id, currentUserId);

                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi lấy thông tin dự án"));
            }
        }

        /// <summary>
        /// Tạo project mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject(CreateProjectRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.CreateProjectAsync(request, currentUserId);

                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, 
                    ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi tạo dự án"));
            }
        }

        /// <summary>
        /// Cập nhật project
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProject(int id, UpdateProjectRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.UpdateProjectAsync(id, request, currentUserId);

                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật dự án"));
            }
        }

        /// <summary>
        /// Xóa project (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProject(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var success = await _projectService.DeleteProjectAsync(id, currentUserId);

                if (!success)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
                return StatusCode(500, ApiResponse<bool>.ErrorResult("Đã xảy ra lỗi khi xóa dự án"));
            }
        }

        #endregion

        #region Hierarchy Operations

        /// <summary>
        /// ✅ NEW: Lấy danh sách children của một project với pagination
        /// GET: api/projects/{parentId}/children?pageNumber=1&pageSize=20
        /// </summary>
        [HttpGet("{parentId}/children")]
        public async Task<ActionResult<ApiResponse<PagedResult<ProjectDto>>>> GetProjectChildren(
            int parentId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _projectService.GetProjectChildrenAsync(
                    parentId, 
                    pageNumber, 
                    pageSize, 
                    currentUserId);

                return Ok(ApiResponse<PagedResult<ProjectDto>>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting children for project {ParentId}", parentId);
                return StatusCode(500, ApiResponse<PagedResult<ProjectDto>>.ErrorResult(
                    "Đã xảy ra lỗi khi lấy danh sách dự án con"));
            }
        }

        /// <summary>
        /// ✅ NEW: Lấy toàn bộ hierarchy tree của một project
        /// GET: api/projects/{id}/hierarchy?maxDepth=3
        /// </summary>
        [HttpGet("{id}/hierarchy")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProjectHierarchy(
            int id,
            [FromQuery] int maxDepth = 3)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var hierarchy = await _projectService.GetProjectHierarchyAsync(id, currentUserId, maxDepth);

                return Ok(ApiResponse<ProjectDto>.SuccessResult(hierarchy));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hierarchy for project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult(
                    "Đã xảy ra lỗi khi lấy cấu trúc phân cấp dự án"));
            }
        }

        /// <summary>
        /// ✅ NEW: Di chuyển project sang parent mới
        /// PUT: api/projects/{id}/move
        /// </summary>
        [HttpPut("{id}/move")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> MoveProject(
            int id,
            [FromBody] MoveProjectRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.MoveProjectAsync(
                    id, 
                    request.NewParentId, 
                    currentUserId);

                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult(
                    "Đã xảy ra lỗi khi di chuyển dự án"));
            }
        }

        #endregion

        #region Project State Management

        /// <summary>
        /// Bắt đầu project
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> StartProject(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.StartProjectAsync(id, currentUserId);

                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi bắt đầu dự án"));
            }
        }

        /// <summary>
        /// Hoàn thành project
        /// </summary>
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CompleteProject(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.CompleteProjectAsync(id, currentUserId);

                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi hoàn thành dự án"));
            }
        }

        /// <summary>
        /// Pause/Resume project
        /// </summary>
        [HttpPost("{id}/pause")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> PauseProject(int id, [FromBody] PauseProjectRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.PauseProjectAsync(id, request.Reason, currentUserId);

                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi tạm dừng dự án"));
            }
        }

        /// <summary>
        /// Resume project
        /// </summary>
        [HttpPost("{id}/resume")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> ResumeProject(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.ResumeProjectAsync(id, currentUserId);

                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi tiếp tục dự án"));
            }
        }

        /// <summary>
        /// Cập nhật tiến độ project
        /// </summary>
        [HttpPut("{id}/progress")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProgress(int id, [FromBody] UpdateProjectProgressRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.UpdateProgressAsync(id, request, currentUserId);

                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project progress {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật tiến độ"));
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProjectStatus(int id, [FromBody] UpdateProjectStatusRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var project = await _projectService.UpdateProjectStatusAsync(id, request, currentUserId);
                if (project == null)
                {
                    return NotFound(ApiResponse<ProjectDto>.ErrorResult("Không tìm thấy dự án"));
                }
                return Ok(ApiResponse<ProjectDto>.SuccessResult(project));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project status {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật trạng thái dự án"));
            }
        }

        #endregion

        #region Project Members Management

        /// <summary>
        /// Lấy thành viên của project
        /// </summary>
        [HttpGet("{id}/members")]
        public async Task<ActionResult<ApiResponse<List<ProjectMemberDto>>>> GetProjectMembers(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var members = await _projectService.GetProjectMembersAsync(id, currentUserId);

                return Ok(ApiResponse<List<ProjectMemberDto>>.SuccessResult(members));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project members {ProjectId}", id);
                return StatusCode(500, ApiResponse<List<ProjectMemberDto>>.ErrorResult("Đã xảy ra lỗi khi lấy danh sách thành viên"));
            }
        }

        /// <summary>
        /// lay thanh vien theo id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="memberId"></param>
        /// <returns></returns>
        [HttpGet("{id}/membersget/{memberId}")]
        public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> GetProjectMember(int id, int memberId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var member = await _projectService.GetProjectMemberByIdAsync(id, memberId, currentUserId);
                if (member == null)
                {
                    return NotFound(ApiResponse<ProjectMemberDto>.ErrorResult("Không tìm thấy thành viên"));
                }
                return Ok(ApiResponse<ProjectMemberDto>.SuccessResult(member));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project member {ProjectId} {MemberId}", id, memberId);
                return StatusCode(500, ApiResponse<ProjectMemberDto>.ErrorResult("Đã xảy ra lỗi khi lấy thông tin thành viên"));
            }
        }

        /// <summary>
        /// Thêm thành viên vào project
        /// </summary>
        [HttpPost("{id}/members")]
        public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> AddProjectMember(int id, CreateProjectMemberRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var member = await _projectService.AddProjectMemberAsync(id, request, currentUserId);

                return Ok(ApiResponse<ProjectMemberDto>.SuccessResult(member));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectMemberDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding project member {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectMemberDto>.ErrorResult("Đã xảy ra lỗi khi thêm thành viên"));
            }
        }

        /// <summary>
        /// Cập nhật thành viên project
        /// </summary>
        [HttpPut("{id}/members/{memberId}")]
        public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> UpdateProjectMember(
            int id, 
            int memberId, 
            UpdateProjectMemberRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var member = await _projectService.UpdateProjectMemberAsync(id, memberId, request, currentUserId);

                if (member == null)
                {
                    return NotFound(ApiResponse<ProjectMemberDto>.ErrorResult("Không tìm thấy thành viên"));
                }

                return Ok(ApiResponse<ProjectMemberDto>.SuccessResult(member));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectMemberDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project member {ProjectId} {MemberId}", id, memberId);
                return StatusCode(500, ApiResponse<ProjectMemberDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật thành viên"));
            }
        }

        /// <summary>
        /// Khôi phục thành viên đã rời project
        /// </summary>
        [HttpPut("{id}/members/{memberId}/recover")]
        public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> RecoverProjectMember(
            int id,
            int memberId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var member = await _projectService.RecoverProjectMemberAsync(id, memberId, currentUserId);
                
                if (member == null)
                {
                    return NotFound(ApiResponse<ProjectMemberDto>.ErrorResult("Không tìm thấy thành viên"));
                }
                
                return Ok(ApiResponse<ProjectMemberDto>.SuccessResult(member));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectMemberDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recovering project member {ProjectId} {MemberId}", id, memberId);
                return StatusCode(500, ApiResponse<ProjectMemberDto>.ErrorResult("Đã xảy ra lỗi khi khôi phục thành viên"));
            }
        }

        /// <summary>
        /// Xóa thành viên khỏi project
        /// </summary>
        [HttpDelete("{id}/members/{memberId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveProjectMember(int id, int memberId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var success = await _projectService.RemoveProjectMemberAsync(id, memberId, currentUserId);

                if (!success)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Không tìm thấy thành viên"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing project member {ProjectId} {MemberId}", id, memberId);
                return StatusCode(500, ApiResponse<bool>.ErrorResult("Đã xảy ra lỗi khi xóa thành viên"));
            }
        }

        #endregion

        #region Project Statistics

        /// <summary>
        /// Lấy summary/statistics của project
        /// </summary>
        [HttpGet("{id}/summary")]
        public async Task<ActionResult<ApiResponse<ProjectSummaryDto>>> GetProjectSummary(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var summary = await _projectService.GetProjectSummaryAsync(id, currentUserId);

                if (summary == null)
                {
                    return NotFound(ApiResponse<ProjectSummaryDto>.ErrorResult("Không tìm thấy dự án"));
                }

                return Ok(ApiResponse<ProjectSummaryDto>.SuccessResult(summary));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project summary {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectSummaryDto>.ErrorResult("Đã xảy ra lỗi khi lấy thống kê dự án"));
            }
        }

        #endregion

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var sessionToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(sessionToken))
                return -1;

            var session = _context.UserSessions
                .FirstOrDefault(s => s.SessionToken == sessionToken && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            return session?.UserId ?? -1;
        }

        #endregion
    }

    #region Request Models

    /// <summary>
    /// Request model for moving project to new parent
    /// </summary>
    public class MoveProjectRequest
    {
        /// <summary>
        /// ID of new parent project (null = move to root)
        /// </summary>
        public int? NewParentId { get; set; }

        /// <summary>
        /// Reason for moving (optional)
        /// </summary>
        public string Reason { get; set; } = "";
    }

    #endregion
}