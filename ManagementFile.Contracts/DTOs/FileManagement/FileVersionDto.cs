using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// FileVersion response DTO
    /// </summary>
    public class FileVersionDto
    {
        public int Id { get; set; } 
        public int ProjectFileId { get; set; }
        public string VersionNumber { get; set; } = "";
        public FileChangeType ChangeType { get; set; }
        public long FileSize { get; set; }
        public string FileHash { get; set; } = "";
        public string VersionNotes { get; set; } = "";
        public bool IsCurrentVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; } 
        public string CreatedByName { get; set; } = "";
    }
}
