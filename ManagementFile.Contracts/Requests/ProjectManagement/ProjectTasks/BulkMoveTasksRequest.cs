using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để bulk move nhiều tasks
    /// </summary>
    public class BulkMoveTasksRequest
    {
        /// <summary>
        /// Danh sách IDs của tasks cần di chuyển
        /// </summary>
        [Required(ErrorMessage = "TaskIds là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 task")]
        [MaxLength(100, ErrorMessage = "Không thể di chuyển quá 100 tasks cùng lúc")]
        public List<int> TaskIds { get; set; } = new List<int>();

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
