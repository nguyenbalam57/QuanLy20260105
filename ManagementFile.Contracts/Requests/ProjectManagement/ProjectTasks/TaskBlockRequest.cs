using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    public class TaskBlockRequest
    {
        [Required]
        [StringLength(500, ErrorMessage = "BlockReason không được vượt quá 500 ký tự")]
        public string BlockReason { get; set; } = "";
    }
}
