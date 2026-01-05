using ManagementFile.Contracts.Enums;
using ManagementFile.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.AuditAndLogging
{
    /// <summary>
    /// AuditLog - Nhật ký kiểm toán hệ thống
    /// Ghi lại tất cả thao tác quan trọng
    /// </summary>
    [Table("AuditLogs")]
    [Index(nameof(EntityType), nameof(EntityId), nameof(Action))]
    [Index(nameof(UserId), nameof(CreatedAt))]
    public class AuditLog : BaseEntity
    {
        /// <summary>
        /// UserId - ID người thực hiện thao tác
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// EntityType - Loại entity bị thay đổi
        /// </summary>
        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = "";

        /// <summary>
        /// EntityId - ID của entity bị thay đổi
        /// </summary>
        [Required]
        public int EntityId { get; set; } = -1;

        /// <summary>
        /// Action - Hành động thực hiện
        /// </summary>
        public AuditAction Action { get; set; } = AuditAction.Read;

        /// <summary>
        /// OldValues - Giá trị cũ (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string OldValues { get; set; } = "";

        /// <summary>
        /// NewValues - Giá trị mới (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string NewValues { get; set; } = "";

        /// <summary>
        /// Changes - Chi tiết các field thay đổi (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Changes { get; set; } = "";

        /// <summary>
        /// IPAddress - IP thực hiện thao tác
        /// </summary>
        [StringLength(45)]
        public string IPAddress { get; set; } = "";

        /// <summary>
        /// UserAgent - Thông tin browser/app
        /// </summary>
        [StringLength(500)]
        public string UserAgent { get; set; } = "";

        /// <summary>
        /// SessionId - ID session
        /// </summary>
        [StringLength(450)]
        public int SessionId { get; set; }

        /// <summary>
        /// Notes - Ghi chú bổ sung
        /// </summary>
        [StringLength(1000)]
        public string Notes { get; set; } = "";
    }
}
