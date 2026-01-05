using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    /// <summary>
    /// ProjectDto - Data Transfer Object cho Project
    /// Dùng để trả về thông tin project từ API
    /// </summary>
    public class ProjectDto
    {

        /// <summary>ID duy nhất của project</summary>
        public int Id { get; set; }

        /// <summary>Mã project (unique)</summary>
        public string ProjectCode { get; set; } = "";

        /// <summary>Tên project</summary>
        public string ProjectName { get; set; } = "";

        /// <summary>Mô tả chi tiết project</summary>
        public string Description { get; set; } = "";

        /// <summary>Trạng thái project (enum string)</summary>
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

        /// <summary>Độ ưu tiên (enum string)</summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>ID của Project Manager</summary>
        public int? ProjectManagerId { get; set; }

        /// <summary>Tên Project Manager</summary>
        public string ProjectManagerName { get; set; } = "";

        /// <summary>ID của Client</summary>
        public int? ClientId { get; set; }

        /// <summary>Tên Client</summary>
        public string ClientName { get; set; } = "";

        /// <summary>Ngày bắt đầu project</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>Ngày kết thúc dự kiến</summary>
        public DateTime? PlannedEndDate { get; set; }

        /// <summary>Ngày kết thúc thực tế</summary>
        public DateTime? ActualEndDate { get; set; }

        /// <summary>Ngân sách dự kiến</summary>
        public decimal EstimatedBudget { get; set; }

        /// <summary>Ngân sách thực tế đã sử dụng</summary>
        public decimal ActualBudget { get; set; }

        /// <summary>Số giờ dự kiến</summary>
        public decimal EstimatedHours { get; set; }

        /// <summary>Số giờ thực tế đã làm</summary>
        public decimal ActualHours { get; set; }

        /// <summary>Phần trăm hoàn thành (0-100)</summary>
        public decimal CompletionPercentage { get; set; }

        /// <summary>Project có đang hoạt động không</summary>
        public bool IsActive { get; set; }

        /// <summary>Project có public không</summary>
        public bool IsPublic { get; set; }

        /// <summary>Danh sách tags</summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>Ngày tạo</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Người tạo</summary>
        public int CreatedBy { get; set; }

        /// <summary>Ngày cập nhật cuối</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Người cập nhật cuối</summary>
        public int UpdatedBy { get; set; }

        // Computed Properties - Chỉ có trong DTO
        /// <summary>Project có quá hạn không (computed)</summary>
        public bool IsOverdue { get; set; }

        /// <summary>Project đã hoàn thành chưa (computed)</summary>
        public bool IsCompleted { get; set; }

        /// <summary>Chênh lệch ngân sách (computed)</summary>
        public decimal BudgetVariance { get; set; }

        /// <summary>Chênh lệch giờ làm (computed)</summary>
        public int HourVariance { get; set; }


        public int TotalTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int TotalMembers { get; set; }


        /// <summary>
        /// ID của project cha (nếu đây là sub-project)
        /// null = project gốc, có giá trị = sub-project của project khác
        /// </summary>
        public int? ProjectParentId { get; set; }

        /// <summary>
        /// Tên của project cha
        /// </summary>
        public string ProjectParentName { get; set; }

        /// <summary>
        /// Tổng số sub-projects trực tiếp
        /// Chỉ đếm children level 1, không đếm nested deeper
        /// </summary>
        public int TotalChildCount { get; set; }

        /// <summary>
        /// Danh sách sub-projects
        /// Load lazy hoặc eager tùy business need
        /// </summary>
        public List<ProjectDto> Children { get; set; } = new List<ProjectDto>();


    }
}
