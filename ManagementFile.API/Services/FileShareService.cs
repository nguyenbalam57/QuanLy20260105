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
    /// Interface for FileShare Service
    /// </summary>
    public interface IFileShareService
    {
        // Share operations
        Task<List<FileShareDto>> GetFileSharesAsync(int fileId, int userId);
        Task<FileShareDto?> GetShareByTokenAsync(string shareToken);
        Task<FileShareDto> CreateShareAsync(CreateFileShareRequest request, int userId);
        Task<FileShareDto> UpdateShareAsync(int shareId, UpdateFileShareRequest request, int userId);
        Task<bool> DeleteShareAsync(int shareId, int userId);
        
        // Access operations
        Task<object> AccessSharedFileAsync(ShareAccessRequest request, int accessedBy = -1, string? ipAddress = null, string? userAgent = null);
        Task RecordShareAccessAsync(string shareToken, string accessType, int accessedBy = -1, string? ipAddress = null, string? userAgent = null);
        
        // Analytics
        Task<ShareAnalyticsDto> GetShareAnalyticsAsync(int shareId, int userId);
        Task<string> GenerateQRCodeAsync(int shareId, int userId);
        
        // Utility methods
        Task<bool> ValidateShareAccessAsync(int shareId, int userId);
        Task CleanupExpiredSharesAsync();
    }

    /// <summary>
    /// FileShare Service Implementation
    /// </summary>
    public class FileShareService : IFileShareService
    {
        private readonly ManagementFileDbContext _context;
        private readonly IFilePermissionService _permissionService;
        private readonly ILogger<FileShareService> _logger;
        private readonly string _baseUrl;

        public FileShareService(
            ManagementFileDbContext context,
            IFilePermissionService permissionService,
            ILogger<FileShareService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _permissionService = permissionService;
            _logger = logger;
            _baseUrl = configuration.GetValue<string>("BaseUrl") ?? "https://localhost:7190";
        }

        public async Task<List<FileShareDto>> GetFileSharesAsync(int fileId, int userId)
        {
            if (!await _permissionService.HasPermissionAsync(userId, fileId, "share"))
                return new List<FileShareDto>();

            var shares = await _context.FileShares
                .Where(fs => fs.ProjectFileId == fileId && fs.IsActive && !fs.IsDeleted)
                .OrderByDescending(fs => fs.CreatedAt)
                .ToListAsync();

            return shares.Select(MapToShareDto).ToList();
        }

        public async Task<FileShareDto?> GetShareByTokenAsync(string shareToken)
        {
            var share = await _context.FileShares
                .Include(fs => fs.ProjectFile)
                .FirstOrDefaultAsync(fs => fs.ShareToken == shareToken && fs.IsActive && !fs.IsDeleted);

            if (share == null || share.IsExpired)
                return null;

            return MapToShareDto(share);
        }

        public async Task<FileShareDto> CreateShareAsync(CreateFileShareRequest request, int userId)
        {
            if (!await _permissionService.HasPermissionAsync(userId, request.FileId, "share"))
                throw new UnauthorizedAccessException("Insufficient permissions to share file");

            var file = await _context.ProjectFiles.FindAsync(request.FileId);
            if (file == null || !file.IsActive || file.IsDeleted)
                throw new ArgumentException("File not found");

            var share = new ManagementFile.Models.FileManagement.FileShare
            {
                ProjectFileId = request.FileId,
                ShareType = request.ShareType,
                SharedWithUserId = request.SharedWithUserId,
                SharedWithEmail = request.SharedWithEmail,
                SharedWithName = request.SharedWithName,
                ShareTitle = request.ShareTitle,
                ShareMessage = request.ShareMessage,
                AllowDownload = request.AllowDownload,
                AllowPreview = request.AllowPreview,
                AllowComment = request.AllowComment,
                AllowPrint = request.AllowPrint,
                MaxDownloads = request.MaxDownloads,
                MaxViews = request.MaxViews,
                ExpiresAt = request.ExpiresAt,
                NotifyOnAccess = request.NotifyOnAccess,
                NotifyOnDownload = request.NotifyOnDownload,
                CreatedBy = userId
            };

            share.GenerateNewToken();
            share.SetPassword(request.Password);
            share.ShareUrl = $"{_baseUrl}/api/shares/{share.ShareToken}";

            file.MarkShared();

            _context.FileShares.Add(share);
            await _context.SaveChangesAsync();

            return MapToShareDto(share);
        }

        public async Task<FileShareDto> UpdateShareAsync(int shareId, UpdateFileShareRequest request, int userId)
        {
            if (!await ValidateShareAccessAsync(shareId, userId))
                throw new UnauthorizedAccessException("Access denied");

            var share = await _context.FileShares.FindAsync(shareId);
            if (share == null || !share.IsActive || share.IsDeleted)
                throw new ArgumentException("Share not found");

            // Update properties
            if (!string.IsNullOrEmpty(request.ShareTitle))
                share.ShareTitle = request.ShareTitle;

            if (!string.IsNullOrEmpty(request.ShareMessage))
                share.ShareMessage = request.ShareMessage;

            if (!string.IsNullOrEmpty(request.Password))
                share.SetPassword(request.Password);

            if (request.AllowDownload.HasValue)
                share.AllowDownload = request.AllowDownload.Value;

            if (request.AllowPreview.HasValue)
                share.AllowPreview = request.AllowPreview.Value;

            if (request.AllowComment.HasValue)
                share.AllowComment = request.AllowComment.Value;

            if (request.AllowPrint.HasValue)
                share.AllowPrint = request.AllowPrint.Value;

            if (request.MaxDownloads.HasValue)
                share.MaxDownloads = request.MaxDownloads.Value;

            if (request.MaxViews.HasValue)
                share.MaxViews = request.MaxViews.Value;

            if (request.ExpiresAt.HasValue)
                share.ExpiresAt = request.ExpiresAt.Value;

            if (request.NotifyOnAccess.HasValue)
                share.NotifyOnAccess = request.NotifyOnAccess.Value;

            if (request.NotifyOnDownload.HasValue)
                share.NotifyOnDownload = request.NotifyOnDownload.Value;

            share.MarkAsUpdated(userId);
            await _context.SaveChangesAsync();

            return MapToShareDto(share);
        }

        public async Task<bool> DeleteShareAsync(int shareId, int userId)
        {
            if (!await ValidateShareAccessAsync(shareId, userId))
                return false;

            var share = await _context.FileShares.FindAsync(shareId);
            if (share == null || share.IsDeleted)
                return false;

            share.SoftDelete(userId);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<object> AccessSharedFileAsync(ShareAccessRequest request, int accessedBy = -1, string? ipAddress = null, string? userAgent = null)
        {
            var share = await _context.FileShares
                .Include(fs => fs.ProjectFile)
                .FirstOrDefaultAsync(fs => fs.ShareToken == request.ShareToken && fs.IsActive && !fs.IsDeleted);

            if (share == null)
                throw new ArgumentException("Invalid share token");

            if (!share.IsAccessible)
                throw new InvalidOperationException("Share is not accessible (expired or limit reached)");

            // Verify password if required
            if (!share.VerifyPassword(request.Password))
                throw new UnauthorizedAccessException("Invalid password");

            // Record access
            await RecordShareAccessAsync(request.ShareToken, "view", accessedBy, ipAddress, userAgent);

            return new
            {
                Share = MapToShareDto(share),
                File = new
                {
                    share.ProjectFile.Id,
                    share.ProjectFile.FileName,
                    share.ProjectFile.DisplayName,
                    share.ProjectFile.FileExtension,
                    share.ProjectFile.MimeType,
                    share.ProjectFile.CurrentFileSize,
                    share.ProjectFile.Description
                },
                AccessAllowed = new
                {
                    share.AllowDownload,
                    share.AllowPreview,
                    share.AllowComment,
                    share.AllowPrint
                }
            };
        }

        public async Task RecordShareAccessAsync(string shareToken, string accessType, int accessedBy = -1, string? ipAddress = null, string? userAgent = null)
        {
            var share = await _context.FileShares.FirstOrDefaultAsync(fs => fs.ShareToken == shareToken);
            if (share == null)
                return;

            var access = share.RecordAccess(accessType, ipAddress ?? "", userAgent ?? "", accessedBy > 0 ? accessedBy : -1);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Share access recorded: {ShareId}, {AccessType}, {IPAddress}", 
                share.Id, accessType, ipAddress);
        }

        public async Task<ShareAnalyticsDto> GetShareAnalyticsAsync(int shareId, int userId)
        {
            if (!await ValidateShareAccessAsync(shareId, userId))
                throw new UnauthorizedAccessException("Access denied");

            var share = await _context.FileShares
                .Include(fs => fs.ShareAccesses.OrderByDescending(sa => sa.AccessedAt).Take(50))
                .FirstOrDefaultAsync(fs => fs.Id == shareId);

            if (share == null)
                throw new ArgumentException("Share not found");

            var accesses = share.ShareAccesses.ToList();

            return new ShareAnalyticsDto
            {
                ShareId = shareId,
                TotalAccesses = accesses.Count,
                TotalDownloads = accesses.Count(a => a.AccessType.ToLower() == "download"),
                TotalViews = accesses.Count(a => a.AccessType.ToLower() == "view"),
                FirstAccessedAt = accesses.Count > 0 ? accesses.Min(a => a.AccessedAt) : null,
                LastAccessedAt = accesses.Count > 0 ? accesses.Max(a => a.AccessedAt) : null,
                RecentAccesses = accesses.Take(10).Select(a => new ShareAccessDto
                {
                    Id = a.Id,
                    AccessType = a.AccessType,
                    AccessedAt = a.AccessedAt,
                    IPAddress = a.IPAddress,
                    UserAgent = a.UserAgent,
                    AccessedBy = a.AccessedBy,
                    IsSuccessful = a.IsSuccessful
                }).ToList()
            };
        }

        public async Task<string> GenerateQRCodeAsync(int shareId, int userId)
        {
            if (!await ValidateShareAccessAsync(shareId, userId))
                throw new UnauthorizedAccessException("Access denied");

            var share = await _context.FileShares.FindAsync(shareId);
            if (share == null)
                throw new ArgumentException("Share not found");

            // In a real implementation, would use QR code generation library
            // For now, return a placeholder
            var qrCodeData = $"data:image/svg+xml;base64,{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"<svg>QR Code for {share.ShareUrl}</svg>"))}";
            
            share.QRCodeData = qrCodeData;
            await _context.SaveChangesAsync();

            return qrCodeData;
        }

        public async Task<bool> ValidateShareAccessAsync(int shareId, int userId)
        {
            var share = await _context.FileShares.Include(fs => fs.ProjectFile).FirstOrDefaultAsync(fs => fs.Id == shareId);
            if (share == null)
                return false;

            // Check if user has permission on the underlying file
            return await _permissionService.HasPermissionAsync(userId, share.ProjectFileId, "share");
        }

        public async Task CleanupExpiredSharesAsync()
        {
            var expiredShares = await _context.FileShares
                .Where(fs => fs.IsActive && !fs.IsDeleted && 
                            fs.ExpiresAt.HasValue && fs.ExpiresAt.Value <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var share in expiredShares)
            {
                share.IsActive = false;
                share.MarkAsUpdated();
            }

            if (expiredShares.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired shares", expiredShares.Count);
            }
        }

        #region Private Methods

        private FileShareDto MapToShareDto(ManagementFile.Models.FileManagement.FileShare share)
        {
            return new FileShareDto
            {
                Id = share.Id,
                ProjectFileId = share.ProjectFileId,
                ShareToken = share.ShareToken,
                ShareType = share.ShareType,
                SharedWithUserId = share.SharedWithUserId,
                SharedWithEmail = share.SharedWithEmail,
                SharedWithName = share.SharedWithName,
                ShareTitle = share.ShareTitle,
                ShareMessage = share.ShareMessage,
                IsActive = share.IsActive,
                RequirePassword = share.RequirePassword,
                AllowDownload = share.AllowDownload,
                AllowPreview = share.AllowPreview,
                AllowComment = share.AllowComment,
                AllowPrint = share.AllowPrint,
                MaxDownloads = share.MaxDownloads,
                CurrentDownloads = share.CurrentDownloads,
                MaxViews = share.MaxViews,
                CurrentViews = share.CurrentViews,
                ExpiresAt = share.ExpiresAt,
                LastAccessedAt = share.LastAccessedAt,
                ShareUrl = share.ShareUrl,
                CreatedAt = share.CreatedAt,
                CreatedBy = share.CreatedBy,
                CreatedByName = "", // Would need user lookup
                IsExpired = share.IsExpired,
                IsDownloadLimitReached = share.IsDownloadLimitReached,
                IsViewLimitReached = share.IsViewLimitReached,
                IsAccessible = share.IsAccessible,
                RemainingDownloads = share.RemainingDownloads,
                RemainingViews = share.RemainingViews,
                TimeUntilExpiry = share.TimeUntilExpiry
            };
        }

        #endregion
    }
}