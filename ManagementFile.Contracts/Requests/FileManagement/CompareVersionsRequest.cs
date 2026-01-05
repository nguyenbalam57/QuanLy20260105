using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Compare versions request DTO
    /// </summary>
    public class CompareVersionsRequest
    {
        [Required]
        public int FromVersionId { get; set; }

        [Required]
        public int ToVersionId { get; set; } 
    }
}
