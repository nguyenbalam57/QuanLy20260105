using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    /// <summary>
    /// Request để cập nhật progress của task
    /// </summary>
    public class UpdateTaskProgressRequest
    {
        [Range(0, 100)]
        public decimal Progress { get; set; }

        public string Note { get; set; }
    }
}
