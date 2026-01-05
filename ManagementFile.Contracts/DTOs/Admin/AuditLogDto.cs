using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// Audit log DTO
    /// </summary>
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string EntityType { get; set; } = "";
        public int EntityId { get; set; }
        public string Action { get; set; } = "";
        public string OldValues { get; set; } = "";
        public string NewValues { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string IpAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
    }
}
