using ManagementFile.API.Data;
using ManagementFile.API.Services;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks;
using ManagementFile.Contracts.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using ManagementFile.Contracts.Requests.ProjectManagement.TaskComments;
using ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// Controller quản lý Project Tasks
    /// </summary>
    [ApiController]
    [Route("api/projects/{projectId}/tasks")]
    public class ProjectTasksController : ControllerBase
    {
        private readonly ProjectTaskService _taskService;
        private readonly ILogger<ProjectTasksController> _logger;
        private readonly ManagementFileDbContext _context;

        public ProjectTasksController(ProjectTaskService taskService, ILogger<ProjectTasksController> logger, ManagementFileDbContext context)
        {
            _taskService = taskService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tasks của project
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<ProjectTaskDto>>>> GetTasks(
            int projectId,
            [FromQuery] TaskFilterRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var result = await _taskService.GetTasksAsync(request, currentUserId);
                return Ok(ApiResponse<PagedResult<ProjectTaskDto>>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for project {ProjectId}", projectId);
                return StatusCode(500, ApiResponse<PagedResult<ProjectTaskDto>>.ErrorResult("Đã xảy ra lỗi khi lấy danh sách tasks"));
            }
        }

        /// <summary>
        /// Lấy task theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> GetTask(int projectId, int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var task = await _taskService.GetTaskByIdAsync(projectId, id, currentUserId);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi lấy thông tin task"));
            }
        }

        /// <summary>
        /// Tạo task mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> CreateTask(
            int projectId,
            [FromBody] CreateTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                // Set ProjectId from route
                request.ProjectId = projectId;

                var currentUserId = GetCurrentUserId();
                
                // Enhanced logging for debugging
                _logger.LogInformation("Creating task for project {ProjectId} by user {UserId}", projectId, currentUserId);
                _logger.LogDebug("Task request: {@Request}", request);

                // Validate currentUserId
                if (currentUserId <= 0)
                {
                    _logger.LogWarning("Invalid currentUserId: {UserId} for CreateTask", currentUserId);
                    return Unauthorized(ApiResponse<ProjectTaskDto>.ErrorResult("Session không hợp lệ hoặc đã hết hạn"));
                }

                var task = await _taskService.CreateTaskAsync(projectId, request, currentUserId);

                _logger.LogInformation("Task created successfully with ID {TaskId}", task.Id);

                return CreatedAtAction(
                    nameof(GetTask),
                    new { projectId = projectId, id = task.Id },
                    ApiResponse<ProjectTaskDto>.SuccessResult(task));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access for CreateTask: ProjectId={ProjectId}, UserId={UserId}", 
                    projectId, GetCurrentUserId());
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for CreateTask: {Message}", ex.Message);
                return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task for project {ProjectId}. Request: {@Request}", 
                    projectId, request);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi tạo task"));
            }
        }

        /// <summary>
        /// Cập nhật task
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> UpdateTask(
            int projectId,
            int id,
            [FromBody] UpdateTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var task = await _taskService.UpdateTaskAsync(projectId, id, request, currentUserId);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật task"));
            }
        }

        /// <summary>
        /// Xóa task (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(int projectId, int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _taskService.DeleteTaskAsync(projectId, id, currentUserId);

                if (!result)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true, "Task đã được xóa thành công"));
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
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                return StatusCode(500, ApiResponse<bool>.ErrorResult("Đã xảy ra lỗi khi xóa task"));
            }
        }

        /// <summary>
        /// Bắt đầu task
        /// </summary>
        [HttpPatch("{id}/start")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> StartTask(int projectId, int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var task = await _taskService.StartTaskAsync(projectId, id, currentUserId);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task, "Task đã được bắt đầu"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi bắt đầu task"));
            }
        }

        /// <summary>
        /// Hoàn thành task
        /// </summary>
        [HttpPatch("{id}/complete")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> CompleteTask(int projectId, int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var task = await _taskService.CompleteTaskAsync(projectId, id, currentUserId);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task, "Task đã được hoàn thành"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi hoàn thành task"));
            }
        }

        /// <summary>
        /// Cập nhật tiến độ task
        /// </summary>
        [HttpPatch("{id}/progress")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> UpdateProgress(
            int projectId,
            int id,
            [FromBody] TaskProgressUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var task = await _taskService.UpdateProgressAsync(projectId, id, request, currentUserId);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task, "Tiến độ task đã được cập nhật"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật tiến độ"));
            }
        }

        /// <summary>
        /// Assign task cho user
        /// </summary>
        [HttpPatch("{id}/assign")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> AssignTask(
            int projectId,
            int id,
            [FromBody] TaskAssignmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var task = await _taskService.AssignTaskAsync(projectId, id, request.AssignedToId, currentUserId);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task, "Task đã được assign thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi assign task"));
            }
        }

        /// <summary>
        /// Block task
        /// </summary>
        [HttpPatch("{id}/block")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> BlockTask(
            int projectId,
            int id,
            [FromBody] TaskBlockRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var task = await _taskService.BlockTaskAsync(projectId, id, request.BlockReason, currentUserId);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task, "Task đã được block"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi block task"));
            }
        }

        /// <summary>
        /// Unblock task
        /// </summary>
        [HttpPatch("{id}/unblock")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> UnblockTask(int projectId, int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var task = await _taskService.UnblockTaskAsync(projectId, id, currentUserId);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task, "Task đã được unblock"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi unblock task"));
            }
        }

        #region Task Comments - Extended API Endpoints

        /// <summary>
        /// Lấy comments của task với filtering và pagination
        /// </summary>
        [HttpGet("{id}/comments/paged")]
        public async Task<ActionResult<ApiResponse<PagedResult<TaskCommentDto>>>> GetTaskCommentsPaged(
            int projectId,
            int id,
            [FromQuery] GetTaskCommentsRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<PagedResult<TaskCommentDto>>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();

                // Set TaskId from route parameter
                request.TaskId = id;

                var result = await _taskService.GetTaskCommentsPagedAsync(projectId, request, currentUserId);

                return Ok(ApiResponse<PagedResult<TaskCommentDto>>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged comments for task {TaskId}", id);
                return StatusCode(500, ApiResponse<PagedResult<TaskCommentDto>>.ErrorResult("Đã xảy ra lỗi khi lấy comments"));
            }
        }

        /// <summary>
        /// Lấy comment cụ thể theo ID
        /// </summary>
        [HttpGet("{id}/comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> GetTaskComment(
            int projectId,
            int id,
            int commentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.GetTaskCommentByIdAsync(projectId, id, commentId, currentUserId);

                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }

                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment {CommentId} for task {TaskId}", commentId, id);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi lấy comment"));
            }
        }

        /// <summary>
        /// Tạo comment mới cho task
        /// </summary>
        [HttpPost("{id}/comments")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> CreateTaskComment(
            int projectId,
            int id,
            [FromBody] CreateTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();

                // Validate currentUserId
                if (currentUserId <= 0)
                {
                    _logger.LogWarning("Invalid currentUserId: {UserId} for CreateTaskComment", currentUserId);
                    return Unauthorized(ApiResponse<TaskCommentDto>.ErrorResult("Session không hợp lệ hoặc đã hết hạn"));
                }

                // Set TaskId from route parameter
                request.TaskId = id;

                // Enhanced logging for debugging
                _logger.LogInformation("Creating comment for task {TaskId} in project {ProjectId} by user {UserId}",
                    id, projectId, currentUserId);
                _logger.LogDebug("Comment request: {@Request}", request);

                var comment = await _taskService.CreateTaskCommentAsync(projectId, id, request, currentUserId);

                _logger.LogInformation("Comment created successfully with ID {CommentId} for task {TaskId}",
                    comment.Id, id);

                return CreatedAtAction(
                    nameof(GetTaskComment),
                    new { projectId = projectId, id = id, commentId = comment.Id },
                    ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được tạo thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access for CreateTaskComment: ProjectId={ProjectId}, TaskId={TaskId}, UserId={UserId}",
                    projectId, id, GetCurrentUserId());
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for CreateTaskComment: {Message}", ex.Message);
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment for task {TaskId} in project {ProjectId}. Request: {@Request}",
                    id, projectId, request);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi tạo comment"));
            }
        }

        /// <summary>
        /// Cập nhật comment
        /// </summary>
        [HttpPut("{id}/comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> UpdateTaskComment(
            int projectId,
            int id,
            int commentId,
            [FromBody] UpdateTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.UpdateTaskCommentAsync(projectId, id, commentId, request, currentUserId);

                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }

                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được cập nhật thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật comment"));
            }
        }

        /// <summary>
        /// Xóa comment (soft delete)
        /// </summary>
        [HttpDelete("{id}/comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTaskComment(
            int projectId,
            int id,
            int commentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _taskService.DeleteTaskCommentAsync(projectId, id, commentId, currentUserId);

                if (!result)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Không tìm thấy comment"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true, "Comment đã được xóa thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<bool>.ErrorResult("Đã xảy ra lỗi khi xóa comment"));
            }
        }

        /// <summary>
        /// Resolve comment (đánh dấu đã giải quyết)
        /// </summary>
        [HttpPatch("{id}/comments/{commentId}/resolve")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> ResolveTaskComment(
            int projectId,
            int id,
            int commentId,
            [FromBody] ResolveTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.ResolveTaskCommentAsync(projectId, id, commentId, request, currentUserId);

                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }

                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được đánh dấu là đã giải quyết"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi resolve comment"));
            }
        }

        /// <summary>
        /// Verify comment (xác nhận đã kiểm tra)
        /// </summary>
        [HttpPatch("{id}/comments/{commentId}/verify")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> VerifyTaskComment(
            int projectId,
            int id,
            int commentId,
            [FromBody] VerifyTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.VerifyTaskCommentAsync(projectId, id, commentId, request, currentUserId);

                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }

                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được verify thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi verify comment"));
            }
        }

        /// <summary>
        /// Agree/Disagree với comment (đồng ý hoặc không đồng ý)
        /// </summary>
        [HttpPatch("{id}/comments/{commentId}/agree")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> AgreeTaskComment(
            int projectId,
            int id,
            int commentId,
            [FromBody] AgreeTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.AgreeTaskCommentAsync(projectId, id, commentId, request, currentUserId);

                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }

                var message = request.IsAgreed ? "Đã đồng ý với comment" : "Đã không đồng ý với comment";
                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error agreeing comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi thực hiện agree comment"));
            }
        }

        /// <summary>
        /// Assign comment cho user (giao comment cho người khác)
        /// </summary>
        [HttpPatch("{id}/comments/{commentId}/assign")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> AssignTaskComment(
            int projectId,
            int id,
            int commentId,
            [FromBody] AssignTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.AssignTaskCommentAsync(projectId, id, commentId, request, currentUserId);

                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }

                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được assign thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi assign comment"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="id"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch("{id}/comments/{commentId}/review")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> ReviewTaskComment(
            int projectId,
            int id,
            int commentId,
            [FromBody] ReviewTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }
                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.ReviewTaskCommentAsync(projectId, id, commentId, request, currentUserId);
                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }
                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được review thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi review comment"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="id"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch("{id}/comments/{commentId}/priority")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> PriorityTaskComment(
            int projectId,
            int id,
            int commentId,
            [FromBody] PriorityTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }
                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.PriorityTaskCommentAsync(projectId, id, commentId, request, currentUserId);
                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }
                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được priority thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error priority comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi priority comment"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="id"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch("{id}/comments/{commentId}/block")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> BlockTaskComment(
    int projectId,
    int id,
    int commentId,
    [FromBody] ToggleBlockingTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }
                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.BlockingTaskCommentAsync(projectId, id, commentId, request, currentUserId);
                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }
                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được block thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi thực hiện block comment"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="id"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch("{id}/comments/{commentId}/discussion")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> RequiresDiscussionTaskComment(
    int projectId,
    int id,
    int commentId,
    [FromBody] ToggleDiscussionTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }
                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.DiscussionTaskCommentAsync(projectId, id, commentId, request, currentUserId);
                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }
                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được requires discussion thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requires discussion comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi requires discussion comment"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="id"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch("{id}/comments/{commentId}/status")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> StatusTaskComment(
    int projectId,
    int id,
    int commentId,
    [FromBody] StatusTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }
                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.StatusTaskCommentAsync(projectId, id, commentId, request, currentUserId);
                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }
                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được cập nhật trạng thái thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error status comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật trạng thái comment"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="id"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch("{id}/comments/{commentId}/type")]
        public async Task<ActionResult<ApiResponse<TaskCommentDto>>> CommentTypeTaskComment(
    int projectId,
    int id,
    int commentId,
    [FromBody] CommentTypeTaskCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }
                var currentUserId = GetCurrentUserId();
                var comment = await _taskService.CommentTypeTaskCommentAsync(projectId, id, commentId, request, currentUserId);
                if (comment == null)
                {
                    return NotFound(ApiResponse<TaskCommentDto>.ErrorResult("Không tìm thấy comment"));
                }
                return Ok(ApiResponse<TaskCommentDto>.SuccessResult(comment, "Comment đã được cập nhật loại comment thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TaskCommentDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error commentType comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<TaskCommentDto>.ErrorResult("Đã xảy ra lỗi khi cập nhật loại comment"));
            }
        }


        /// <summary>
        /// Lấy replies của một comment
        /// </summary>
        [HttpGet("{id}/comments/{commentId}/replies")]
        public async Task<ActionResult<ApiResponse<List<TaskCommentDto>>>> GetCommentReplies(
            int projectId,
            int id,
            int commentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var replies = await _taskService.GetCommentRepliesAsync(projectId, id, commentId, currentUserId);

                return Ok(ApiResponse<List<TaskCommentDto>>.SuccessResult(replies));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting replies for comment {CommentId}", commentId);
                return StatusCode(500, ApiResponse<List<TaskCommentDto>>.ErrorResult("Đã xảy ra lỗi khi lấy replies"));
            }
        }

        /// <summary>
        /// Lấy thống kê comments của task
        /// </summary>
        [HttpGet("{id}/comments/stats")]
        public async Task<ActionResult<ApiResponse<TaskCommentStats>>> GetTaskCommentStats(
            int projectId,
            int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var stats = await _taskService.GetTaskCommentStatsAsync(projectId, id, currentUserId);

                return Ok(ApiResponse<TaskCommentStats>.SuccessResult(stats));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment stats for task {TaskId}", id);
                return StatusCode(500, ApiResponse<TaskCommentStats>.ErrorResult("Đã xảy ra lỗi khi lấy thống kê comments"));
            }
        }

        #endregion

        /// <summary>
        /// Lấy time logs của task
        /// </summary>
        [HttpGet("{id}/timelogs")]
        public async Task<ActionResult<ApiResponse<List<TaskTimeLogDto>>>> GetTaskTimeLogs(int projectId, int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var timeLogs = await _taskService.GetTaskTimeLogsAsync(projectId, id, currentUserId);

                return Ok(ApiResponse<List<TaskTimeLogDto>>.SuccessResult(timeLogs));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time logs for task {TaskId}", id);
                return StatusCode(500, ApiResponse<List<TaskTimeLogDto>>.ErrorResult("Đã xảy ra lỗi khi lấy time logs"));
            }
        }

        /// <summary>
        /// Lấy subtasks
        /// </summary>
        [HttpGet("{id}/subtasks")]
        public async Task<ActionResult<ApiResponse<List<ProjectTaskDto>>>> GetSubTasks(int projectId, int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var subtasks = await _taskService.GetSubTasksAsync(projectId, id, currentUserId);

                return Ok(ApiResponse<List<ProjectTaskDto>>.SuccessResult(subtasks));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subtasks for task {TaskId}", id);
                return StatusCode(500, ApiResponse<List<ProjectTaskDto>>.ErrorResult("Đã xảy ra lỗi khi lấy subtasks"));
            }
        }

        #region ✅ NEW: Hierarchy API Endpoints

        /// <summary>
        /// Lấy subtasks với pagination (lazy loading)
        /// </summary>
        [HttpGet("{id}/subtasks/paged")]
        public async Task<ActionResult<ApiResponse<PagedResult<ProjectTaskDto>>>> GetSubTasksPaged(
            int projectId,
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(ApiResponse<PagedResult<ProjectTaskDto>>.ErrorResult("Page phải >= 1"));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(ApiResponse<PagedResult<ProjectTaskDto>>.ErrorResult("PageSize phải từ 1-100"));
                }

                var currentUserId = GetCurrentUserId();
                var result = await _taskService.GetSubTasksPagedAsync(projectId, id, page, pageSize, currentUserId);

                return Ok(ApiResponse<PagedResult<ProjectTaskDto>>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged subtasks for task {TaskId}", id);
                return StatusCode(500, ApiResponse<PagedResult<ProjectTaskDto>>.ErrorResult("Đã xảy ra lỗi khi lấy subtasks"));
            }
        }

        /// <summary>
        /// Lấy full hierarchy tree của task
        /// </summary>
        [HttpGet("{id}/hierarchy")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> GetTaskHierarchy(
            int projectId,
            int id,
            [FromQuery] int maxDepth = 3)
        {
            try
            {
                if (maxDepth < 1 || maxDepth > 10)
                {
                    return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult("MaxDepth phải từ 1-10"));
                }

                var currentUserId = GetCurrentUserId();
                var task = await _taskService.GetTaskHierarchyAsync(projectId, id, currentUserId, maxDepth);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hierarchy for task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi lấy hierarchy"));
            }
        }

        /// <summary>
        /// Di chuyển task sang parent mới
        /// </summary>
        [HttpPatch("{id}/move")]
        public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> MoveTask(
            int projectId,
            int id,
            [FromBody] MoveTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var task = await _taskService.MoveTaskToParentAsync(
                    projectId, id, request.NewParentTaskId, currentUserId, request.Reason);

                if (task == null)
                {
                    return NotFound(ApiResponse<ProjectTaskDto>.ErrorResult("Không tìm thấy task"));
                }

                return Ok(ApiResponse<ProjectTaskDto>.SuccessResult(task, "Task đã được di chuyển thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProjectTaskDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving task {TaskId}", id);
                return StatusCode(500, ApiResponse<ProjectTaskDto>.ErrorResult("Đã xảy ra lỗi khi di chuyển task"));
            }
        }

        /// <summary>
        /// Lấy root tasks (tasks không có parent) với pagination
        /// </summary>
        [HttpGet("root")]
        public async Task<ActionResult<ApiResponse<PagedResult<ProjectTaskDto>>>> GetRootTasks(
            int projectId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(ApiResponse<PagedResult<ProjectTaskDto>>.ErrorResult("Page phải >= 1"));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(ApiResponse<PagedResult<ProjectTaskDto>>.ErrorResult("PageSize phải từ 1-100"));
                }

                var currentUserId = GetCurrentUserId();
                var result = await _taskService.GetRootTasksAsync(projectId, page, pageSize, currentUserId);

                return Ok(ApiResponse<PagedResult<ProjectTaskDto>>.SuccessResult(result));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting root tasks for project {ProjectId}", projectId);
                return StatusCode(500, ApiResponse<PagedResult<ProjectTaskDto>>.ErrorResult("Đã xảy ra lỗi khi lấy root tasks"));
            }
        }

        /// <summary>
        /// Lấy task path (breadcrumb từ root đến current task)
        /// </summary>
        [HttpGet("{id}/path")]
        public async Task<ActionResult<ApiResponse<List<ProjectTaskDto>>>> GetTaskPath(
            int projectId,
            int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var path = await _taskService.GetTaskPathAsync(projectId, id, currentUserId);

                return Ok(ApiResponse<List<ProjectTaskDto>>.SuccessResult(path));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting path for task {TaskId}", id);
                return StatusCode(500, ApiResponse<List<ProjectTaskDto>>.ErrorResult("Đã xảy ra lỗi khi lấy task path"));
            }
        }

        /// <summary>
        /// Bulk move tasks sang parent mới
        /// </summary>
        [HttpPost("bulk-move")]
        public async Task<ActionResult<ApiResponse<BulkMoveTasksResponse>>> BulkMoveTasks(
            int projectId,
            [FromBody] BulkMoveTasksRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<BulkMoveTasksResponse>.ErrorResult("Dữ liệu không hợp lệ", errors));
                }

                var currentUserId = GetCurrentUserId();
                var movedCount = await _taskService.BulkMoveTasksAsync(
                    projectId, request.TaskIds, request.NewParentTaskId, currentUserId, request.Reason);

                var response = new BulkMoveTasksResponse
                {
                    TotalTasks = request.TaskIds?.Count ?? 0,
                    MovedTasks = movedCount,
                    FailedTasks = (request.TaskIds?.Count ?? 0) - movedCount,
                    Message = $"Đã di chuyển {movedCount}/{request.TaskIds?.Count} tasks thành công"
                };

                return Ok(ApiResponse<BulkMoveTasksResponse>.SuccessResult(response, response.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<BulkMoveTasksResponse>.ErrorResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<BulkMoveTasksResponse>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk moving tasks in project {ProjectId}", projectId);
                return StatusCode(500, ApiResponse<BulkMoveTasksResponse>.ErrorResult("Đã xảy ra lỗi khi di chuyển tasks"));
            }
        }

        #endregion

        #region Helper Methods

        private int GetCurrentUserId()
        {
            try
            {
                // Ưu tiên lấy từ HttpContext.Items (được set bởi AuthMiddleware)
                if (HttpContext.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is int userId)
                {
                    _logger.LogDebug("Got UserId from HttpContext.Items: {UserId}", userId);
                    return userId;
                }

                // Fallback: parse từ Authorization header
                var sessionToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                if (string.IsNullOrEmpty(sessionToken))
                {
                    _logger.LogWarning("No session token found in Authorization header");
                    return -1;
                }

                var session = _context.UserSessions
                    .FirstOrDefault(s => s.SessionToken == sessionToken && 
                                       s.IsActive && 
                                       s.ExpiresAt > DateTime.UtcNow);

                if (session == null)
                {
                    _logger.LogWarning("No valid session found for token: {TokenPrefix}...", 
                        sessionToken.Length > 10 ? sessionToken.Substring(0, 10) : sessionToken);
                    return -1;
                }

                _logger.LogDebug("Got UserId from session: {UserId}", session.UserId);
                return session.UserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID");
                return -1;
            }
        }

        #endregion

    }
}