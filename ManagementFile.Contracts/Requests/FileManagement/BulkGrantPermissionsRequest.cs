using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Bulk grant permissions request DTO
    /// </summary>
    public class BulkGrantPermissionsRequest
    {
        [Required]
        public List<int> FileIds { get; set; } = new List<int>();

        public int UserId { get; set; } = -1;
        public string RoleName { get; set; } = "";

        [Required]
        public string PermissionType { get; set; } = "Individual";

        [Required]
        public PermissionLevel Level { get; set; } = PermissionLevel.Reader;

        public DateTime? ExpiresAt { get; set; }

        [StringLength(1000)]
        public string Reason { get; set; } = "";
    }
}
