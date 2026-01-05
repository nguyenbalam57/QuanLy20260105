using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Create version request DTO
    /// </summary>
    public class CreateVersionRequest
    {
        [Required]
        [StringLength(50)]
        public string VersionNumber { get; set; } = "";

        public FileChangeType ChangeType { get; set; } = FileChangeType.Modified;

        [StringLength(2000)]
        public string VersionNotes { get; set; } = "";
    }
}
