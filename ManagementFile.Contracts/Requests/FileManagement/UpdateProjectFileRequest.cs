using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Update file metadata request DTO
    /// </summary>
    public class UpdateProjectFileRequest
    {
        [StringLength(255)]
        public string DisplayName { get; set; } = "";

        [StringLength(2000)]
        public string Description { get; set; } = "";

        public bool? IsPublic { get; set; }
        public bool? IsReadOnly { get; set; }
        public bool? RequireApproval { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
