using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// Share analytics response DTO
    /// </summary>
    public class ShareAnalyticsDto
    {
        public int ShareId { get; set; } = -1;
        public int TotalAccesses { get; set; }
        public int TotalDownloads { get; set; }
        public int TotalViews { get; set; }
        public DateTime? FirstAccessedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public List<ShareAccessDto> RecentAccesses { get; set; } = new List<ShareAccessDto>();
    }
}
