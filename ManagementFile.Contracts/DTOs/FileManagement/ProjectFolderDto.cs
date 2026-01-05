using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// ProjectFolder response DTO
    /// </summary>
    public class ProjectFolderDto
    {
        public int Id { get; set; } 
        public int ProjectId { get; set; }
        public int ParentFolderId { get; set; }
        public string FolderName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string FolderPath { get; set; } = "";
        public int FolderLevel { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public bool IsReadOnly { get; set; }
        public int SortOrder { get; set; }
        public string IconName { get; set; } = "";
        public string Color { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UpdatedBy { get; set; } 

        // Computed properties
        public bool IsRootFolder { get; set; }
        public int TotalFiles { get; set; }
        public long TotalFileSize { get; set; }
        public int TotalSubFolders { get; set; }

        // Navigation properties
        public List<ProjectFolderDto> SubFolders { get; set; } = new List<ProjectFolderDto>();
        public List<ProjectFileDto> Files { get; set; } = new List<ProjectFileDto>();
    }
}
