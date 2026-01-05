using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Grant permission request DTO
    /// </summary>
    public class GrantPermissionRequest
    {
        [Required]
        public int FileId { get; set; } = -1;

        public int UserId { get; set; } = -1;
        public string RoleName { get; set; } = "";

        [Required]
        public string PermissionType { get; set; } = "Individual"; // Individual, Role, Group, Public

        [Required]
        public PermissionLevel Level { get; set; } = PermissionLevel.Reader;

        public DateTime? ExpiresAt { get; set; }

        [StringLength(1000)]
        public string Reason { get; set; } = "";
    }
}
