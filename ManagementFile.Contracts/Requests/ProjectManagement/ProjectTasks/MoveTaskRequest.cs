using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để di chuyển task sang parent mới
    /// </summary>
    public class MoveTaskRequest
    {
        /// <summary>
        /// ID của parent task mới (null nếu muốn move về root level)
        /// </summary>
        public int? NewParentTaskId { get; set; }

        /// <summary>
        /// Lý do di chuyển (optional)
        /// </summary>
        [StringLength(500, ErrorMessage = "Reason không được vượt quá 500 ký tự")]
        public string Reason { get; set; } = "";
    }
}
