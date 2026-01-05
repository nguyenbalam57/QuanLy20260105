
using ManagementFile.API.Data;
using ManagementFile.API.Services;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.FileManagement;
using ManagementFile.Contracts.Requests.FileManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// ProjectFolders API Controller
    /// Manages folder operations within projects
    /// </summary>
    [ApiController]
    [Route("api/projects/{projectId}/folders")]
    [Authorize]
    public class ProjectFoldersController : ControllerBase
    {
        private readonly IProjectFolderService _folderService;
        private readonly ILogger<ProjectFoldersController> _logger;
        private readonly ManagementFileDbContext _context;

        public ProjectFoldersController(IProjectFolderService folderService, ILogger<ProjectFoldersController> logger, ManagementFileDbContext context)
        {
            _folderService = folderService;
            _logger = logger;
            _context = context;
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

        /// <summary>
        /// Get folder tree structure for project
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <returns>List of folders in tree structure</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ProjectFolderDto>>>> GetProjectFolders(int projectId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _folderService.GetProjectFoldersAsync(projectId, userId);

                return Ok(new ApiResponse<List<ProjectFolderDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Folders retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving folders for project {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<List<ProjectFolderDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get specific folder details
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="folderId">Folder ID</param>
        /// <returns>Folder details</returns>
        [HttpGet("{folderId}")]
        public async Task<ActionResult<ApiResponse<ProjectFolderDto>>> GetFolder(int projectId, int folderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _folderService.GetFolderByIdAsync(folderId, userId);

                if (result is null)
                    return NotFound(new ApiResponse<ProjectFolderDto>
                    {
                        Success = false,
                        Message = "Folder not found or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<ProjectFolderDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Folder retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving folder {FolderId}", folderId);
                return StatusCode(500, new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create new folder in project
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="request">Create folder request</param>
        /// <returns>Created folder details</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectFolderDto>>> CreateFolder(
            int projectId,
            [FromBody] CreateProjectFolderRequest request)
        {
            try
            {
                request.ProjectId = projectId; // Ensure consistency
                var userId = GetCurrentUserId();
                var result = await _folderService.CreateFolderAsync(request, userId);

                return Created($"api/folders/{result.Id}", new ApiResponse<ProjectFolderDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Folder created successfully",
                    StatusCode = 201
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder in project {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }

    /// <summary>
    /// Folders API Controller
    /// Manages individual folder operations
    /// </summary>
    [ApiController]
    [Route("api/folders")]
    [Authorize]
    public class FoldersController : ControllerBase
    {
        private readonly IProjectFolderService _folderService;
        private readonly ILogger<FoldersController> _logger;
        private readonly ManagementFileDbContext _context;

        public FoldersController(IProjectFolderService folderService, ILogger<FoldersController> logger, ManagementFileDbContext context)
        {
            _folderService = folderService;
            _logger = logger;
            _context = context;
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

        /// <summary>
        /// Update folder (rename, modify properties)
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated folder details</returns>
        [HttpPut("{folderId}")]
        public async Task<ActionResult<ApiResponse<ProjectFolderDto>>> UpdateFolder(
            int folderId,
            [FromBody] UpdateProjectFolderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _folderService.UpdateFolderAsync(folderId, request, userId);

                return Ok(new ApiResponse<ProjectFolderDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Folder updated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder {FolderId}", folderId);
                return StatusCode(500, new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete folder
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <returns>Deletion confirmation</returns>
        [HttpDelete("{folderId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteFolder(int folderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _folderService.DeleteFolderAsync(folderId, userId);

                if (!success)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Folder not found or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Folder deleted successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder {FolderId}", folderId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get breadcrumb navigation for folder
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <returns>Breadcrumb path</returns>
        [HttpGet("{folderId}/breadcrumb")]
        public async Task<ActionResult<ApiResponse<List<ProjectFolderDto>>>> GetBreadcrumb(int folderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _folderService.GetBreadcrumbAsync(folderId, userId);

                return Ok(new ApiResponse<List<ProjectFolderDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Breadcrumb retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting breadcrumb for folder {FolderId}", folderId);
                return StatusCode(500, new ApiResponse<List<ProjectFolderDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get folder contents (files and subfolders)
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <returns>Folder contents</returns>
        [HttpGet("{folderId}/contents")]
        public async Task<ActionResult<ApiResponse<FolderContentsDto>>> GetFolderContents(int folderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _folderService.GetFolderContentsAsync(folderId, userId);

                return Ok(new ApiResponse<FolderContentsDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Folder contents retrieved successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<FolderContentsDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contents for folder {FolderId}", folderId);
                return StatusCode(500, new ApiResponse<FolderContentsDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create subfolder
        /// </summary>
        /// <param name="folderId">Parent folder ID</param>
        /// <param name="request">Create folder request</param>
        /// <returns>Created subfolder details</returns>
        [HttpPost("{folderId}/subfolders")]
        public async Task<ActionResult<ApiResponse<ProjectFolderDto>>> CreateSubfolder(
            int folderId,
            [FromBody] CreateProjectFolderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _folderService.CreateSubFolderAsync(folderId, request, userId);

                return Created($"api/folders/{result.Id}", new ApiResponse<ProjectFolderDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Subfolder created successfully",
                    StatusCode = 201
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subfolder in {FolderId}", folderId);
                return StatusCode(500, new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Move folder to different parent
        /// </summary>
        /// <param name="folderId">Folder ID to move</param>
        /// <param name="request">Move request</param>
        /// <returns>Moved folder details</returns>
        [HttpPost("{folderId}/move")]
        public async Task<ActionResult<ApiResponse<ProjectFolderDto>>> MoveFolder(
            int folderId,
            [FromBody] MoveFolderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _folderService.MoveFolderAsync(folderId, request, userId);

                return Ok(new ApiResponse<ProjectFolderDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Folder moved successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving folder {FolderId}", folderId);
                return StatusCode(500, new ApiResponse<ProjectFolderDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}