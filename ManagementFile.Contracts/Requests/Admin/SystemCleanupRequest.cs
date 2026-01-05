using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.Admin
{
    /// <summary>
    /// Request để cleanup system
    /// </summary>
    public class SystemCleanupRequest
    {
        public bool CleanupExpiredSessions { get; set; } = true;
        public bool CleanupExpiredNotifications { get; set; } = true;
        public bool CleanupOldAuditLogs { get; set; } = false;
        public bool CleanupTempFiles { get; set; } = true;
        public bool CleanupSoftDeletedRecords { get; set; } = false;
        public int RetentionDays { get; set; } = 90;
    }
}
