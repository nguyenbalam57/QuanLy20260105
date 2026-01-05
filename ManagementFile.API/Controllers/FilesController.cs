
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
    /// Files API Controller
    /// Manages individual file operations (versions, permissions, shares, comments)
    /// </summary>
    [ApiController]
    [Route("api/files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IProjectFileService _fileService;
        private readonly IFilePermissionService _permissionService;
        private readonly ILogger<FilesController> _logger;
        private readonly ManagementFileDbContext _context;

        public FilesController(
            IProjectFileService fileService, 
            IFilePermissionService permissionService,
            ILogger<FilesController> logger,
            ManagementFileDbContext context)
        {
            _fileService = fileService;
            _permissionService = permissionService;
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

        #region File Operations

        /// <summary>
        /// Get file by ID
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File information</returns>
        [HttpGet("{fileId}")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> GetFile(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _fileService.GetFileByIdAsync(fileId, userId);

                if (result is null)
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
        /// Update file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated file information</returns>
        [HttpPut("{fileId}")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> UpdateFile(
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
            catch (UnauthorizedAccessException)
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
        /// <param name="fileId">File ID</param>
        /// <returns>Deletion confirmation</returns>
        [HttpDelete("{fileId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteFile(int fileId)
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
        /// <param name="fileId">File ID</param>
        /// <returns>File content</returns>
        [HttpGet("{fileId}/download")]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var fileInfo = await _fileService.GetFileByIdAsync(fileId, userId);
                if (fileInfo is null)
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

        #endregion

        #region Permissions

        /// <summary>
        /// Get file permissions
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>List of permissions</returns>
        [HttpGet("{fileId}/permissions")]
        public async Task<ActionResult<ApiResponse<List<FilePermissionDto>>>> GetFilePermissions(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await _permissionService.HasPermissionAsync(userId, fileId, "managepermissions"))
                    return Forbid();

                var result = await _permissionService.GetFilePermissionsAsync(fileId);

                return Ok(new ApiResponse<List<FilePermissionDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "File permissions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<List<FilePermissionDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Grant file permission
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Grant permission request</param>
        /// <returns>Created permission</returns>
        [HttpPost("{fileId}/permissions")]
        public async Task<ActionResult<ApiResponse<FilePermissionDto>>> GrantPermission(
            int fileId,
            [FromBody] GrantPermissionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await _permissionService.HasPermissionAsync(userId, fileId, "managepermissions"))
                    return Forbid();

                request.FileId = fileId; // Ensure consistency

                var permission = await _permissionService.GrantPermissionAsync(
                    fileId,
                    request.UserId,
                    request.RoleName,
                    request.Level,
                    userId,
                    request.ExpiresAt,
                    request.Reason);

                var result = new FilePermissionDto
                {
                    Id = permission.Id,
                    ProjectFileId = permission.ProjectFileId,
                    UserId = permission.UserId,
                    RoleName = permission.RoleName,
                    PermissionType = permission.PermissionType,
                    CanRead = permission.CanRead,
                    CanWrite = permission.CanWrite,
                    CanDelete = permission.CanDelete,
                    CanShare = permission.CanShare,
                    CanManagePermissions = permission.CanManagePermissions,
                    CanDownload = permission.CanDownload,
                    CanPrint = permission.CanPrint,
                    CanComment = permission.CanComment,
                    CanCheckout = permission.CanCheckout,
                    CanApprove = permission.CanApprove,
                    IsActive = permission.IsActive,
                    ExpiresAt = permission.ExpiresAt,
                    GrantedBy = permission.GrantedBy,
                    GrantedAt = permission.GrantedAt,
                    PermissionLevel = permission.GetPermissionLevel(),
                    IsExpired = permission.IsExpired,
                    IsRevoked = permission.IsRevoked,
                    IsEffective = permission.IsEffective
                };

                return Created($"api/files/{fileId}/permissions/{permission.Id}", new ApiResponse<FilePermissionDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Permission granted successfully",
                    StatusCode = 201
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<FilePermissionDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<FilePermissionDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get effective permissions for current user
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>Effective permissions</returns>
        [HttpGet("{fileId}/permissions/effective")]
        public async Task<ActionResult<ApiResponse<FilePermissionResult>>> GetEffectivePermissions(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _permissionService.GetEffectivePermissionsAsync(userId, fileId);

                return Ok(new ApiResponse<FilePermissionResult>
                {
                    Success = true,
                    Data = result,
                    Message = "Effective permissions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving effective permissions for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<FilePermissionResult>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Check specific permission
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="permission">Permission to check</param>
        /// <returns>Permission check result</returns>
        [HttpPost("{fileId}/permissions/check")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckPermission(
            int fileId,
            [FromBody] string permission)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _permissionService.HasPermissionAsync(userId, fileId, permission);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = $"Permission check for '{permission}' completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for file {FileId}", permission, fileId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Bulk grant permissions to multiple files
        /// </summary>
        /// <param name="request">Bulk grant request</param>
        /// <returns>List of created permissions</returns>
        [HttpPost("permissions/bulk-grant")]
        public async Task<ActionResult<ApiResponse<List<FilePermissionDto>>>> BulkGrantPermissions(
            [FromBody] BulkGrantPermissionsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Validate user has manage permissions on all files
                foreach (var fileId in request.FileIds)
                {
                    if (!await _permissionService.HasPermissionAsync(userId, fileId, "managepermissions"))
                    {
                        return Forbid();
                    }
                }

                var permissions = await _permissionService.BulkGrantPermissionsAsync(
                    request.FileIds,
                    request.UserId,
                    request.RoleName,
                    request.Level,
                    userId,
                    request.ExpiresAt,
                    request.Reason);

                var result = permissions.Select(p => new FilePermissionDto
                {
                    Id = p.Id,
                    ProjectFileId = p.ProjectFileId,
                    UserId = p.UserId,
                    RoleName = p.RoleName,
                    PermissionType = p.PermissionType,
                    CanRead = p.CanRead,
                    CanWrite = p.CanWrite,
                    CanDelete = p.CanDelete,
                    CanShare = p.CanShare,
                    CanManagePermissions = p.CanManagePermissions,
                    CanDownload = p.CanDownload,
                    CanPrint = p.CanPrint,
                    CanComment = p.CanComment,
                    CanCheckout = p.CanCheckout,
                    CanApprove = p.CanApprove,
                    IsActive = p.IsActive,
                    ExpiresAt = p.ExpiresAt,
                    GrantedBy = p.GrantedBy,
                    GrantedAt = p.GrantedAt,
                    PermissionLevel = p.GetPermissionLevel(),
                    IsExpired = p.IsExpired,
                    IsRevoked = p.IsRevoked,
                    IsEffective = p.IsEffective
                }).ToList();

                return Ok(new ApiResponse<List<FilePermissionDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Permissions granted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk granting permissions");
                return StatusCode(500, new ApiResponse<List<FilePermissionDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion
    }
}