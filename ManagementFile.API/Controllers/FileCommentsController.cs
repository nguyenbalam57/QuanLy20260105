
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
    /// FileComments API Controller
    /// Manages file comment operations
    /// </summary>
    [ApiController]
    [Authorize]
    public class FileCommentsController : ControllerBase
    {
        private readonly IFileCommentService _commentService;
        private readonly ILogger<FileCommentsController> _logger;
        private readonly ManagementFileDbContext _context;

        public FileCommentsController(IFileCommentService commentService, ILogger<FileCommentsController> logger, ManagementFileDbContext context)
        {
            _commentService = commentService;
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


        #region File Comments

        /// <summary>
        /// Get all comments for a file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>List of comments</returns>
        [HttpGet("api/files/{fileId}/comments")]
        public async Task<ActionResult<ApiResponse<List<FileCommentDto>>>> GetFileComments(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _commentService.GetFileCommentsAsync(fileId, userId);

                return Ok(new ApiResponse<List<FileCommentDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "File comments retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comments for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<List<FileCommentDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get unresolved comments for a file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>List of unresolved comments</returns>
        [HttpGet("api/files/{fileId}/comments/unresolved")]
        public async Task<ActionResult<ApiResponse<List<FileCommentDto>>>> GetUnresolvedComments(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _commentService.GetUnresolvedCommentsAsync(fileId, userId);

                return Ok(new ApiResponse<List<FileCommentDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Unresolved comments retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unresolved comments for file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<List<FileCommentDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Version Comments

        /// <summary>
        /// Get comments for specific file version
        /// </summary>
        /// <param name="versionId">Version ID</param>
        /// <returns>List of comments for the version</returns>
        [HttpGet("api/versions/{versionId}/comments")]
        public async Task<ActionResult<ApiResponse<List<FileCommentDto>>>> GetVersionComments(int versionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _commentService.GetVersionCommentsAsync(versionId, userId);

                return Ok(new ApiResponse<List<FileCommentDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Version comments retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comments for version {VersionId}", versionId);
                return StatusCode(500, new ApiResponse<List<FileCommentDto>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Add comment to file version
        /// </summary>
        /// <param name="versionId">Version ID</param>
        /// <param name="request">Comment creation request</param>
        /// <returns>Created comment details</returns>
        [HttpPost("api/versions/{versionId}/comments")]
        public async Task<ActionResult<ApiResponse<FileCommentDto>>> AddVersionComment(
            int versionId,
            [FromBody] CreateFileCommentRequest request)
        {
            try
            {
                request.FileVersionId = versionId; // Ensure consistency
                var userId = GetCurrentUserId();
                var result = await _commentService.CreateCommentAsync(request, userId);

                return Created($"api/comments/{result.Id}", new ApiResponse<FileCommentDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Comment added successfully",
                    StatusCode = 201
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<FileCommentDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to version {VersionId}", versionId);
                return StatusCode(500, new ApiResponse<FileCommentDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Comment Management

        /// <summary>
        /// Get specific comment by ID
        /// </summary>
        /// <param name="commentId">Comment ID</param>
        /// <returns>Comment details</returns>
        [HttpGet("api/comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<FileCommentDto>>> GetComment(int commentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _commentService.GetCommentByIdAsync(commentId, userId);

                if (result == null)
                    return NotFound(new ApiResponse<FileCommentDto>
                    {
                        Success = false,
                        Message = "Comment not found or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<FileCommentDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Comment retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<FileCommentDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update comment
        /// </summary>
        /// <param name="commentId">Comment ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated comment details</returns>
        [HttpPut("api/comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<FileCommentDto>>> UpdateComment(
            int commentId,
            [FromBody] UpdateFileCommentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _commentService.UpdateCommentAsync(commentId, request, userId);

                return Ok(new ApiResponse<FileCommentDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Comment updated successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<FileCommentDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<FileCommentDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete comment
        /// </summary>
        /// <param name="commentId">Comment ID</param>
        /// <returns>Deletion confirmation</returns>
        [HttpDelete("api/comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteComment(int commentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _commentService.DeleteCommentAsync(commentId, userId);

                if (!success)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Comment not found or access denied",
                        StatusCode = 404
                    });

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Comment deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Comment Operations

        /// <summary>
        /// Reply to a comment
        /// </summary>
        /// <param name="commentId">Parent comment ID</param>
        /// <param name="request">Reply creation request</param>
        /// <returns>Created reply details</returns>
        [HttpPost("api/comments/{commentId}/reply")]
        public async Task<ActionResult<ApiResponse<FileCommentDto>>> ReplyToComment(
            int commentId,
            [FromBody] CreateFileCommentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _commentService.ReplyToCommentAsync(commentId, request, userId);

                return Created($"api/comments/{result.Id}", new ApiResponse<FileCommentDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Reply added successfully",
                    StatusCode = 201
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<FileCommentDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<FileCommentDto>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Resolve comment
        /// </summary>
        /// <param name="commentId">Comment ID</param>
        /// <returns>Resolved comment details</returns>
        [HttpPost("api/comments/{commentId}/resolve")]
        public async Task<ActionResult<ApiResponse<FileCommentDto>>> ResolveComment(int commentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _commentService.ResolveCommentAsync(commentId, userId);

                return Ok(new ApiResponse<FileCommentDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Comment resolved successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<FileCommentDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving comment {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<FileCommentDto>
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