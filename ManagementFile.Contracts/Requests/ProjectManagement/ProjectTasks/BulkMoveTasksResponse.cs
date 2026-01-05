using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Response cho bulk move tasks operation
    /// </summary>
    public class BulkMoveTasksResponse
    {
        /// <summary>
        /// Tổng số tasks được yêu cầu di chuyển
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// Số tasks di chuyển thành công
        /// </summary>
        public int MovedTasks { get; set; }

        /// <summary>
        /// Số tasks di chuyển thất bại
        /// </summary>
        public int FailedTasks { get; set; }

        /// <summary>
        /// Message tóm tắt kết quả
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Danh sách errors chi tiết (nếu có)
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
