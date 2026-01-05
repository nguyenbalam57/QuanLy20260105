using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để unassign task
    /// </summary>
    public class UnassignTaskRequest
    {
        [StringLength(500)]
        public string UnassignmentNotes { get; set; } = "";
    }
}
