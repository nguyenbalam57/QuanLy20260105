using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// File permission result DTO
    /// </summary>
    public class FilePermissionResult
    {
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanDelete { get; set; }
        public bool CanShare { get; set; }
        public bool CanManagePermissions { get; set; }
        public bool CanDownload { get; set; }
        public bool CanPrint { get; set; }
        public bool CanComment { get; set; }
        public bool CanCheckout { get; set; }
        public bool CanApprove { get; set; }
        public string PermissionLevel { get; set; } = "None";
        public string Source { get; set; } = ""; // "Direct", "Role", "Inherited"
        public DateTime? ExpiresAt { get; set; }
    }
}
