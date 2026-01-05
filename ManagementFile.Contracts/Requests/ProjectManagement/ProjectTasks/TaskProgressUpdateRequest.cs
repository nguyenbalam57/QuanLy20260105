using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    public class TaskProgressUpdateRequest
    {
        [Required]
        [Range(0, 100, ErrorMessage = "Progress phải nằm trong khoảng 0-100")]
        public decimal Progress { get; set; }

        
        public int ActualHours { get; set; }
    }
}
