using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// Approve/Reject request DTO
    /// </summary>
    public class ApprovalRequest
    {
        [Required]
        public string Action { get; set; } = ""; // "approve" or "reject"

        [StringLength(1000)]
        public string Notes { get; set; } = "";
    }
}
