using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// File statistics DTO
    /// </summary>
    public class FileStatisticsDto
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public long AverageFileSize { get; set; }
        public int NewFiles { get; set; }
        public int CheckedOutFiles { get; set; }
        public int PendingApprovals { get; set; }
        public Dictionary<string, FileTypeStats> FilesByType { get; set; } = new Dictionary<string, FileTypeStats>();
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
