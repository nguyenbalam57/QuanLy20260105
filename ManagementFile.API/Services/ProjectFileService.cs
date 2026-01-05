using ManagementFile.API.Data;
using ManagementFile.Models.FileManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.FileManagement;
using ManagementFile.Contracts.DTOs.FileManagement;
using ManagementFile.Contracts.Responses.FileManagement;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Interface for ProjectFile Service
    /// </summary>
    public interface IProjectFileService
    {
        // CRUD operations
        Task<ProjectFileDto?> GetFileByIdAsync(int fileId, int userId);
        Task<PagedResponse<ProjectFileDto>> GetProjectFilesAsync(int projectId, int userId, int page = 1, int pageSize = 10, 
            string? search = null, int? folderId = null, string? fileType = null);
        Task<ProjectFileDto> CreateFileAsync(CreateProjectFileRequest request, FileUploadDto upload, int userId);
        Task<ProjectFileDto> UpdateFileAsync(int fileId, UpdateProjectFileRequest request, int userId);
        Task<bool> DeleteFileAsync(int fileId, int userId);
        
        // File operations
        Task<byte[]> DownloadFileAsync(int fileId, int userId);
        Task<string> GetPreviewUrlAsync(int fileId, int userId);
        Task<ProjectFileDto> CopyFileAsync(int fileId, CopyFileRequest request, int userId);
        Task<ProjectFileDto> MoveFileAsync(int fileId, MoveFileRequest request, int userId);
        
        // Version control
        Task<ProjectFileDto> CheckoutFileAsync(int fileId, CheckoutRequest request, int userId);
        Task<ProjectFileDto> CheckinFileAsync(int fileId, FileUploadDto? upload, int userId, string notes = "");
        Task<ProjectFileDto> ForceCheckinFileAsync(int fileId, int adminUserId);
        
        // Approval workflow
        Task<ProjectFileDto> ApproveFileAsync(int fileId, ApprovalRequest request, int userId);
        Task<ProjectFileDto> RejectFileAsync(int fileId, ApprovalRequest request, int userId);
        
        // Statistics
        Task<FileStatsDto> GetFileStatsAsync(int fileId, int userId);
        
        // Utility methods
        Task<bool> ValidateFileAccessAsync(int fileId, int userId, string permissionType);
        Task CleanupOverdueCheckoutsAsync();
    }

    /// <summary>
    /// ProjectFile Service Implementation
    /// </summary>
    public class ProjectFileService : IProjectFileService
    {
        private readonly ManagementFileDbContext _context;
        private readonly IFilePermissionService _permissionService;
        private readonly string _storagePath;

        public ProjectFileService(ManagementFileDbContext context, IFilePermissionService permissionService, IConfiguration configuration)
        {
            _context = context;
            _permissionService = permissionService;
            _storagePath = configuration.GetValue<string>("FileStorage:BasePath") ?? "uploads";
            
            // Ensure storage directory exists
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<ProjectFileDto?> GetFileByIdAsync(int fileId, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "read"))
                return null;

            var file = await _context.ProjectFiles
                .Include(f => f.Project)
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Id == fileId && f.IsActive && !f.IsDeleted);

            if (file == null)
                return null;

            // Mark as accessed
            file.MarkAccessed(userId);
            await _context.SaveChangesAsync();

            return await MapToFileDto(file, userId);
        }

        public async Task<PagedResponse<ProjectFileDto>> GetProjectFilesAsync(int projectId, int userId, int page = 1, int pageSize = 10,
            string? search = null, int? folderId = -1, string? fileType = null)
        {
            var query = _context.ProjectFiles
                .Include(f => f.Project)
                .Include(f => f.Folder)
                .Where(f => f.ProjectId == projectId && f.IsActive && !f.IsDeleted);

            // Apply filters
            if (folderId >= 0)
            {
                query = query.Where(f => f.FolderId == folderId);
            }

            if (!string.IsNullOrEmpty(fileType))
            {
                query = query.Where(f => f.FileType == fileType);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f => f.FileName.Contains(search) || 
                                        f.DisplayName.Contains(search) || 
                                        f.Description.Contains(search));
            }

            // Apply permission filtering - only show files user can read
            var allFiles = await query.ToListAsync();
            var accessibleFiles = new List<ProjectFile>();

            foreach (var file in allFiles)
            {
                if (await ValidateFileAccessAsync(file.Id, userId, "read"))
                {
                    accessibleFiles.Add(file);
                }
            }

            // Apply pagination
            var totalCount = accessibleFiles.Count;
            var items = accessibleFiles
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var fileDtos = new List<ProjectFileDto>();
            foreach (var file in items)
            {
                var fileDto = await MapToFileDto(file, userId);
                fileDtos.Add(fileDto);
            }

            return new PagedResponse<ProjectFileDto>
            {
                Items = fileDtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<ProjectFileDto> CreateFileAsync(CreateProjectFileRequest request, FileUploadDto upload, int userId)
        {
            // Validate project access
            var project = await _context.Projects.FindAsync(request.ProjectId);
            if (project == null || !project.IsActive)
                throw new ArgumentException("Project not found or inactive");

            // Validate folder if specified
            if (request.FolderId >= 0)
            {
                var folder = await _context.ProjectFolders.FindAsync(request.FolderId);
                if (folder == null || folder.ProjectId != request.ProjectId)
                    throw new ArgumentException("Invalid folder");
            }

            // Check for duplicate filename
            var existingFile = await _context.ProjectFiles
                .AnyAsync(f => f.ProjectId == request.ProjectId && 
                              f.FolderId == request.FolderId && 
                              f.FileName == request.FileName && 
                              f.IsActive && !f.IsDeleted);

            if (existingFile)
                throw new InvalidOperationException("File with same name already exists in this location");

            // Save physical file
            var physicalPath = await SavePhysicalFileAsync(upload, request.ProjectId);
            var fileHash = CalculateFileHash(upload.Content);

            // Create ProjectFile entity
            var projectFile = new ProjectFile
            {
                ProjectId = request.ProjectId,
                FolderId = request.FolderId,
                FileName = request.FileName,
                DisplayName = !string.IsNullOrEmpty(request.DisplayName) ? request.DisplayName : request.FileName,
                Description = request.Description,
                FileExtension = Path.GetExtension(request.FileName).ToLowerInvariant(),
                FileType = DetermineFileType(request.FileName),
                MimeType = upload.ContentType,
                CurrentFileSize = upload.FileSize,
                CurrentFileHash = fileHash,
                StoragePath = physicalPath,
                RelativePath = GetRelativePath(request.ProjectId, request.FolderId),
                IsPublic = request.IsPublic,
                IsReadOnly = request.IsReadOnly,
                RequireApproval = request.RequireApproval,
                ApprovalStatus = request.RequireApproval ? "Pending" : "Approved",
                CreatedBy = userId
            };

            projectFile.SetTags(request.Tags);
            projectFile.UpdateFileExtension();

            _context.ProjectFiles.Add(projectFile);
            await _context.SaveChangesAsync();

            // Create initial version
            var initialVersion = projectFile.CreateNewVersion(
                "1.0",
                userId,
                FileChangeType.Created,
                physicalPath,
                upload.FileSize,
                fileHash,
                "Initial version");

            await _context.SaveChangesAsync();

            // Grant owner permissions to creator
            await _permissionService.GrantPermissionAsync(
                projectFile.Id,
                userId,
                "",
                PermissionLevel.Owner,
                userId,
                reason: "File creator");

            return await MapToFileDto(projectFile, userId);
        }

        public async Task<ProjectFileDto> UpdateFileAsync(int fileId, UpdateProjectFileRequest request, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "write"))
                throw new UnauthorizedAccessException("Insufficient permissions to update file");

            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");
                
            if (file.IsReadOnly && !await ValidateFileAccessAsync(fileId, userId, "managepermissions"))
                throw new InvalidOperationException("File is read-only");

            // Update properties
            if (!string.IsNullOrEmpty(request.DisplayName))
                file.DisplayName = request.DisplayName;
                
            if (!string.IsNullOrEmpty(request.Description))
                file.Description = request.Description;

            if (request.IsPublic.HasValue)
                file.IsPublic = request.IsPublic.Value;

            if (request.IsReadOnly.HasValue)
                file.IsReadOnly = request.IsReadOnly.Value;

            if (request.RequireApproval.HasValue)
                file.RequireApproval = request.RequireApproval.Value;

            if (request.Tags.Any())
                file.SetTags(request.Tags);

            file.MarkAsUpdated(userId);
            await _context.SaveChangesAsync();

            return await MapToFileDto(file, userId);
        }

        public async Task<bool> DeleteFileAsync(int fileId, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "delete"))
                return false;

            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || file.IsDeleted)
                return false;

            // Soft delete
            file.SoftDelete(userId);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<byte[]> DownloadFileAsync(int fileId, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "download"))
                throw new UnauthorizedAccessException("Insufficient permissions to download file");

            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            if (!File.Exists(file.StoragePath))
                throw new FileNotFoundException("Physical file not found");

            // Mark as downloaded
            file.MarkDownloaded(userId);
            await _context.SaveChangesAsync();

            return await File.ReadAllBytesAsync(file.StoragePath);
        }

        public async Task<string> GetPreviewUrlAsync(int fileId, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "read"))
                throw new UnauthorizedAccessException("Insufficient permissions to preview file");

            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            // Mark as accessed
            file.MarkAccessed(userId);
            await _context.SaveChangesAsync();

            return !string.IsNullOrEmpty(file.PreviewPath) ? file.PreviewPath : file.StoragePath;
        }

        public async Task<ProjectFileDto> CopyFileAsync(int fileId, CopyFileRequest request, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "read"))
                throw new UnauthorizedAccessException("Insufficient permissions to copy file");

            var sourceFile = await _context.ProjectFiles.FindAsync(fileId);
            if (sourceFile == null || !sourceFile.IsActive || sourceFile.IsDeleted)
                throw new ArgumentException("Source file not found");

            // Validate target project
            var targetProject = await _context.Projects.FindAsync(request.TargetProjectId);
            if (targetProject == null || !targetProject.IsActive)
                throw new ArgumentException("Target project not found or inactive");

            // Copy physical file
            var newPhysicalPath = await CopyPhysicalFileAsync(sourceFile.StoragePath, request.TargetProjectId);

            // Create new file entity
            var newFile = new ProjectFile
            {
                ProjectId = request.TargetProjectId,
                FolderId = request.TargetFolderId,
                FileName = !string.IsNullOrEmpty(request.NewFileName) ? request.NewFileName : sourceFile.FileName,
                DisplayName = sourceFile.DisplayName + " (Copy)",
                Description = sourceFile.Description,
                FileExtension = sourceFile.FileExtension,
                FileType = sourceFile.FileType,
                MimeType = sourceFile.MimeType,
                CurrentFileSize = sourceFile.CurrentFileSize,
                CurrentFileHash = sourceFile.CurrentFileHash,
                StoragePath = newPhysicalPath,
                RelativePath = GetRelativePath(request.TargetProjectId, request.TargetFolderId),
                IsPublic = sourceFile.IsPublic,
                IsReadOnly = sourceFile.IsReadOnly,
                RequireApproval = sourceFile.RequireApproval,
                ApprovalStatus = sourceFile.RequireApproval ? "Pending" : "Approved",
                CreatedBy = userId
            };

            newFile.SetTags(sourceFile.GetTags());

            _context.ProjectFiles.Add(newFile);
            await _context.SaveChangesAsync();

            // Create initial version
            newFile.CreateNewVersion(
                "1.0",
                userId,
                FileChangeType.Copied,
                newPhysicalPath,
                sourceFile.CurrentFileSize,
                sourceFile.CurrentFileHash,
                $"Copied from {sourceFile.FileName}");

            await _context.SaveChangesAsync();

            // Grant owner permissions to copier
            await _permissionService.GrantPermissionAsync(
                newFile.Id,
                userId,
                "",
                PermissionLevel.Owner,
                userId,
                reason: "File copier");

            return await MapToFileDto(newFile, userId);
        }

        public async Task<ProjectFileDto> MoveFileAsync(int fileId, MoveFileRequest request, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "write"))
                throw new UnauthorizedAccessException("Insufficient permissions to move file");

            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            // Validate target folder
            if (request.NewFolderId >= 0)
            {
                var targetFolder = await _context.ProjectFolders.FindAsync(request.NewFolderId);
                if (targetFolder == null || targetFolder.ProjectId != file.ProjectId)
                    throw new ArgumentException("Invalid target folder");
            }

            // Check for duplicate filename in target location
            var existingFile = await _context.ProjectFiles
                .AnyAsync(f => f.ProjectId == file.ProjectId && 
                              f.FolderId == request.NewFolderId && 
                              f.FileName == file.FileName && 
                              f.Id != fileId &&
                              f.IsActive && !f.IsDeleted);

            if (existingFile)
                throw new InvalidOperationException("File with same name already exists in target location");

            file.FolderId = request.NewFolderId;
            file.RelativePath = GetRelativePath(file.ProjectId, request.NewFolderId);
            file.MarkAsUpdated(userId);

            // Create version for move operation
            file.CreateNewVersion(
                GenerateNextVersionNumber(file),
                userId,
                FileChangeType.Moved,
                file.StoragePath,
                file.CurrentFileSize,
                file.CurrentFileHash,
                $"Moved to {file.RelativePath}");

            await _context.SaveChangesAsync();

            return await MapToFileDto(file, userId);
        }

        public async Task<ProjectFileDto> CheckoutFileAsync(int fileId, CheckoutRequest request, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "checkout"))
                throw new UnauthorizedAccessException("Insufficient permissions to checkout file");

            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            if (file.IsCheckedOut())
                throw new InvalidOperationException("File is already checked out");

            file.Checkout(userId, request.ExpectedCheckinHours);
            await _context.SaveChangesAsync();

            return await MapToFileDto(file, userId);
        }

        public async Task<ProjectFileDto> CheckinFileAsync(int fileId, FileUploadDto? upload, int userId, string notes = "")
        {
            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            if (!file.IsCheckedOutBy(userId))
                throw new InvalidOperationException("File is not checked out by this user");

            // If new content provided, save it and create new version
            if (upload != null)
            {
                var newPhysicalPath = await SavePhysicalFileAsync(upload, file.ProjectId);
                var newFileHash = CalculateFileHash(upload.Content);

                file.CreateNewVersion(
                    GenerateNextVersionNumber(file),
                    userId,
                    FileChangeType.Modified,
                    newPhysicalPath,
                    upload.FileSize,
                    newFileHash,
                    notes);

                // Update current file info
                file.StoragePath = newPhysicalPath;
                file.CurrentFileSize = upload.FileSize;
                file.CurrentFileHash = newFileHash;
            }

            file.Checkin(userId);
            await _context.SaveChangesAsync();

            return await MapToFileDto(file, userId);
        }

        public async Task<ProjectFileDto> ForceCheckinFileAsync(int fileId, int adminUserId)
        {
            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            if (!file.IsCheckedOut())
                throw new InvalidOperationException("File is not checked out");

            file.ForceCheckin(adminUserId);
            await _context.SaveChangesAsync();

            return await MapToFileDto(file, adminUserId);
        }

        public async Task<ProjectFileDto> ApproveFileAsync(int fileId, ApprovalRequest request, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "approve"))
                throw new UnauthorizedAccessException("Insufficient permissions to approve file");

            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            if (!file.RequireApproval)
                throw new InvalidOperationException("File does not require approval");

            file.ApprovalStatus = "Approved";
            file.ApprovedBy = userId;
            file.ApprovedAt = DateTime.UtcNow;
            file.MarkAsUpdated(userId);

            await _context.SaveChangesAsync();

            return await MapToFileDto(file, userId);
        }

        public async Task<ProjectFileDto> RejectFileAsync(int fileId, ApprovalRequest request, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "approve"))
                throw new UnauthorizedAccessException("Insufficient permissions to reject file");

            var file = await _context.ProjectFiles.FindAsync(fileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            if (!file.RequireApproval)
                throw new InvalidOperationException("File does not require approval");

            file.ApprovalStatus = "Rejected";
            file.ApprovedBy = userId;
            file.ApprovedAt = DateTime.UtcNow;
            file.MarkAsUpdated(userId);

            await _context.SaveChangesAsync();

            return await MapToFileDto(file, userId);
        }

        public async Task<FileStatsDto> GetFileStatsAsync(int fileId, int userId)
        {
            if (!await ValidateFileAccessAsync(fileId, userId, "read"))
                throw new UnauthorizedAccessException("Insufficient permissions to view file statistics");

            var file = await _context.ProjectFiles
                .Include(f => f.FileVersions)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
                throw new ArgumentException("File not found");

            var stats = new FileStatsDto
            {
                FileId = fileId,
                TotalVersions = file.FileVersions.Count,
                ViewCount = file.ViewCount,
                DownloadCount = file.DownloadCount,
                ShareCount = file.ShareCount,
                LastAccessedAt = file.LastAccessedAt,
                LastAccessedBy = file.LastAccessedBy
            };

            // Get recent versions
            stats.RecentVersions = file.FileVersions
                .OrderByDescending(v => v.CreatedAt)
                .Take(5)
                .Select(v => new FileVersionDto
                {
                    Id = v.Id,
                    ProjectFileId = v.ProjectFileId,
                    VersionNumber = v.VersionNumber,
                    ChangeType = v.ChangeType,
                    FileSize = v.FileSize,
                    FileHash = v.FileHash,
                    VersionNotes = v.VersionNotes,
                    IsCurrentVersion = v.IsCurrentVersion,
                    CreatedAt = v.CreatedAt,
                    CreatedBy = v.CreatedBy
                })
                .ToList();

            return stats;
        }

        public async Task<bool> ValidateFileAccessAsync(int fileId, int userId, string permissionType)
        {
            return await _permissionService.HasPermissionAsync(userId, fileId, permissionType);
        }

        public async Task CleanupOverdueCheckoutsAsync()
        {
            var overdueFiles = await _context.ProjectFiles
                .Where(f => f.IsActive && !f.IsDeleted && 
                           f.CheckoutBy >= 0 && 
                           f.ExpectedCheckinAt.HasValue && 
                           f.ExpectedCheckinAt.Value <= DateTime.UtcNow &&
                           f.AutoCheckinHours > 0)
                .ToListAsync();

            foreach (var file in overdueFiles)
            {
                file.ForceCheckin(-2);
            }

            if (overdueFiles.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        #region Private Methods

        private async Task<ProjectFileDto> MapToFileDto(ProjectFile file, int userId)
        {
            var effectivePermissions = await _permissionService.GetEffectivePermissionsAsync(userId, file.Id);
            var currentVersion = file.GetCurrentVersion();

            return new ProjectFileDto
            {
                Id = file.Id,
                ProjectId = file.ProjectId,
                FolderId = file.FolderId,
                FileName = file.FileName,
                DisplayName = file.DisplayName,
                FileExtension = file.FileExtension,
                FileType = file.FileType,
                MimeType = file.MimeType,
                CurrentFileSize = file.CurrentFileSize,
                CurrentFileHash = file.CurrentFileHash,
                RelativePath = file.RelativePath,
                Description = file.Description,
                IsActive = file.IsActive,
                IsPublic = file.IsPublic,
                IsReadOnly = file.IsReadOnly,
                RequireApproval = file.RequireApproval,
                ApprovalStatus = file.ApprovalStatus,
                ApprovedBy = file.ApprovedBy,
                ApprovedAt = file.ApprovedAt,
                LastAccessedAt = file.LastAccessedAt,
                LastAccessedBy = file.LastAccessedBy,
                DownloadCount = file.DownloadCount,
                ViewCount = file.ViewCount,
                ShareCount = file.ShareCount,
                CheckoutBy = file.CheckoutBy,
                CheckoutAt = file.CheckoutAt,
                ExpectedCheckinAt = file.ExpectedCheckinAt,
                ThumbnailPath = file.ThumbnailPath,
                PreviewPath = file.PreviewPath,
                Tags = file.GetTags(),
                CreatedAt = file.CreatedAt,
                CreatedBy = file.CreatedBy,
                UpdatedAt = file.UpdatedAt,
                UpdatedBy = file.UpdatedBy,
                IsCheckedOut = file.IsCheckedOut(),
                IsOverdueCheckout = file.IsOverdueCheckout(),
                CurrentVersion = currentVersion?.VersionNumber ?? "",
                EffectivePermissions = effectivePermissions
            };
        }

        private async Task<string> SavePhysicalFileAsync(FileUploadDto upload, int projectId)
        {
            var projectPath = Path.Combine(_storagePath, projectId.ToString());
            if (!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(upload.FileName)}";
            var filePath = Path.Combine(projectPath, fileName);

            await File.WriteAllBytesAsync(filePath, upload.Content);
            return filePath;
        }

        private async Task<string> CopyPhysicalFileAsync(string sourcePath, int targetProjectId)
        {
            var targetProjectPath = Path.Combine(_storagePath, targetProjectId.ToString());
            if (!Directory.Exists(targetProjectPath))
            {
                Directory.CreateDirectory(targetProjectPath);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
            var targetPath = Path.Combine(targetProjectPath, fileName);

            File.Copy(sourcePath, targetPath);
            return targetPath;
        }

        private string CalculateFileHash(byte[] content)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(content);
            return Convert.ToBase64String(hash);
        }

        private string DetermineFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" => "Image",
                ".pdf" => "PDF",
                ".doc" or ".docx" => "Word Document",
                ".xls" or ".xlsx" => "Excel Spreadsheet",
                ".ppt" or ".pptx" => "PowerPoint Presentation",
                ".txt" => "Text File",
                ".zip" or ".rar" or ".7z" => "Archive",
                ".mp4" or ".avi" or ".mov" or ".wmv" => "Video",
                ".mp3" or ".wav" or ".wma" => "Audio",
                ".cs" or ".js" or ".html" or ".css" or ".xml" or ".json" => "Code",
                _ => "Document"
            };
        }

        private string GetRelativePath(int projectId, int folderId = -1)
        {
            if (folderId < 0)
                return null;
                
            // This would need to build the full folder path
            // For now, return the folder ID
            return folderId.ToString();
        }

        private string GenerateNextVersionNumber(ProjectFile file)
        {
            var currentVersion = file.GetCurrentVersion();
            if (currentVersion == null)
                return "1.0";

            var parts = currentVersion.VersionNumber.Split('.');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int minor))
            {
                return $"{parts[0]}.{minor + 1}";
            }

            return "1.0";
        }

        #endregion
    }
}