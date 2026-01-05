using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Create/Upload file request DTO
    /// </summary>
    public class CreateProjectFileRequest
    {
        [Required]
        public int ProjectId { get; set; } 

        public int FolderId { get; set; } 

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = "";

        [StringLength(255)]
        public string DisplayName { get; set; } = "";

        [StringLength(2000)]
        public string Description { get; set; } = "";

        public bool IsPublic { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;
        public bool RequireApproval { get; set; } = false;
        public List<string> Tags { get; set; } = new List<string>();
    }
}
