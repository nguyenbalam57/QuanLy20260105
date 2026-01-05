using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Copy file request DTO
    /// </summary>
    public class CopyFileRequest
    {
        [Required]
        public int TargetProjectId { get; set; } = -1;

        public int TargetFolderId { get; set; } = -1;

        [StringLength(255)]
        public string NewFileName { get; set; } = "";
    }
}
