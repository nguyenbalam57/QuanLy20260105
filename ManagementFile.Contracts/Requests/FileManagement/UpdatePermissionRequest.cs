using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Update permission request DTO
    /// </summary>
    public class UpdatePermissionRequest
    {
        public bool? CanRead { get; set; }
        public bool? CanWrite { get; set; }
        public bool? CanDelete { get; set; }
        public bool? CanShare { get; set; }
        public bool? CanManagePermissions { get; set; }
        public bool? CanDownload { get; set; }
        public bool? CanPrint { get; set; }
        public bool? CanComment { get; set; }
        public bool? CanCheckout { get; set; }
        public bool? CanApprove { get; set; }
        public DateTime? ExpiresAt { get; set; }

        [StringLength(1000)]
        public string Reason { get; set; } = "";
    }
}
