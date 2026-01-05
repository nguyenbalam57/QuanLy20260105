using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// File share response DTO
    /// </summary>
    public class FileShareDto
    {
        public int Id { get; set; }
        public int ProjectFileId { get; set; } = -1;
        public string ShareToken { get; set; } = "";
        public string ShareType { get; set; } = "";
        public int SharedWithUserId { get; set; } = -1;
        public string SharedWithEmail { get; set; } = "";
        public string SharedWithName { get; set; } = "";
        public string ShareTitle { get; set; } = "";
        public string ShareMessage { get; set; } = "";
        public bool IsActive { get; set; }
        public bool RequirePassword { get; set; }
        public bool AllowDownload { get; set; }
        public bool AllowPreview { get; set; }
        public bool AllowComment { get; set; }
        public bool AllowPrint { get; set; }
        public int MaxDownloads { get; set; }
        public int CurrentDownloads { get; set; }
        public int MaxViews { get; set; }
        public int CurrentViews { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public string ShareUrl { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; } 
        public string CreatedByName { get; set; } = "";

        // Computed properties
        public bool IsExpired { get; set; }
        public bool IsDownloadLimitReached { get; set; }
        public bool IsViewLimitReached { get; set; }
        public bool IsAccessible { get; set; }
        public int RemainingDownloads { get; set; }
        public int RemainingViews { get; set; }
        public TimeSpan? TimeUntilExpiry { get; set; }
    }
}
