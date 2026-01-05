using ManagementFile.API.Data;
using ManagementFile.Contracts.DTOs.FileManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.FileManagement;
using ManagementFile.Models.FileManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Interface for File Permission Service
    /// </summary>
    public interface IFilePermissionService
    {
        // Check permissions
        Task<bool> HasPermissionAsync(int userId, int fileId, string permissionType);
        Task<FilePermissionResult> GetEffectivePermissionsAsync(int userId, int fileId);
        Task<List<string>> GetUserPermissionsAsync(int userId, int fileId);
        Task<List<FilePermissionDto>> GetFilePermissionsAsync(int fileId);
        
        // Grant/Revoke permissions
        Task<FilePermission> GrantPermissionAsync(int fileId, int userId, string roleName, 
            PermissionLevel level, int grantedBy, DateTime? expiresAt = null, string reason = "");
        Task RevokePermissionAsync(int permissionId, int revokedBy, string reason = "");
        Task<FilePermission> UpdatePermissionAsync(int permissionId, UpdatePermissionRequest request, int updatedBy);
        
        // Bulk operations
        Task<List<FilePermission>> BulkGrantPermissionsAsync(List<int> fileIds, int userId, string roleName,
            PermissionLevel level, int grantedBy, DateTime? expiresAt = null, string reason = "");
        Task InheritFolderPermissionsAsync(int folderId, int userId);
        
        // Utility methods
        Task CleanupExpiredPermissionsAsync();
    }

    /// <summary>
    /// File Permission Service Implementation
    /// </summary>
    public class FilePermissionService : IFilePermissionService
    {
        private readonly ManagementFileDbContext _context;
        
        public FilePermissionService(ManagementFileDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(int userId, int fileId, string permissionType)
        {
            var effectivePermissions = await GetEffectivePermissionsAsync(userId, fileId);
            
            return permissionType.ToLower() switch
            {
                "read" => effectivePermissions.CanRead,
                "write" => effectivePermissions.CanWrite,
                "delete" => effectivePermissions.CanDelete,
                "share" => effectivePermissions.CanShare,
                "managepermissions" => effectivePermissions.CanManagePermissions,
                "download" => effectivePermissions.CanDownload,
                "print" => effectivePermissions.CanPrint,
                "comment" => effectivePermissions.CanComment,
                "checkout" => effectivePermissions.CanCheckout,
                "approve" => effectivePermissions.CanApprove,
                _ => false
            };
        }

        public async Task<FilePermissionResult> GetEffectivePermissionsAsync(int userId, int fileId)
        {
            var result = new FilePermissionResult();
            
            // Get the file
            var file = await _context.ProjectFiles
                .Include(f => f.Project)
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Id == fileId);
                
            if (file == null)
                return result;

            // 1. Check if file is public
            if (file.IsPublic)
            {
                result.CanRead = true;
                result.CanDownload = true;
                result.CanPrint = true;
                result.Source = "Public";
                result.PermissionLevel = "Reader";
                return result;
            }

            // 2. Get explicit file permissions for user
            var userPermissions = await GetUserFilePermissionsAsync(userId, fileId);
            if (userPermissions.Any())
            {
                return MergePermissions(userPermissions, "Direct");
            }

            // 3. Get role-based permissions
            var rolePermissions = await GetRoleFilePermissionsAsync(userId, fileId);
            if (rolePermissions.Any())
            {
                return MergePermissions(rolePermissions, "Role");
            }

            // 4. Get inherited folder permissions
            var folderPermissions = await GetInheritedFolderPermissionsAsync(userId, fileId);
            if (folderPermissions.Any())
            {
                return MergePermissions(folderPermissions, "Inherited");
            }

            // 5. Get project-level permissions
            var projectPermissions = await GetProjectPermissionsAsync(userId, file.ProjectId);
            if (projectPermissions.Any())
            {
                return MergePermissions(projectPermissions, "Project");
            }

            // 6. Default permissions - none
            return result;
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId, int fileId)
        {
            var effectivePermissions = await GetEffectivePermissionsAsync(userId, fileId);
            var permissions = new List<string>();

            if (effectivePermissions.CanRead) permissions.Add("read");
            if (effectivePermissions.CanWrite) permissions.Add("write");
            if (effectivePermissions.CanDelete) permissions.Add("delete");
            if (effectivePermissions.CanShare) permissions.Add("share");
            if (effectivePermissions.CanManagePermissions) permissions.Add("managepermissions");
            if (effectivePermissions.CanDownload) permissions.Add("download");
            if (effectivePermissions.CanPrint) permissions.Add("print");
            if (effectivePermissions.CanComment) permissions.Add("comment");
            if (effectivePermissions.CanCheckout) permissions.Add("checkout");
            if (effectivePermissions.CanApprove) permissions.Add("approve");

            return permissions;
        }

        public async Task<List<FilePermissionDto>> GetFilePermissionsAsync(int fileId)
        {
            var permissions = await _context.FilePermissions
                .Where(fp => fp.ProjectFileId == fileId && fp.IsActive && !fp.IsDeleted)
                .Select(fp => new FilePermissionDto
                {
                    Id = fp.Id,
                    ProjectFileId = fp.ProjectFileId,
                    UserId = fp.UserId,
                    RoleName = fp.RoleName,
                    PermissionType = fp.PermissionType,
                    CanRead = fp.CanRead,
                    CanWrite = fp.CanWrite,
                    CanDelete = fp.CanDelete,
                    CanShare = fp.CanShare,
                    CanManagePermissions = fp.CanManagePermissions,
                    CanDownload = fp.CanDownload,
                    CanPrint = fp.CanPrint,
                    CanComment = fp.CanComment,
                    CanCheckout = fp.CanCheckout,
                    CanApprove = fp.CanApprove,
                    IsActive = fp.IsActive,
                    ExpiresAt = fp.ExpiresAt,
                    GrantedBy = fp.GrantedBy,
                    GrantedAt = fp.GrantedAt,
                    PermissionLevel = fp.GetPermissionLevel(),
                    IsExpired = fp.IsExpired,
                    IsRevoked = fp.IsRevoked,
                    IsEffective = fp.IsEffective
                })
                .ToListAsync();

            return permissions;
        }

        public async Task<FilePermission> GrantPermissionAsync(int fileId, int userId, string roleName, 
            PermissionLevel level, int grantedBy, DateTime? expiresAt = null, string reason = "")
        {
            // Check if permission already exists
            var existingPermission = await _context.FilePermissions
                .FirstOrDefaultAsync(fp => fp.ProjectFileId == fileId && 
                    ((fp.UserId == userId && userId >= 0) ||
                     (fp.RoleName == roleName && !string.IsNullOrEmpty(roleName))) &&
                    fp.IsActive && !fp.IsDeleted);

            if (existingPermission != null)
            {
                // Update existing permission
                SetPermissionsByLevel(existingPermission, level);
                existingPermission.ExpiresAt = expiresAt;
                existingPermission.Reason = reason;
                existingPermission.MarkAsUpdated(grantedBy);
            }
            else
            {
                // Create new permission
                existingPermission = new FilePermission
                {
                    ProjectFileId = fileId,
                    UserId = userId,
                    RoleName = roleName,
                    PermissionType = userId >= 0 ? "Individual" : "Role",
                    ExpiresAt = expiresAt,
                    GrantedBy = grantedBy,
                    Reason = reason,
                    CreatedBy = grantedBy
                };

                SetPermissionsByLevel(existingPermission, level);
                _context.FilePermissions.Add(existingPermission);
            }

            await _context.SaveChangesAsync();
            return existingPermission;
        }

        public async Task RevokePermissionAsync(int permissionId, int revokedBy, string reason = "")
        {
            var permission = await _context.FilePermissions
                .FirstOrDefaultAsync(fp => fp.Id == permissionId);

            if (permission == null)
                throw new ArgumentException("Permission not found");

            permission.RevokePermission(revokedBy, reason);
            await _context.SaveChangesAsync();
        }

        public async Task<FilePermission> UpdatePermissionAsync(int permissionId, UpdatePermissionRequest request, int updatedBy)
        {
            var permission = await _context.FilePermissions
                .FirstOrDefaultAsync(fp => fp.Id == permissionId);

            if (permission == null)
                throw new ArgumentException("Permission not found");

            // Update individual permissions
            if (request.CanRead.HasValue) permission.CanRead = request.CanRead.Value;
            if (request.CanWrite.HasValue) permission.CanWrite = request.CanWrite.Value;
            if (request.CanDelete.HasValue) permission.CanDelete = request.CanDelete.Value;
            if (request.CanShare.HasValue) permission.CanShare = request.CanShare.Value;
            if (request.CanManagePermissions.HasValue) permission.CanManagePermissions = request.CanManagePermissions.Value;
            if (request.CanDownload.HasValue) permission.CanDownload = request.CanDownload.Value;
            if (request.CanPrint.HasValue) permission.CanPrint = request.CanPrint.Value;
            if (request.CanComment.HasValue) permission.CanComment = request.CanComment.Value;
            if (request.CanCheckout.HasValue) permission.CanCheckout = request.CanCheckout.Value;
            if (request.CanApprove.HasValue) permission.CanApprove = request.CanApprove.Value;

            if (request.ExpiresAt.HasValue) permission.ExpiresAt = request.ExpiresAt.Value;
            if (!string.IsNullOrEmpty(request.Reason)) permission.Reason = request.Reason;

            permission.MarkAsUpdated(updatedBy);
            await _context.SaveChangesAsync();

            return permission;
        }

        public async Task<List<FilePermission>> BulkGrantPermissionsAsync(List<int> fileIds, int userId, string roleName,
            PermissionLevel level, int grantedBy, DateTime? expiresAt = null, string reason = "")
        {
            var permissions = new List<FilePermission>();

            foreach (var fileId in fileIds)
            {
                var permission = await GrantPermissionAsync(fileId, userId, roleName, level, grantedBy, expiresAt, reason);
                permissions.Add(permission);
            }

            return permissions;
        }

        public async Task InheritFolderPermissionsAsync(int folderId, int userId)
        {
            // Get folder permissions
            var folderPermissions = await _context.FilePermissions
                .Where(fp => fp.ProjectFileId == folderId && fp.IsActive && !fp.IsDeleted)
                .ToListAsync();

            // Get all files in folder
            var filesInFolder = await _context.ProjectFiles
                .Where(f => f.FolderId == folderId && f.IsActive && !f.IsDeleted)
                .ToListAsync();

            // Apply folder permissions to each file
            foreach (var file in filesInFolder)
            {
                foreach (var folderPermission in folderPermissions)
                {
                    await GrantPermissionAsync(
                        file.Id,
                        folderPermission.UserId,
                        folderPermission.RoleName,
                        GetPermissionLevel(folderPermission),
                        userId,
                        folderPermission.ExpiresAt,
                        "Inherited from folder");
                }
            }
        }

        public async Task CleanupExpiredPermissionsAsync()
        {
            var expiredPermissions = await _context.FilePermissions
                .Where(fp => fp.IsActive && !fp.IsDeleted && fp.ExpiresAt.HasValue && fp.ExpiresAt.Value <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var permission in expiredPermissions)
            {
                permission.IsActive = false;
                permission.MarkAsUpdated(0);
            }

            if (expiredPermissions.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        #region Private Methods

        private async Task<List<FilePermission>> GetUserFilePermissionsAsync(int userId, int fileId)
        {
            return await _context.FilePermissions
                .Where(fp => fp.ProjectFileId == fileId && fp.UserId == userId && fp.IsEffective)
                .ToListAsync();
        }

        private async Task<List<FilePermission>> GetRoleFilePermissionsAsync(int userId, int fileId)
        {
            // Get user roles from UserRoles or similar table
            // For now, assuming we have a method to get user roles
            var userRoles = await GetUserRolesAsync(userId);
            
            return await _context.FilePermissions
                .Where(fp => fp.ProjectFileId == fileId && userRoles.Contains(fp.RoleName) && fp.IsEffective)
                .ToListAsync();
        }

        private async Task<List<FilePermission>> GetInheritedFolderPermissionsAsync(int userId, int fileId)
        {
            // Get file's folder hierarchy and check permissions at each level
            var file = await _context.ProjectFiles
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file?.Folder == null)
                return new List<FilePermission>();

            // For now, return empty list - implement folder permission inheritance logic
            return new List<FilePermission>();
        }

        private async Task<List<FilePermission>> GetProjectPermissionsAsync(int userId, int projectId)
        {
            // Get project-level permissions for user
            // This would typically come from ProjectMember table
            return new List<FilePermission>();
        }

        private async Task<List<string>> GetUserRolesAsync(int userId)
        {
            // Get user roles - implement based on your user role system
            // For now, return empty list
            return new List<string>();
        }

        private FilePermissionResult MergePermissions(List<FilePermission> permissions, string source)
        {
            var result = new FilePermissionResult { Source = source };

            foreach (var permission in permissions)
            {
                if (permission.CanRead) result.CanRead = true;
                if (permission.CanWrite) result.CanWrite = true;
                if (permission.CanDelete) result.CanDelete = true;
                if (permission.CanShare) result.CanShare = true;
                if (permission.CanManagePermissions) result.CanManagePermissions = true;
                if (permission.CanDownload) result.CanDownload = true;
                if (permission.CanPrint) result.CanPrint = true;
                if (permission.CanComment) result.CanComment = true;
                if (permission.CanCheckout) result.CanCheckout = true;
                if (permission.CanApprove) result.CanApprove = true;

                // Use earliest expiry date
                if (permission.ExpiresAt.HasValue && 
                    (!result.ExpiresAt.HasValue || permission.ExpiresAt.Value < result.ExpiresAt.Value))
                {
                    result.ExpiresAt = permission.ExpiresAt.Value;
                }
            }

            result.PermissionLevel = GetOverallPermissionLevel(result);
            return result;
        }

        private string GetOverallPermissionLevel(FilePermissionResult permissions)
        {
            if (permissions.CanManagePermissions && permissions.CanDelete)
                return "Owner";
            if (permissions.CanWrite && permissions.CanCheckout)
                return "Editor";
            if (permissions.CanComment && permissions.CanApprove)
                return "Reviewer";
            if (permissions.CanRead)
                return "Reader";
            return "None";
        }

        private void SetPermissionsByLevel(FilePermission permission, PermissionLevel level)
        {
            switch (level)
            {
                case PermissionLevel.Owner:
                    permission.SetFullPermissions(-1);
                    break;
                case PermissionLevel.Editor:
                    permission.SetEditorPermissions(-1);
                    break;
                case PermissionLevel.Reviewer:
                    permission.SetReviewerPermissions(-1);
                    break;
                case PermissionLevel.Reader:
                    permission.SetReadOnlyPermissions(-1);
                    break;
                default:
                    // No permissions
                    permission.UpdatePermissions(updatedBy: -1);
                    break;
            }
        }

        private PermissionLevel GetPermissionLevel(FilePermission permission)
        {
            if (permission.CanManagePermissions && permission.CanDelete)
                return PermissionLevel.Owner;
            if (permission.CanWrite && permission.CanCheckout)
                return PermissionLevel.Editor;
            if (permission.CanComment && permission.CanApprove)
                return PermissionLevel.Reviewer;
            if (permission.CanRead)
                return PermissionLevel.Reader;
            return PermissionLevel.None;
        }

        #endregion
    }
}