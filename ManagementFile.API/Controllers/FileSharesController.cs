
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
    /// FileShares API Controller  
    /// Manages file sharing operations
    /// </summary>
    [ApiController]
    [Authorize]
    public class FileSharesController : ControllerBase
    {
        private readonly IFileShareService _shareService;
        private readonly ILogger<FileSharesController> _logger;
        private readonly ManagementFileDbContext _context;

        public FileSharesController(IFileShareService shareService, ILogger<FileSharesController> logger, ManagementFileDbContext context)
        {
            _shareService = shareService;
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

        private string GetClientIP()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        }

        private string GetUserAgent()
        {
            return HttpContext.Request.Headers["User-Agent"].ToString();
        }

        #region File Shares

        /// <summary>
        /// List file shares
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>List of shares for the file</returns>
        [HttpGet("api/files/{fileId}/shares")]
        public async Task<ActionResult<ApiResponse<List<FileShareDto>>>> GetFileShares(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _shareService.GetFileSharesAsync(fileId, userId);

                return Ok(new ApiResponse<List<FileShareDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "File shares retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shares for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<List<FileShareDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create new file share
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="request">Share creation request</param>
        /// <returns>Created share details</returns>
        [HttpPost("api/files/{fileId}/shares")]
        public async Task<ActionResult<ApiResponse<FileShareDto>>> CreateFileShare(
            int fileId,
            [FromBody] CreateFileShareRequest request)
        {
            try
            {
                request.FileId = fileId; // Ensure consistency
                var userId = GetCurrentUserId();
                var result = await _shareService.CreateShareAsync(request, userId);

                return Created($"api/shares/{result.Id}", new ApiResponse<FileShareDto>
                {
                    Success = true,
                    Data = result,
                    Message = "File share created successfully",
                    StatusCode = 201
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<FileShareDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating share for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<FileShareDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Share Management

        /// <summary>
        /// Access shared file by token (public endpoint)
        /// </summary>
        /// <param name="shareToken">Share token</param>
        /// <returns>Shared file access details</returns>
        [HttpGet("api/shares/{shareToken}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> AccessSharedFile(string shareToken)
        {
            try
            {
                var request = new ShareAccessRequest { ShareToken = shareToken };
                var result = await _shareService.AccessSharedFileAsync(
                    request, 
                    GetCurrentUserId(),
                    GetClientIP(), 
                    GetUserAgent());

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = result,
                    Message = "Shared file accessed successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
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
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 401
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing shared file {ShareToken}", shareToken);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update share settings
        /// </summary>
        /// <param name="shareId">Share ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated share details</returns>
        [HttpPut("api/shares/{shareId}")]
        public async Task<ActionResult<ApiResponse<FileShareDto>>> UpdateShare(
            int shareId,
            [FromBody] UpdateFileShareRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _shareService.UpdateShareAsync(shareId, request, userId);

                return Ok(new ApiResponse<FileShareDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Share updated successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<FileShareDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating share {ShareId}", shareId);
                return StatusCode(500, new ApiResponse<FileShareDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Revoke share (delete)
        /// </summary>
        /// <param name="shareId">Share ID</param>
        /// <returns>Revocation confirmation</returns>
        [HttpDelete("api/shares/{shareId}")]
        public async Task<ActionResult<ApiResponse<object>>> RevokeShare(int shareId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _shareService.DeleteShareAsync(shareId, userId);

                if (!success)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Share not found or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Share revoked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share {ShareId}", shareId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Share Analytics & Utilities

        /// <summary>
        /// Generate QR code for share
        /// </summary>
        /// <param name="shareId">Share ID</param>
        /// <returns>QR code data</returns>
        [HttpGet("api/shares/{shareId}/qr-code")]
        public async Task<ActionResult<ApiResponse<string>>> GenerateQRCode(int shareId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _shareService.GenerateQRCodeAsync(shareId, userId);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = result,
                    Message = "QR code generated successfully"
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
                _logger.LogError(ex, "Error generating QR code for share {ShareId}", shareId);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get share access analytics
        /// </summary>
        /// <param name="shareId">Share ID</param>
        /// <returns>Analytics data</returns>
        [HttpGet("api/shares/{shareId}/analytics")]
        public async Task<ActionResult<ApiResponse<ShareAnalyticsDto>>> GetShareAnalytics(int shareId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _shareService.GetShareAnalyticsAsync(shareId, userId);

                return Ok(new ApiResponse<ShareAnalyticsDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Share analytics retrieved successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<ShareAnalyticsDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics for share {ShareId}", shareId);
                return StatusCode(500, new ApiResponse<ShareAnalyticsDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Record share access (for tracking)
        /// </summary>
        /// <param name="shareToken">Share token</param>
        /// <param name="request">Access recording request</param>
        /// <returns>Recording confirmation</returns>
        [HttpPost("api/shares/{shareToken}/access")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> RecordShareAccess(
            string shareToken,
            [FromBody] ShareAccessRequest request)
        {
            try
            {
                await _shareService.RecordShareAccessAsync(
                    shareToken,
                    "access",
                    GetCurrentUserId(),
                    GetClientIP(),
                    GetUserAgent());

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Share access recorded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording share access {ShareToken}", shareToken);
                return StatusCode(500, new ApiResponse<object>
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