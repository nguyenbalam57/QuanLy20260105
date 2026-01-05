using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.Admin
{
    /// <summary>
    /// Request để filter audit logs
    /// </summary>
    public class AuditLogFilterRequest
    {
        public int UserId { get; set; }
        public string EntityType { get; set; }
        public string Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }
}
