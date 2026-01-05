using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Update file share request DTO
    /// </summary>
    public class UpdateFileShareRequest
    {
        [StringLength(200)]
        public string ShareTitle { get; set; } = "";

        [StringLength(1000)]
        public string ShareMessage { get; set; } = "";

        public string Password { get; set; } = "";
        public bool? AllowDownload { get; set; }
        public bool? AllowPreview { get; set; }
        public bool? AllowComment { get; set; }
        public bool? AllowPrint { get; set; }
        public int? MaxDownloads { get; set; }
        public int? MaxViews { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool? NotifyOnAccess { get; set; }
        public bool? NotifyOnDownload { get; set; }
    }
}
