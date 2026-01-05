using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.Projects
{
    /// <summary>
    /// Base request class chứa các field chung
    /// </summary>
    public abstract class BaseProjectRequest
    {
        /// <summary>Tên project</summary>
        [Required(ErrorMessage = "Tên project là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên project không được vượt quá 200 ký tự")]
        public string ProjectName { get; set; } = "";

        /// <summary>Mô tả chi tiết project</summary>
        [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string Description { get; set; } = "";

        /// <summary>Độ ưu tiên</summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Low;

        /// <summary>
        /// trạng thái
        /// </summary>
        public ProjectStatus ProjectStatus { get; set; } = ProjectStatus.Planning;

        /// <summary>ID của Client</summary>
        public int? ClientId { get; set; }

        /// <summary>Tên Client</summary>
        [StringLength(200, ErrorMessage = "Tên client không được vượt quá 200 ký tự")]
        public string ClientName { get; set; } = "";

        /// <summary>Ngày kết thúc dự kiến</summary>
        public DateTime? PlannedEndDate { get; set; }

        /// <summary>
        /// Ngày kết thúc thực tế
        /// </summary>
        public DateTime? ActualEndDate { get; set; }

        /// <summary>Ngân sách dự kiến</summary>
        //[Range(0, double.MaxValue, ErrorMessage = "Ngân sách phải >= 0")]
        //public decimal EstimatedBudget { get; set; }

        /// <summary>Số giờ dự kiến</summary>
        [Range(0, int.MaxValue, ErrorMessage = "Số giờ phải >= 0")]
        public decimal EstimatedHours { get; set; }

        /// <summary>Tiến độ (%)</summary>
        public decimal? Progress { get; set; }

        /// <summary>Project có public không</summary>
        public bool IsPublic { get; set; }

        /// <summary>Danh sách tags</summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
