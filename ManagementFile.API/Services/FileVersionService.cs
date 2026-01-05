using ManagementFile.API.Data;
using ManagementFile.Models.FileManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ManagementFile.Contracts.DTOs.FileManagement;
using ManagementFile.Contracts.Requests.FileManagement;
using ManagementFile.Contracts.Enums;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Interface for FileVersion Service
    /// </summary>
    public interface IFileVersionService
    {
        // Version operations
        Task<List<FileVersionDto>> GetFileVersionsAsync(int fileId, int userId);
        Task<FileVersionDto?> GetVersionByIdAsync(int versionId, int userId);
        Task<FileVersionDto> CreateVersionAsync(int fileId, CreateVersionRequest request, FileUploadDto upload, int userId);
        Task<bool> RestoreVersionAsync(int fileId, int versionId, int userId);
        
        // Version comparison
        Task<object> CompareVersionsAsync(int fileId, CompareVersionsRequest request, int userId);
        
        // Download operations
        Task<byte[]> DownloadVersionAsync(int versionId, int userId);
        
        // Utility methods
        Task<bool> ValidateVersionAccessAsync(int versionId, int userId);
    }

    /// <summary>
    /// FileVersion Service Implementation
    /// </summary>
    public class FileVersionService : IFileVersionService
    {
        private readonly ManagementFileDbContext _context;
        private readonly IFilePermissionService _permissionService;
        private readonly ILogger<FileVersionService> _logger;
        private readonly string _storagePath;

        public FileVersionService(
            ManagementFileDbContext context, 
            IFilePermissionService permissionService,
            ILogger<FileVersionService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _permissionService = permissionService;
            _logger = logger;
            _storagePath = configuration.GetValue<string>("FileStorage:BasePath") ?? "uploads";
        }

        public async Task<List<FileVersionDto>> GetFileVersionsAsync(int fileId, int userId)
        {
            if (!await _permissionService.HasPermissionAsync(userId, fileId, "read"))
                return new List<FileVersionDto>();

            var versions = await _context.FileVersions
                .Where(fv => fv.ProjectFileId == fileId && !fv.IsDeleted)
                .OrderByDescending(fv => fv.CreatedAt)
                .ToListAsync();

            return versions.Select(v => new FileVersionDto
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
                CreatedBy = v.CreatedBy,
                CreatedByName = "" // Would need to join with Users table
            }).ToList();
        }

        public async Task<FileVersionDto?> GetVersionByIdAsync(int versionId, int userId)
        {
            if (!await ValidateVersionAccessAsync(versionId, userId))
                return null;

            var version = await _context.FileVersions
                .FirstOrDefaultAsync(fv => fv.Id == versionId && !fv.IsDeleted);

            if (version == null)
                return null;

            return new FileVersionDto
            {
                Id = version.Id,
                ProjectFileId = version.ProjectFileId,
                VersionNumber = version.VersionNumber,
                ChangeType = version.ChangeType,
                FileSize = version.FileSize,
                FileHash = version.FileHash,
                VersionNotes = version.VersionNotes,
                IsCurrentVersion = version.IsCurrentVersion,
                CreatedAt = version.CreatedAt,
                CreatedBy = version.CreatedBy,
                CreatedByName = "" // Would need to join with Users table
            };
        }

        public async Task<FileVersionDto> CreateVersionAsync(int fileId, CreateVersionRequest request, FileUploadDto upload, int userId)
        {
            if (!await _permissionService.HasPermissionAsync(userId, fileId, "write"))
                throw new UnauthorizedAccessException("Insufficient permissions to create new version");

            var file = await _context.ProjectFiles
                .Include(f => f.FileVersions)
                .FirstOrDefaultAsync(f => f.Id == fileId && f.IsActive && !f.IsDeleted);

            if (file == null)
                throw new ArgumentException("File not found");

            if (file.IsCheckedOut() && !file.IsCheckedOutBy(userId))
                throw new InvalidOperationException("File is checked out by another user");

            // Save physical file
            var physicalPath = await SaveVersionFileAsync(upload, file.ProjectId, fileId);
            var fileHash = CalculateFileHash(upload.Content);

            // Create new version
            var newVersion = file.CreateNewVersion(
                request.VersionNumber,
                userId,
                request.ChangeType,
                physicalPath,
                upload.FileSize,
                fileHash,
                request.VersionNotes);

            await _context.SaveChangesAsync();

            return new FileVersionDto
            {
                Id = newVersion.Id,
                ProjectFileId = newVersion.ProjectFileId,
                VersionNumber = newVersion.VersionNumber,
                ChangeType = newVersion.ChangeType,
                FileSize = newVersion.FileSize,
                FileHash = newVersion.FileHash,
                VersionNotes = newVersion.VersionNotes,
                IsCurrentVersion = newVersion.IsCurrentVersion,
                CreatedAt = newVersion.CreatedAt,
                CreatedBy = newVersion.CreatedBy,
                CreatedByName = "" // Would need user lookup
            };
        }

        public async Task<bool> RestoreVersionAsync(int fileId, int versionId, int userId)
        {
            if (!await _permissionService.HasPermissionAsync(userId, fileId, "write"))
                return false;

            var file = await _context.ProjectFiles
                .Include(f => f.FileVersions)
                .FirstOrDefaultAsync(f => f.Id == fileId && f.IsActive && !f.IsDeleted);

            if (file == null)
                return false;

            var versionToRestore = file.FileVersions.FirstOrDefault(v => v.Id == versionId);
            if (versionToRestore == null)
                return false;

            if (file.IsCheckedOut() && !file.IsCheckedOutBy(userId))
                throw new InvalidOperationException("File is checked out by another user");

            // Mark current version as not current
            var currentVersion = file.FileVersions.FirstOrDefault(v => v.IsCurrentVersion);
            if (currentVersion != null)
            {
                currentVersion.IsCurrentVersion = false;
            }

            // Create new version based on the restored version
            var restoredVersion = file.CreateNewVersion(
                GenerateNextVersionNumber(file),
                userId,
                FileChangeType.Restored,
                versionToRestore.PhysicalPath,
                versionToRestore.FileSize,
                versionToRestore.FileHash,
                $"Restored from version {versionToRestore.VersionNumber}");

            // Update file properties
            file.CurrentFileSize = versionToRestore.FileSize;
            file.CurrentFileHash = versionToRestore.FileHash;
            file.StoragePath = versionToRestore.PhysicalPath;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<object> CompareVersionsAsync(int fileId, CompareVersionsRequest request, int userId)
        {
            if (!await _permissionService.HasPermissionAsync(userId, fileId, "read"))
                throw new UnauthorizedAccessException("Access denied");

            var fromVersion = await _context.FileVersions
                .FirstOrDefaultAsync(fv => fv.Id == request.FromVersionId && fv.ProjectFileId == fileId);

            var toVersion = await _context.FileVersions
                .FirstOrDefaultAsync(fv => fv.Id == request.ToVersionId && fv.ProjectFileId == fileId);

            if (fromVersion == null || toVersion == null)
                throw new ArgumentException("One or both versions not found");

            // Basic comparison - in real implementation would use diff libraries
            return new
            {
                FromVersion = new
                {
                    fromVersion.Id,
                    fromVersion.VersionNumber,
                    fromVersion.FileSize,
                    fromVersion.FileHash,
                    fromVersion.CreatedAt,
                    fromVersion.CreatedBy
                },
                ToVersion = new
                {
                    toVersion.Id,
                    toVersion.VersionNumber,
                    toVersion.FileSize,
                    toVersion.FileHash,
                    toVersion.CreatedAt,
                    toVersion.CreatedBy
                },
                SizeChange = toVersion.FileSize - fromVersion.FileSize,
                HashChanged = fromVersion.FileHash != toVersion.FileHash,
                // In real implementation, would include actual diff content
                DiffAvailable = true
            };
        }

        public async Task<byte[]> DownloadVersionAsync(int versionId, int userId)
        {
            if (!await ValidateVersionAccessAsync(versionId, userId))
                throw new UnauthorizedAccessException("Access denied");

            var version = await _context.FileVersions
                .FirstOrDefaultAsync(fv => fv.Id == versionId && !fv.IsDeleted);

            if (version == null)
                throw new ArgumentException("Version not found");

            if (!File.Exists(version.PhysicalPath))
                throw new FileNotFoundException("Physical version file not found");

            return await File.ReadAllBytesAsync(version.PhysicalPath);
        }

        public async Task<bool> ValidateVersionAccessAsync(int versionId, int userId)
        {
            var version = await _context.FileVersions
                .Include(fv => fv.ProjectFile)
                .FirstOrDefaultAsync(fv => fv.Id == versionId);

            if (version == null)
                return false;

            return await _permissionService.HasPermissionAsync(userId, version.ProjectFileId, "read");
        }

        #region Private Methods

        private async Task<string> SaveVersionFileAsync(FileUploadDto upload, int projectId, int fileId)
        {
            var versionPath = Path.Combine(_storagePath, projectId.ToString(), "versions", fileId.ToString());
            if (!Directory.Exists(versionPath))
            {
                Directory.CreateDirectory(versionPath);
            }

            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{Path.GetExtension(upload.FileName)}";
            var filePath = Path.Combine(versionPath, fileName);

            await File.WriteAllBytesAsync(filePath, upload.Content);
            return filePath;
        }

        private string CalculateFileHash(byte[] content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(content);
            return Convert.ToBase64String(hash);
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