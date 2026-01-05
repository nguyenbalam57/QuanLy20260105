using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// File statistics DTO
    /// </summary>
    public class FileStatsDto
    {
        public int FileId { get; set; } = -1;
        public int TotalVersions { get; set; }
        public int TotalComments { get; set; }
        public int TotalShares { get; set; }
        public int TotalPermissions { get; set; }
        public int ViewCount { get; set; }
        public int DownloadCount { get; set; }
        public int ShareCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public int LastAccessedBy { get; set; } = -1;
        public List<FileVersionDto> RecentVersions { get; set; } = new List<FileVersionDto>();
        public List<FileCommentDto> RecentComments { get; set; } = new List<FileCommentDto>();
    }
}
