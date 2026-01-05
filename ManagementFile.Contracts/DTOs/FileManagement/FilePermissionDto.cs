using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// File permission response DTO
    /// </summary>
    public class FilePermissionDto
    {
        public int Id { get; set; } = -1;
        public int ProjectFileId { get; set; } = -1;
        public int UserId { get; set; } = -1;
        public string UserName { get; set; } = "";
        public string RoleName { get; set; } = "";
        public string PermissionType { get; set; } = "";
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
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int GrantedBy { get; set; }
        public string GrantedByName { get; set; } = "";
        public DateTime GrantedAt { get; set; }
        public string PermissionLevel { get; set; } = "";
        public bool IsExpired { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsEffective { get; set; }
    }
}
