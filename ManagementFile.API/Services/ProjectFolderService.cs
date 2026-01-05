using ManagementFile.API.Data;
using ManagementFile.Contracts.DTOs.FileManagement;
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
    /// Interface for ProjectFolder Service
    /// </summary>
    public interface IProjectFolderService
    {
        // CRUD operations
        Task<ProjectFolderDto?> GetFolderByIdAsync(int folderId, int userId);
        Task<List<ProjectFolderDto>> GetProjectFoldersAsync(int projectId, int userId);
        Task<ProjectFolderDto> CreateFolderAsync(CreateProjectFolderRequest request, int userId);
        Task<ProjectFolderDto> UpdateFolderAsync(int folderId, UpdateProjectFolderRequest request, int userId);
        Task<bool> DeleteFolderAsync(int folderId, int userId);
        
        // Folder operations
        Task<ProjectFolderDto> MoveFolderAsync(int folderId, MoveFolderRequest request, int userId);
        Task<ProjectFolderDto> CreateSubFolderAsync(int parentFolderId, CreateProjectFolderRequest request, int userId);
        Task<List<ProjectFolderDto>> GetBreadcrumbAsync(int folderId, int userId);
        Task<FolderContentsDto> GetFolderContentsAsync(int folderId, int userId);
        
        // Utility methods
        Task<bool> ValidateFolderAccessAsync(int folderId, int userId, string permissionType = "read");
    }

    /// <summary>
    /// ProjectFolder Service Implementation
    /// </summary>
    public class ProjectFolderService : IProjectFolderService
    {
        private readonly ManagementFileDbContext _context;
        private readonly IFilePermissionService _permissionService;
        private readonly ILogger<ProjectFolderService> _logger;

        public ProjectFolderService(
            ManagementFileDbContext context, 
            IFilePermissionService permissionService,
            ILogger<ProjectFolderService> logger)
        {
            _context = context;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<ProjectFolderDto?> GetFolderByIdAsync(int folderId, int userId)
        {
            if (!await ValidateFolderAccessAsync(folderId, userId))
                return null;

            var folder = await _context.ProjectFolders
                .Include(f => f.Project)
                .Include(f => f.ParentFolder)
                .FirstOrDefaultAsync(f => f.Id == folderId && f.IsActive && !f.IsDeleted);

            if (folder == null)
                return null;

            return await MapToFolderDto(folder);
        }

        public async Task<List<ProjectFolderDto>> GetProjectFoldersAsync(int projectId, int userId)
        {
            // Get project to validate access
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || !project.IsActive)
                return new List<ProjectFolderDto>();

            var folders = await _context.ProjectFolders
                .Include(f => f.ParentFolder)
                .Where(f => f.ProjectId == projectId && f.IsActive && !f.IsDeleted)
                .OrderBy(f => f.FolderLevel)
                .ThenBy(f => f.SortOrder)
                .ThenBy(f => f.FolderName)
                .ToListAsync();

            var folderDtos = new List<ProjectFolderDto>();
            foreach (var folder in folders)
            {
                if (await ValidateFolderAccessAsync(folder.Id, userId))
                {
                    folderDtos.Add(await MapToFolderDto(folder));
                }
            }

            return folderDtos;
        }

        public async Task<ProjectFolderDto> CreateFolderAsync(CreateProjectFolderRequest request, int userId)
        {
            // Validate project access
            var project = await _context.Projects.FindAsync(request.ProjectId);
            if (project == null || !project.IsActive)
                throw new ArgumentException("Project not found or inactive");

            // Validate parent folder if specified
            if (request.ParentFolderId >= 0)
            {
                var parentFolder = await _context.ProjectFolders.FindAsync(request.ParentFolderId);
                if (parentFolder == null || parentFolder.ProjectId != request.ProjectId)
                    throw new ArgumentException("Invalid parent folder");

                if (!await ValidateFolderAccessAsync(request.ParentFolderId, userId, "write"))
                    throw new UnauthorizedAccessException("Insufficient permissions to create folder in this location");
            }

            // Check for duplicate folder name in same location
            var existingFolder = await _context.ProjectFolders
                .AnyAsync(f => f.ProjectId == request.ProjectId && 
                              f.ParentFolderId == request.ParentFolderId && 
                              f.FolderName.Equals(request.FolderName, StringComparison.OrdinalIgnoreCase) &&
                              f.IsActive && !f.IsDeleted);

            if (existingFolder)
                throw new InvalidOperationException($"Folder '{request.FolderName}' already exists in this location");

            // Get parent folder level
            int folderLevel = 0;
            if (request.ParentFolderId >= 0)
            {
                var parent = await _context.ProjectFolders.FindAsync(request.ParentFolderId);
                folderLevel = parent?.FolderLevel + 1 ?? 0;
            }

            // Create new folder
            var folder = new ProjectFolder
            {
                ProjectId = request.ProjectId,
                ParentFolderId = request.ParentFolderId,
                FolderName = request.FolderName,
                DisplayName = !string.IsNullOrEmpty(request.DisplayName) ? request.DisplayName : request.FolderName,
                Description = request.Description,
                FolderLevel = folderLevel,
                IconName = request.IconName,
                Color = request.Color,
                IsActive = true,
                CreatedBy = userId
            };

            folder.SetTags(request.Tags);
            folder.UpdateFolderPath();

            _context.ProjectFolders.Add(folder);
            await _context.SaveChangesAsync();

            return await MapToFolderDto(folder);
        }

        public async Task<ProjectFolderDto> UpdateFolderAsync(int folderId, UpdateProjectFolderRequest request, int userId)
        {
            if (!await ValidateFolderAccessAsync(folderId, userId, "write"))
                throw new UnauthorizedAccessException("Insufficient permissions to update folder");

            var folder = await _context.ProjectFolders.FindAsync(folderId);
            if (folder == null || !folder.IsActive || folder.IsDeleted)
                throw new ArgumentException("Folder not found");

            // Update properties
            if (!string.IsNullOrEmpty(request.DisplayName))
                folder.DisplayName = request.DisplayName;

            if (!string.IsNullOrEmpty(request.Description))
                folder.Description = request.Description;

            if (!string.IsNullOrEmpty(request.IconName))
                folder.IconName = request.IconName;

            if (!string.IsNullOrEmpty(request.Color))
                folder.Color = request.Color;

            if (request.Tags.Any())
                folder.SetTags(request.Tags);

            folder.MarkAsUpdated(userId);
            await _context.SaveChangesAsync();

            return await MapToFolderDto(folder);
        }

        public async Task<bool> DeleteFolderAsync(int folderId, int userId)
        {
            if (!await ValidateFolderAccessAsync(folderId, userId, "delete"))
                return false;

            var folder = await _context.ProjectFolders
                .Include(f => f.SubFolders)
                .Include(f => f.ProjectFiles)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null || folder.IsDeleted)
                return false;

            // Check if folder has active subfolders or files
            var hasActiveSubFolders = folder.SubFolders.Any(sf => sf.IsActive && !sf.IsDeleted);
            var hasActiveFiles = folder.ProjectFiles.Any(pf => pf.IsActive && !pf.IsDeleted);

            if (hasActiveSubFolders || hasActiveFiles)
                throw new InvalidOperationException("Cannot delete folder that contains active subfolders or files");

            // Soft delete
            folder.SoftDelete(userId);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ProjectFolderDto> MoveFolderAsync(int folderId, MoveFolderRequest request, int userId)
        {
            if (!await ValidateFolderAccessAsync(folderId, userId, "write"))
                throw new UnauthorizedAccessException("Insufficient permissions to move folder");

            var folder = await _context.ProjectFolders.FindAsync(folderId);
            if (folder == null || !folder.IsActive || folder.IsDeleted)
                throw new ArgumentException("Folder not found");

            // Validate target parent folder
            if (request.NewParentFolderId >= 0)
            {
                var targetParent = await _context.ProjectFolders.FindAsync(request.NewParentFolderId);
                if (targetParent == null || targetParent.ProjectId != folder.ProjectId)
                    throw new ArgumentException("Invalid target parent folder");

                if (!await ValidateFolderAccessAsync(request.NewParentFolderId, userId, "write"))
                    throw new UnauthorizedAccessException("Insufficient permissions to move folder to target location");
            }

            // Check if move is valid (not moving to itself or its descendants)
            if (!folder.CanMoveTo(request.NewParentFolderId))
                throw new InvalidOperationException("Cannot move folder to this location");

            // Check for name conflicts in target location
            var conflictingFolder = await _context.ProjectFolders
                .AnyAsync(f => f.ProjectId == folder.ProjectId &&
                              f.ParentFolderId == request.NewParentFolderId &&
                              f.FolderName.Equals(folder.FolderName, StringComparison.OrdinalIgnoreCase) &&
                              f.Id != folderId &&
                              f.IsActive && !f.IsDeleted);

            if (conflictingFolder)
                throw new InvalidOperationException($"A folder with name '{folder.FolderName}' already exists in the target location");

            // Update folder level
            int newLevel = 0;
            if (request.NewParentFolderId >= 0)
            {
                var newParent = await _context.ProjectFolders.FindAsync(request.NewParentFolderId);
                newLevel = newParent?.FolderLevel + 1 ?? 0;
            }

            // Move folder
            folder.ParentFolderId = request.NewParentFolderId;
            folder.FolderLevel = newLevel;
            folder.UpdateFolderPath();
            folder.MarkAsUpdated(userId);

            // Update levels for all descendant folders
            await UpdateDescendantFolderLevelsAsync(folder);

            await _context.SaveChangesAsync();

            return await MapToFolderDto(folder);
        }

        public async Task<ProjectFolderDto> CreateSubFolderAsync(int parentFolderId, CreateProjectFolderRequest request, int userId)
        {
            if (!await ValidateFolderAccessAsync(parentFolderId, userId, "write"))
                throw new UnauthorizedAccessException("Insufficient permissions to create subfolder");

            var parentFolder = await _context.ProjectFolders.FindAsync(parentFolderId);
            if (parentFolder == null || !parentFolder.IsActive || parentFolder.IsDeleted)
                throw new ArgumentException("Parent folder not found");

            // Set the parent folder ID in the request
            request.ParentFolderId = parentFolderId;
            request.ProjectId = parentFolder.ProjectId;

            return await CreateFolderAsync(request, userId);
        }

        public async Task<List<ProjectFolderDto>> GetBreadcrumbAsync(int folderId, int userId)
        {
            if (!await ValidateFolderAccessAsync(folderId, userId))
                return new List<ProjectFolderDto>();

            var folder = await _context.ProjectFolders
                .Include(f => f.ParentFolder)
                .FirstOrDefaultAsync(f => f.Id == folderId && f.IsActive && !f.IsDeleted);

            if (folder == null)
                return new List<ProjectFolderDto>();

            var breadcrumb = new List<ProjectFolder>();
            var current = folder;

            while (current != null)
            {
                breadcrumb.Insert(0, current);
                current = current.ParentFolder;
            }

            var result = new List<ProjectFolderDto>();
            foreach (var breadcrumbFolder in breadcrumb)
            {
                result.Add(await MapToFolderDto(breadcrumbFolder));
            }

            return result;
        }

        public async Task<FolderContentsDto> GetFolderContentsAsync(int folderId, int userId)
        {
            if (!await ValidateFolderAccessAsync(folderId, userId))
                throw new UnauthorizedAccessException("Access denied to folder");

            var folder = await _context.ProjectFolders
                .Include(f => f.SubFolders.Where(sf => sf.IsActive && !sf.IsDeleted))
                .Include(f => f.ProjectFiles.Where(pf => pf.IsActive && !pf.IsDeleted))
                .FirstOrDefaultAsync(f => f.Id == folderId && f.IsActive && !f.IsDeleted);

            if (folder == null)
                throw new ArgumentException("Folder not found");

            var result = new FolderContentsDto
            {
                Folder = await MapToFolderDto(folder),
                Breadcrumb = await GetBreadcrumbAsync(folderId, userId)
            };

            // Get subfolders with access validation
            foreach (var subFolder in folder.SubFolders)
            {
                if (await ValidateFolderAccessAsync(subFolder.Id, userId))
                {
                    result.SubFolders.Add(await MapToFolderDto(subFolder));
                }
            }

            // Get files with access validation
            foreach (var file in folder.ProjectFiles)
            {
                if (await _permissionService.HasPermissionAsync(userId, file.Id, "read"))
                {
                    // Map file to DTO (would need to inject file service or create mapping here)
                    // For now, skip file mapping - should be handled by ProjectFileService
                }
            }

            return result;
        }

        public async Task<bool> ValidateFolderAccessAsync(int folderId, int userId, string permissionType = "read")
        {
            var folder = await _context.ProjectFolders
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                return false;

            // For now, basic project member check
            // In a real implementation, would check folder-specific permissions
            var isMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == folder.ProjectId && pm.UserId == userId && pm.IsActive);

            return isMember;
        }

        #region Private Methods

        private async Task<ProjectFolderDto> MapToFolderDto(ProjectFolder folder)
        {
            return new ProjectFolderDto
            {
                Id = folder.Id,
                ProjectId = folder.ProjectId,
                ParentFolderId = folder.ParentFolderId,
                FolderName = folder.FolderName,
                DisplayName = folder.DisplayName,
                Description = folder.Description,
                FolderPath = folder.FolderPath,
                FolderLevel = folder.FolderLevel,
                IsActive = folder.IsActive,
                IsPublic = folder.IsPublic,
                IsReadOnly = folder.IsReadOnly,
                SortOrder = folder.SortOrder,
                IconName = folder.IconName,
                Color = folder.Color,
                Tags = folder.GetTags(),
                CreatedAt = folder.CreatedAt,
                CreatedBy = folder.CreatedBy,
                UpdatedAt = folder.UpdatedAt,
                UpdatedBy = folder.UpdatedBy,
                IsRootFolder = folder.IsRootFolder,
                TotalFiles = folder.TotalFiles,
                TotalFileSize = folder.TotalFileSize,
                TotalSubFolders = folder.TotalSubFolders
            };
        }

        private async Task UpdateDescendantFolderLevelsAsync(ProjectFolder parentFolder)
        {
            var descendants = await _context.ProjectFolders
                .Where(f => f.ParentFolderId == parentFolder.Id && f.IsActive && !f.IsDeleted)
                .ToListAsync();

            foreach (var descendant in descendants)
            {
                descendant.FolderLevel = parentFolder.FolderLevel + 1;
                descendant.UpdateFolderPath();
                
                // Recursively update descendants
                await UpdateDescendantFolderLevelsAsync(descendant);
            }
        }

        #endregion
    }
}