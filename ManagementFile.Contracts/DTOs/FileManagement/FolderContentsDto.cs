using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// Folder contents response DTO
    /// </summary>
    public class FolderContentsDto
    {
        public ProjectFolderDto Folder { get; set; } = new ProjectFolderDto();
        public List<ProjectFolderDto> SubFolders { get; set; } = new List<ProjectFolderDto>();
        public List<ProjectFileDto> Files { get; set; } = new List<ProjectFileDto>();
        public List<ProjectFolderDto> Breadcrumb { get; set; } = new List<ProjectFolderDto>();
    }
}
