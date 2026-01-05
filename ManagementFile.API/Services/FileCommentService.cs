using ManagementFile.API.Data;
using ManagementFile.Contracts.DTOs.FileManagement;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Requests.FileManagement;
using ManagementFile.Contracts.Responses.UserManagement;
using ManagementFile.Models.FileManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Interface for FileComment Service
    /// </summary>
    public interface IFileCommentService
    {
        // Comment operations
        Task<List<FileCommentDto>> GetFileCommentsAsync(int fileId, int userId);
        Task<List<FileCommentDto>> GetVersionCommentsAsync(int versionId, int userId);
        Task<FileCommentDto?> GetCommentByIdAsync(int commentId, int userId);
        Task<FileCommentDto> CreateCommentAsync(CreateFileCommentRequest request, int userId);
        Task<FileCommentDto> UpdateCommentAsync(int commentId, UpdateFileCommentRequest request, int userId);
        Task<bool> DeleteCommentAsync(int commentId, int userId);
        
        // Reply operations
        Task<FileCommentDto> ReplyToCommentAsync(int commentId, CreateFileCommentRequest request, int userId);
        
        // Resolution operations
        Task<FileCommentDto> ResolveCommentAsync(int commentId, int userId);
        Task<List<FileCommentDto>> GetUnresolvedCommentsAsync(int fileId, int userId);
        
        // Utility methods
        Task<bool> ValidateCommentAccessAsync(int commentId, int userId);
    }

    /// <summary>
    /// FileComment Service Implementation
    /// </summary>
    public class FileCommentService : IFileCommentService
    {
        private readonly ManagementFileDbContext _context;
        private readonly IFilePermissionService _permissionService;
        private readonly ILogger<FileCommentService> _logger;

        public FileCommentService(
            ManagementFileDbContext context,
            IFilePermissionService permissionService,
            ILogger<FileCommentService> logger)
        {
            _context = context;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<List<FileCommentDto>> GetFileCommentsAsync(int fileId, int userId)
        {
            if (!await _permissionService.HasPermissionAsync(userId, fileId, "read"))
                return new List<FileCommentDto>();

            // Get all versions of the file
            var versionIds = await _context.FileVersions
                .Where(fv => fv.ProjectFileId == fileId && !fv.IsDeleted)
                .Select(fv => fv.Id)
                .ToListAsync();

            if (!versionIds.Any())
                return new List<FileCommentDto>();

            var comments = await _context.FileComments
                .Where(fc => versionIds.Contains(fc.FileVersionId) && fc.IsActive && !fc.IsDeleted)
                .Include(fc => fc.Replies.Where(r => r.IsActive && !r.IsDeleted))
                .OrderBy(fc => fc.CreatedAt)
                .ToListAsync();

            // Get only root comments (no parent)
            var rootComments = comments.Where(c => c.ParentCommentId < 0).ToList();

            return rootComments.Select(c => MapToCommentDto(c, comments)).ToList();
        }

        public async Task<List<FileCommentDto>> GetVersionCommentsAsync(int versionId, int userId)
        {
            if (!await ValidateVersionAccess(versionId, userId))
                return new List<FileCommentDto>();

            var comments = await _context.FileComments
                .Where(fc => fc.FileVersionId == versionId && fc.IsActive && !fc.IsDeleted)
                .Include(fc => fc.Replies.Where(r => r.IsActive && !r.IsDeleted))
                .OrderBy(fc => fc.CreatedAt)
                .ToListAsync();

            // Get only root comments (no parent)
            var rootComments = comments.Where(c => c.ParentCommentId < 0).ToList();

            return rootComments.Select(c => MapToCommentDto(c, comments)).ToList();
        }

        public async Task<FileCommentDto?> GetCommentByIdAsync(int commentId, int userId)
        {
            if (!await ValidateCommentAccessAsync(commentId, userId))
                return null;

            var comment = await _context.FileComments
                .Include(fc => fc.Replies.Where(r => r.IsActive && !r.IsDeleted))
                .FirstOrDefaultAsync(fc => fc.Id == commentId && fc.IsActive && !fc.IsDeleted);

            if (comment == null)
                return null;

            return MapToCommentDto(comment, new List<FileComment> { comment }.Concat(comment.Replies).ToList());
        }

        public async Task<FileCommentDto> CreateCommentAsync(CreateFileCommentRequest request, int userId)
        {
            if (!await ValidateVersionAccess(request.FileVersionId, userId))
                throw new UnauthorizedAccessException("Insufficient permissions to comment on this file");

            var version = await _context.FileVersions
                .Include(fv => fv.ProjectFile)
                .FirstOrDefaultAsync(fv => fv.Id == request.FileVersionId);

            if (version == null)
                throw new ArgumentException("File version not found");

            if (!await _permissionService.HasPermissionAsync(userId, version.ProjectFileId, "comment"))
                throw new UnauthorizedAccessException("Insufficient permissions to comment on this file");

            var comment = new FileComment
            {
                FileVersionId = request.FileVersionId,
                Content = request.Content,
                LineNumber = request.LineNumber,
                StartColumn = request.StartColumn,
                EndColumn = request.EndColumn,
                CommentType = request.CommentType,
                ParentCommentId = request.ParentCommentId,
                CreatedBy = userId
            };

            _context.FileComments.Add(comment);
            await _context.SaveChangesAsync();

            return MapToCommentDto(comment, new List<FileComment>());
        }

        public async Task<FileCommentDto> UpdateCommentAsync(int commentId, UpdateFileCommentRequest request, int userId)
        {
            if (!await ValidateCommentAccessAsync(commentId, userId))
                throw new UnauthorizedAccessException("Access denied");

            var comment = await _context.FileComments.FindAsync(commentId);
            if (comment == null || !comment.IsActive || comment.IsDeleted)
                throw new ArgumentException("Comment not found");

            // Only allow creator to update comment
            if (comment.CreatedBy != userId)
                throw new UnauthorizedAccessException("Only comment creator can update comment");

            comment.Content = request.Content;
            comment.MarkAsUpdated(userId);

            await _context.SaveChangesAsync();

            return MapToCommentDto(comment, new List<FileComment>());
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int userId)
        {
            if (!await ValidateCommentAccessAsync(commentId, userId))
                return false;

            var comment = await _context.FileComments
                .Include(fc => fc.Replies)
                .FirstOrDefaultAsync(fc => fc.Id == commentId);

            if (comment == null || comment.IsDeleted)
                return false;

            // Only allow creator or admin to delete comment
            if (comment.CreatedBy != userId)
            {
                // Check if user has admin permissions on the file
                var version = await _context.FileVersions
                    .FirstOrDefaultAsync(fv => fv.Id == comment.FileVersionId);
                
                if (version != null && !await _permissionService.HasPermissionAsync(userId, version.ProjectFileId, "managepermissions"))
                    return false;
            }

            // Soft delete comment and all replies
            comment.SoftDelete(userId);
            foreach (var reply in comment.Replies.Where(r => !r.IsDeleted))
            {
                reply.SoftDelete(userId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FileCommentDto> ReplyToCommentAsync(int commentId, CreateFileCommentRequest request, int userId)
        {
            if (!await ValidateCommentAccessAsync(commentId, userId))
                throw new UnauthorizedAccessException("Access denied");

            var parentComment = await _context.FileComments
                .Include(fc => fc.FileVersion)
                .ThenInclude(fv => fv.ProjectFile)
                .FirstOrDefaultAsync(fc => fc.Id == commentId);

            if (parentComment == null)
                throw new ArgumentException("Parent comment not found");

            if (!await _permissionService.HasPermissionAsync(userId, parentComment.FileVersion.ProjectFileId, "comment"))
                throw new UnauthorizedAccessException("Insufficient permissions to reply to this comment");

            var reply = new FileComment
            {
                FileVersionId = parentComment.FileVersionId,
                Content = request.Content,
                CommentType = request.CommentType,
                ParentCommentId = commentId,
                CreatedBy = userId
            };

            _context.FileComments.Add(reply);
            await _context.SaveChangesAsync();

            return MapToCommentDto(reply, new List<FileComment>());
        }

        public async Task<FileCommentDto> ResolveCommentAsync(int commentId, int userId)
        {
            if (!await ValidateCommentAccessAsync(commentId, userId))
                throw new UnauthorizedAccessException("Access denied");

            var comment = await _context.FileComments
                .Include(fc => fc.FileVersion)
                .ThenInclude(fv => fv.ProjectFile)
                .FirstOrDefaultAsync(fc => fc.Id == commentId);

            if (comment == null)
                throw new ArgumentException("Comment not found");

            // Check if user has approve permissions or is the comment creator
            if (comment.CreatedBy != userId && 
                !await _permissionService.HasPermissionAsync(userId, comment.FileVersion.ProjectFileId, "approve"))
                throw new UnauthorizedAccessException("Insufficient permissions to resolve this comment");

            comment.Resolve(userId);
            await _context.SaveChangesAsync();

            return MapToCommentDto(comment, new List<FileComment>());
        }

        public async Task<List<FileCommentDto>> GetUnresolvedCommentsAsync(int fileId, int userId)
        {
            if (!await _permissionService.HasPermissionAsync(userId, fileId, "read"))
                return new List<FileCommentDto>();

            var versionIds = await _context.FileVersions
                .Where(fv => fv.ProjectFileId == fileId && !fv.IsDeleted)
                .Select(fv => fv.Id)
                .ToListAsync();

            var unresolvedComments = await _context.FileComments
                .Where(fc => versionIds.Contains(fc.FileVersionId) && 
                            !fc.IsResolved && 
                            fc.IsActive && 
                            !fc.IsDeleted)
                .Include(fc => fc.Replies.Where(r => r.IsActive && !r.IsDeleted))
                .OrderBy(fc => fc.CreatedAt)
                .ToListAsync();

            var rootComments = unresolvedComments.Where(c => c.ParentCommentId < 0).ToList();

            return rootComments.Select(c => MapToCommentDto(c, unresolvedComments)).ToList();
        }

        public async Task<bool> ValidateCommentAccessAsync(int commentId, int userId)
        {
            var comment = await _context.FileComments
                .Include(fc => fc.FileVersion)
                .ThenInclude(fv => fv.ProjectFile)
                .FirstOrDefaultAsync(fc => fc.Id == commentId);

            if (comment == null)
                return false;

            return await _permissionService.HasPermissionAsync(userId, comment.FileVersion.ProjectFileId, "read");
        }

        #region Private Methods

        private async Task<bool> ValidateVersionAccess(int versionId, int userId)
        {
            var version = await _context.FileVersions
                .Include(fv => fv.ProjectFile)
                .FirstOrDefaultAsync(fv => fv.Id == versionId);

            if (version == null)
                return false;

            return await _permissionService.HasPermissionAsync(userId, version.ProjectFileId, "read");
        }

        private FileCommentDto MapToCommentDto(FileComment comment, List<FileComment> allComments)
        {
            var replies = allComments.Where(c => c.ParentCommentId == comment.Id).ToList();

            return new FileCommentDto
            {
                Id = comment.Id,
                FileVersionId = comment.FileVersionId,
                Content = comment.Content,
                LineNumber = comment.LineNumber,
                StartColumn = comment.StartColumn,
                EndColumn = comment.EndColumn,
                CommentType = comment.CommentType,
                ParentCommentId = comment.ParentCommentId,
                IsResolved = comment.IsResolved,
                ResolvedBy = comment.ResolvedBy,
                ResolvedByName = "", // Would need user lookup
                ResolvedAt = comment.ResolvedAt,
                CreatedAt = comment.CreatedAt,
                CreatedBy = comment.CreatedBy,
                CreatedByName = "", // Would need user lookup
                Replies = replies.Select(r => MapToCommentDto(r, allComments)).ToList()
            };
        }

        public static CommentResponse FromComment(FileComment comment)
        {
            return new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatorId = comment.CreatedBy,
                ParentId = comment.ParentCommentId,
                IsResolved = comment.IsResolved,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,

                Creator = new UserDto { Id = comment.CreatedBy }, // Would need proper user lookup
                Replies = comment.Replies.Select(FromComment).ToList()
            };
        }

        #endregion
    }
}