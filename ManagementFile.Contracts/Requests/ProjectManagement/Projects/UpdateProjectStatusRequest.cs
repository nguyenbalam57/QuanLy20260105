using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.Projects
{
    /// <summary>
    /// UpdateProjectStatusRequest - Request riêng để cập nhật trạng thái
    /// </summary>
    public class UpdateProjectStatusRequest
    {
        /// <summary>Trạng thái mới</summary>
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public ProjectStatus Status { get; set; }

    }
}
