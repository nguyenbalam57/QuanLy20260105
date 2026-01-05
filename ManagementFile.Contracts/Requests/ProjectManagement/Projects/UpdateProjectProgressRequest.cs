using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.Projects
{
    /// <summary>
    /// UpdateProjectProgressRequest - Request riêng để cập nhật tiến độ
    /// </summary>
    public class UpdateProjectProgressRequest
    {
        /// <summary>Phần trăm hoàn thành mới</summary>
        [Range(0, 100, ErrorMessage = "Phần trăm hoàn thành phải từ 0-100")]
        public decimal CompletionPercentage { get; set; }

        /// <summary>Số giờ thực tế đã làm</summary>
        [Range(0, int.MaxValue, ErrorMessage = "Số giờ phải >= 0")]
        public int ActualHours { get; set; }

    }
}
