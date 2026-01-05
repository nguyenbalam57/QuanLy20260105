using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// ProjectFile response DTO
    /// </summary>
    public class ProjectFileDto
    {
        public int Id { get; set; } = -1;
        public int ProjectId { get; set; } = -1;
        public int FolderId { get; set; } = -1;
        public string FileName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string FileExtension { get; set; } = "";
        public string FileType { get; set; } = "";
        public string MimeType { get; set; } = "";
        public long CurrentFileSize { get; set; }
        public string CurrentFileHash { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public bool IsReadOnly { get; set; }
        public bool RequireApproval { get; set; }
        public string ApprovalStatus { get; set; } = "";
        public int ApprovedBy { get; set; } = -1;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public int LastAccessedBy { get; set; }
        public int DownloadCount { get; set; }
        public int ViewCount { get; set; }
        public int ShareCount { get; set; }
        public int CheckoutBy { get; set; } 
        public DateTime? CheckoutAt { get; set; }
        public DateTime? ExpectedCheckinAt { get; set; }
        public string ThumbnailPath { get; set; } = "";
        public string PreviewPath { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; } = -1;
        public DateTime? UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }  = -1;   

        // Computed properties
        public bool IsCheckedOut { get; set; }
        public bool IsOverdueCheckout { get; set; }
        public string CurrentVersion { get; set; } = "";
        public FilePermissionResult EffectivePermissions { get; set; } = new FilePermissionResult();
    }
}
