
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
    /// FileVersions API Controller
    /// Manages file version operations
    /// </summary>
    [ApiController]
    [Route("api/files/{fileId}/versions")]
    [Authorize]
    public class FileVersionsController : ControllerBase
    {
        private readonly IFileVersionService _versionService;
        private readonly ILogger<FileVersionsController> _logger;
        private readonly ManagementFileDbContext _context;

        public FileVersionsController(IFileVersionService versionService, ILogger<FileVersionsController> logger, ManagementFileDbContext context)
        {
            _versionService = versionService;
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
        /// Get version history for file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>List of versions</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<FileVersionDto>>>> GetFileVersions(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _versionService.GetFileVersionsAsync(fileId, userId);

                return Ok(new ApiResponse<List<FileVersionDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "File versions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving versions for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<List<FileVersionDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get specific version details
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="versionId">Version ID</param>
        /// <returns>Version details</returns>
        [HttpGet("{versionId}")]
        public async Task<ActionResult<ApiResponse<FileVersionDto>>> GetVersion(int fileId, int versionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _versionService.GetVersionByIdAsync(versionId, userId);

                if (result == null)
                    return NotFound(new ApiResponse<FileVersionDto>
                    {
                        Success = false,
                        Message = "Version not found or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<FileVersionDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Version retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving version {VersionId}", versionId);
                return StatusCode(500, new ApiResponse<FileVersionDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create new version for file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="file">New file content</param>
        /// <param name="request">Version creation request</param>
        /// <returns>Created version details</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<FileVersionDto>>> CreateVersion(
            int fileId,
            IFormFile file,
            [FromForm] CreateVersionRequest request)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new ApiResponse<FileVersionDto>
                    {
                        Success = false,
                        Message = "No file uploaded",
                        StatusCode = 400
                    });

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
                var result = await _versionService.CreateVersionAsync(fileId, request, upload, userId);

                return Created($"api/files/{fileId}/versions/{result.Id}", new ApiResponse<FileVersionDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Version created successfully",
                    StatusCode = 201
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<FileVersionDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<FileVersionDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating version for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<FileVersionDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Compare two versions of a file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Compare request with version IDs</param>
        /// <returns>Comparison result</returns>
        [HttpGet("compare")]
        public async Task<ActionResult<ApiResponse<object>>> CompareVersions(
            int fileId,
            [FromQuery] CompareVersionsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _versionService.CompareVersionsAsync(fileId, request, userId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = result,
                    Message = "Version comparison completed successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
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
                _logger.LogError(ex, "Error comparing versions for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Download specific version of file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="versionId">Version ID</param>
        /// <returns>Version file content</returns>
        [HttpGet("{versionId}/download")]
        public async Task<IActionResult> DownloadVersion(int fileId, int versionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Get version info first
                var versionInfo = await _versionService.GetVersionByIdAsync(versionId, userId);
                if (versionInfo == null)
                    return NotFound();

                var content = await _versionService.DownloadVersionAsync(versionId, userId);
                
                // Generate filename with version
                var fileName = $"{Path.GetFileNameWithoutExtension(versionInfo.ProjectFileId.ToString())}_v{versionInfo.VersionNumber}{Path.GetExtension(versionInfo.ProjectFileId.ToString())}";
                
                return File(content, "application/octet-stream", fileName);
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
                _logger.LogError(ex, "Error downloading version {VersionId}", versionId);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Restore file to specific version
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="versionId">Version ID to restore to</param>
        /// <returns>Restoration confirmation</returns>
        [HttpPost("{versionId}/restore")]
        public async Task<ActionResult<ApiResponse<object>>> RestoreVersion(int fileId, int versionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _versionService.RestoreVersionAsync(fileId, versionId, userId);

                if (!success)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "File or version not found, or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "File restored to selected version successfully"
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
                _logger.LogError(ex, "Error restoring file {FileId} to version {VersionId}", fileId, versionId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}