using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Create file share request DTO
    /// </summary>
    public class CreateFileShareRequest
    {
        [Required]
        public int FileId { get; set; } = -1;

        [Required]
        public string ShareType { get; set; } = "Public"; // Public, Email, Internal, External

        public int SharedWithUserId { get; set; } = -1;

        [EmailAddress]
        public string SharedWithEmail { get; set; } = "";

        [StringLength(200)]
        public string SharedWithName { get; set; } = "";

        [StringLength(200)]
        public string ShareTitle { get; set; } = "";

        [StringLength(1000)]
        public string ShareMessage { get; set; } = "";

        public string Password { get; set; } = "";
        public bool AllowDownload { get; set; } = true;
        public bool AllowPreview { get; set; } = true;
        public bool AllowComment { get; set; } = false;
        public bool AllowPrint { get; set; } = true;
        public int MaxDownloads { get; set; } = 0;
        public int MaxViews { get; set; } = 0;
        public DateTime? ExpiresAt { get; set; }
        public bool NotifyOnAccess { get; set; } = false;
        public bool NotifyOnDownload { get; set; } = false;
    }
}
