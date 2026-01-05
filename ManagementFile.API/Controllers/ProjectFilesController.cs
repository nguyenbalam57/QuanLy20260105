
using ManagementFile.API.Data;
using ManagementFile.API.Services;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.FileManagement;
using ManagementFile.Contracts.Requests.FileManagement;
using ManagementFile.Contracts.Responses.FileManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// ProjectFiles API Controller
    /// Manages file operations within projects
    /// </summary>
    [ApiController]
    [Route("api/projects/{projectId}/files")]
    [Authorize]
    public class ProjectFilesController : ControllerBase
    {
        private readonly IProjectFileService _fileService;
        private readonly ILogger<ProjectFilesController> _logger;
        private readonly ManagementFileDbContext _context;

        public ProjectFilesController(IProjectFileService fileService, ILogger<ProjectFilesController> logger, ManagementFileDbContext context)
        {
            _fileService = fileService;
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
        /// Get files in a project
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <param name="search">Search term</param>
        /// <param name="folderId">Filter by folder ID</param>
        /// <param name="fileType">Filter by file type</param>
        /// <returns>Paginated list of files</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<ProjectFileDto>>>> GetProjectFiles(
            int projectId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? folderId = -1,
            [FromQuery] string? fileType = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.GetProjectFilesAsync(projectId, userId, page, pageSize, search, folderId, fileType);

                return Ok(new ApiResponse<PagedResponse<ProjectFileDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Files retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for project {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<PagedResponse<ProjectFileDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Upload a new file to project
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="file">File to upload</param>
        /// <param name="request">File metadata</param>
        /// <returns>Created file information</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> UploadFile(
            int projectId,
            IFormFile file,
            [FromForm] CreateProjectFileRequest request)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new ApiResponse<ProjectFileDto>
                    {
                        Success = false,
                        Message = "No file uploaded",
                        StatusCode = 400
                    });

                // Ensure project ID matches
                request.ProjectId = projectId;

                // Convert IFormFile to FileUploadDto
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                var upload = new FileUploadDto
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    Content = memoryStream.ToArray()
                };

                var userId = GetCurrentUserId();
                var result = await _fileService.CreateFileAsync(request, upload, userId);

                return Created($"api/files/{result.Id}", new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File uploaded successfully",
                    StatusCode = 201
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to project {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get file by ID
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <returns>File information</returns>
        [HttpGet("{fileId}")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> GetFile(int projectId, int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.GetFileByIdAsync(fileId, userId);

                if (result == null)
                    return NotFound(new ApiResponse<ProjectFileDto>
                    {
                        Success = false,
                        Message = "File not found or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update file metadata
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated file information</returns>
        [HttpPut("{fileId}")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> UpdateFile(
            int projectId,
            int fileId,
            [FromBody] UpdateProjectFileRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.UpdateFileAsync(fileId, request, userId);

                return Ok(new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File updated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <returns>Deletion confirmation</returns>
        [HttpDelete("{fileId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteFile(int projectId, int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _fileService.DeleteFileAsync(fileId, userId);

                if (!success)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "File not found or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "File deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <returns>File content</returns>
        [HttpGet("{fileId}/download")]
        public async Task<IActionResult> DownloadFile(int projectId, int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Get file info first
                var fileInfo = await _fileService.GetFileByIdAsync(fileId, userId);
                if (fileInfo == null)
                    return NotFound();

                var content = await _fileService.DownloadFileAsync(fileId, userId);
                
                return File(content, fileInfo.MimeType, fileInfo.FileName);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileId}", fileId);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Get file preview URL
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <returns>Preview URL</returns>
        [HttpGet("{fileId}/preview")]
        public async Task<ActionResult<ApiResponse<string>>> GetPreviewUrl(int projectId, int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var previewUrl = await _fileService.GetPreviewUrlAsync(fileId, userId);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = previewUrl,
                    Message = "Preview URL retrieved successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preview URL for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Copy file
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Copy request</param>
        /// <returns>Copied file information</returns>
        [HttpPost("{fileId}/copy")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> CopyFile(
            int projectId,
            int fileId,
            [FromBody] CopyFileRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.CopyFileAsync(fileId, request, userId);

                return Created($"api/files/{result.Id}", new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File copied successfully",
                    StatusCode = 201
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Move file to different folder
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Move request</param>
        /// <returns>Moved file information</returns>
        [HttpPost("{fileId}/move")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> MoveFile(
            int projectId,
            int fileId,
            [FromBody] MoveFileRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.MoveFileAsync(fileId, request, userId);

                return Ok(new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File moved successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Checkout file for editing
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Checkout request</param>
        /// <returns>File information</returns>
        [HttpPost("{fileId}/checkout")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> CheckoutFile(
            int projectId,
            int fileId,
            [FromBody] CheckoutRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.CheckoutFileAsync(fileId, request, userId);

                return Ok(new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File checked out successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Checkin file after editing
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="file">New file content (optional)</param>
        /// <param name="notes">Version notes</param>
        /// <returns>File information</returns>
        [HttpPost("{fileId}/checkin")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> CheckinFile(
            int projectId,
            int fileId,
            IFormFile? file = null,
            [FromForm] string notes = "")
        {
            try
            {
                var userId = GetCurrentUserId();
                
                FileUploadDto? upload = null;
                if (file != null && file.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);

                    upload = new FileUploadDto
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        Content = memoryStream.ToArray()
                    };
                }

                var result = await _fileService.CheckinFileAsync(fileId, upload, userId, notes);

                return Ok(new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File checked in successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Force checkin file (admin only)
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <returns>File information</returns>
        [HttpPost("{fileId}/force-checkin")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> ForceCheckinFile(int projectId, int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.ForceCheckinFileAsync(fileId, userId);

                return Ok(new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File force checked in successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force checking in file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Approve file
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Approval request</param>
        /// <returns>File information</returns>
        [HttpPost("{fileId}/approve")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> ApproveFile(
            int projectId,
            int fileId,
            [FromBody] ApprovalRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.ApproveFileAsync(fileId, request, userId);

                return Ok(new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File approved successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Reject file
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Rejection request</param>
        /// <returns>File information</returns>
        [HttpPost("{fileId}/reject")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> RejectFile(
            int projectId,
            int fileId,
            [FromBody] ApprovalRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.RejectFileAsync(fileId, request, userId);

                return Ok(new ApiResponse<ProjectFileDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File rejected successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<ProjectFileDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get file statistics
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <returns>File statistics</returns>
        [HttpGet("{fileId}/stats")]
        public async Task<ActionResult<ApiResponse<FileStatsDto>>> GetFileStats(int projectId, int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.GetFileStatsAsync(fileId, userId);

                return Ok(new ApiResponse<FileStatsDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File statistics retrieved successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<FileStatsDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<FileStatsDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}