using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectMembers
{
    /// <summary>
    /// BaseProjectMemberRequest - Lớp base chứa các thuộc tính chung cho các Project Member requests
    /// Được sử dụng làm base class cho CreateProjectMemberRequest và UpdateProjectMemberRequest
    /// </summary>
    public abstract class BaseProjectMemberRequest
    {
        /// <summary>
        /// ProjectRole - Vai trò trong dự án cụ thể
        /// Các vai trò có thể có: ProjectManager, TeamLead, Developer, Tester, Analyst, Designer, DevOps
        /// </summary>
        public UserRole ProjectRole { get; set; } = UserRole.Staff;

        /// <summary>
        /// AllocationPercentage - Phần trăm thời gian dành cho dự án (0-100)
        /// Ví dụ: 100 = full-time, 50 = part-time 50%
        /// </summary>
        [Range(0, 100, ErrorMessage = "Allocation percentage phải từ 0 đến 100")]
        public decimal AllocationPercentage { get; set; } = 100;

        /// <summary>
        /// HourlyRate - Mức lương theo giờ cho member trong project này (tùy chọn)
        /// Có thể khác với mức lương cơ bản của user, phụ thuộc vào project và role
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Hourly rate phải lớn hơn hoặc bằng 0")]
        public decimal? HourlyRate { get; set; }

        /// <summary>
        /// Notes - Ghi chú về member trong project
        /// Có thể chứa thông tin về kỹ năng đặc biệt, trách nhiệm cụ thể, hoặc các lưu ý khác
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notes không được vượt quá 1000 ký tự")]
        public string Notes { get; set; } = "";
    }
}
