using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    public class TaskAssignmentRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "AssignedToId phải > 0")]
        public int AssignedToId { get; set; }
    }
}
