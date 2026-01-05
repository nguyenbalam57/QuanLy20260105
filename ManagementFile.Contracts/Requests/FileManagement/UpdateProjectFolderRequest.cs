using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Update folder request DTO
    /// </summary>
    public class UpdateProjectFolderRequest
    {
        [StringLength(255)]
        public string DisplayName { get; set; } = "";

        [StringLength(1000)]
        public string Description { get; set; } = "";

        [StringLength(50)]
        public string IconName { get; set; } = "";

        [StringLength(7)]
        public string Color { get; set; } = "";

        public List<string> Tags { get; set; } = new List<string>();
    }
}
