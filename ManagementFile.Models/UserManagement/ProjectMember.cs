using ManagementFile.Contracts.Enums;
using ManagementFile.Models.BaseModels;
using ManagementFile.Models.ProjectManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ManagementFile.Models.UserManagement
{
    /// <summary>
    /// ProjectMember - Thành viên dự án
    /// Liên kết User với Project và định nghĩa role trong project
    /// </summary>
    [Table("ProjectMembers")]
    [Index(nameof(ProjectId), nameof(UserId), IsUnique = true)]
    public class ProjectMember : BaseEntity
    {
        /// <summary>
        /// ProjectId - ID dự án
        /// </summary>
        [Required]
        public int ProjectId { get; set; }

        /// <summary>
        /// UserId - ID người dùng
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// ProjectRole - Vai trò trong dự án cụ thể
        /// ProjectManager, TeamLead, Developer, Tester, Analyst
        /// </summary>
        [Required]
        public UserRole ProjectRole { get; set; } = UserRole.Staff;

        /// <summary>
        /// JoinedAt - Ngày tham gia dự án
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// LeftAt - Ngày rời dự án (nếu có)
        /// </summary>
        public DateTime? LeftAt { get; set; }

        /// <summary>
        /// IsActive - Có đang tham gia dự án không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// AllocationPercentage - Phần trăm thời gian dành cho dự án (0-100)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal AllocationPercentage { get; set; } = 100;

        /// <summary>
        /// HourlyRate - Mức lương theo giờ (nếu có)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? HourlyRate { get; set; }

        /// <summary>
        /// Notes - Ghi chú về member trong project
        /// </summary>
        [StringLength(1000)]
        public string Notes { get; set; } = "";

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [ForeignKey(nameof(ProjectId))]
        public virtual Project Project { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        /// <summary>
        /// LeaveProject - Rời khỏi dự án
        /// </summary>
        public virtual void LeaveProject(int leftBy)
        {
            IsActive = false;
            LeftAt = DateTime.UtcNow;
            MarkAsUpdated(leftBy);
        }

        /// <summary>
        /// Khôi phục tham gia dự án
        /// </summary>
        /// <param name="rejoinedBy"></param>
        public virtual void RejoinProject(int rejoinedBy)
        {
            IsActive = true;
            LeftAt = null;
            MarkAsUpdated(rejoinedBy);
        }
    }
}
