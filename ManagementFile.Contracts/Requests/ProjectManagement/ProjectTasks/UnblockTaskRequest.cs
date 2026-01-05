using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để unblock task
    /// </summary>
    public class UnblockTaskRequest
    {
        [StringLength(500)]
        public string UnblockNotes { get; set; } = "";

        public DateTime? UnblockedAt { get; set; }
    }
}
