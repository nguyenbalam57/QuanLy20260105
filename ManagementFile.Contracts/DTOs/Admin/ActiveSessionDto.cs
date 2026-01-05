using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// Active session DTO
    /// </summary>
    public class ActiveSessionDto
    {
        public int Id { get; set; } = -1;
        public int UserId { get; set; } = -1;
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string SessionToken { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string IpAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
