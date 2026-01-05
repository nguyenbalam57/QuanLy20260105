using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// System cleanup result DTO
    /// </summary>
    public class SystemCleanupResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int ExpiredSessionsDeleted { get; set; }
        public int ExpiredNotificationsDeleted { get; set; }
        public int OldAuditLogsDeleted { get; set; }
        public int TempFilesDeleted { get; set; }
        public long TempFilesSize { get; set; }
        public int SoftDeletedRecordsDeleted { get; set; }
        public int ExpiredSessionsRemoved { get; set; }
        public int ExpiredNotificationsRemoved { get; set; }
        public int OldAuditLogsRemoved { get; set; }
        public int SoftDeletedRecordsRemoved { get; set; }
        public DateTime ExecutedAt { get; set; }
        public DateTime CleanupDate { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
