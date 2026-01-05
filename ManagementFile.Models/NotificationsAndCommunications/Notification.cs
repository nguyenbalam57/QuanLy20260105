using ManagementFile.Models.BaseModels;
using ManagementFile.Contracts.Enums;
using ManagementFile.Models.UserManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ManagementFile.Models.ProjectManagement;

namespace ManagementFile.Models.NotificationsAndCommunications
{
    /// <summary>
    /// Notification - Thông báo hệ thống
    /// Hệ thống thông báo cho users về các events
    /// </summary>
    [Table("Notifications")]
    [Index(nameof(UserId), nameof(IsRead), nameof(CreatedAt))]
    public class Notification : SoftDeletableEntity
    {
        /// <summary>
        /// UserId - ID người nhận thông báo
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Title - Tiêu đề thông báo
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        /// <summary>
        /// Content - Nội dung thông báo
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; } = "";

        /// <summary>
        /// Type - Loại thông báo
        /// </summary>
        public NotificationType Type { get; set; } = NotificationType.Info;

        /// <summary>
        /// IsRead - Đã đọc chưa
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// ReadAt - Thời gian đọc
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// RelatedEntityType - Loại entity liên quan
        /// Project, Task, ProjectFile, User
        /// </summary>
        [StringLength(100)]
        public string RelatedEntityType { get; set; } = "";

        /// <summary>
        /// RelatedEntityId - ID entity liên quan
        /// </summary>
        public int? RelatedEntityId { get; set; }

        /// <summary>
        /// ActionUrl - URL để navigate khi click notification
        /// </summary>
        [StringLength(500)]
        public string ActionUrl { get; set; } = "";

        /// <summary>
        /// ExpiresAt - Thông báo hết hạn khi nào
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        /// <summary>
        /// MarkAsRead - Đánh dấu đã đọc
        /// </summary>
        public virtual void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }

        /// <summary>
        /// IsExpired - Kiểm tra thông báo đã hết hạn chưa
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    }
}
