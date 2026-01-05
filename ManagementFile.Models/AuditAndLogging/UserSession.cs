using ManagementFile.Models.BaseModels;
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

namespace ManagementFile.Models.AuditAndLogging
{
    /// <summary>
    /// UserSession - Phiên làm việc người dùng
    /// Tracking user sessions cho security và monitoring
    /// </summary>
    [Table("UserSessions")]
    [Index(nameof(UserId), nameof(IsActive))]
    [Index(nameof(SessionToken), IsUnique = true)]
    public class UserSession : BaseEntity
    {
        /// <summary>
        /// UserId - ID người dùng
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// SessionToken - Token session duy nhất
        /// </summary>
        [Required]
        [StringLength(255)]
        public string SessionToken { get; set; } = "";

        /// <summary>
        /// LoginAt - Thời gian đăng nhập
        /// </summary>
        public DateTime LoginAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// LogoutAt - Thời gian đăng xuất
        /// </summary>
        public DateTime? LogoutAt { get; set; }

        /// <summary>
        /// ExpiresAt - Session hết hạn khi nào
        /// </summary>
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(8);

        /// <summary>
        /// IsActive - Session có đang hoạt động không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// DeactivatedAt - Thời gian session bị deactivate
        /// </summary>
        public DateTime? DeactivatedAt { get; set; }

        /// <summary>
        /// DeactivationReason - Lý do session bị deactivate
        /// Các giá trị phổ biến: "Expired", "Logout", "ForceLogout", "SecurityViolation", "Inactive"
        /// </summary>
        [StringLength(100)]
        public string DeactivationReason { get; set; } = "";

        /// <summary>
        /// IPAddress - IP đăng nhập
        /// </summary>
        [StringLength(45)]
        public string IPAddress { get; set; } = "";

        /// <summary>
        /// UserAgent - Thông tin browser
        /// </summary>
        [StringLength(500)]
        public string UserAgent { get; set; } = "";

        /// <summary>
        /// DeviceInfo - Thông tin thiết bị
        /// </summary>
        [StringLength(500)]
        public string DeviceInfo { get; set; } = "";

        /// <summary>
        /// LastActivityAt - Hoạt động cuối cùng
        /// </summary>
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ActivityCount - Số thao tác trong session
        /// </summary>
        public int ActivityCount { get; set; } = 0;

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        /// <summary>
        /// IsExpired - Session đã hết hạn chưa
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

        /// <summary>
        /// IsDeactivated - Session đã bị deactivate chưa
        /// </summary>
        [NotMapped]
        public bool IsDeactivated => !IsActive || DeactivatedAt.HasValue;

        /// <summary>
        /// SessionDuration - Thời gian session tồn tại
        /// </summary>
        [NotMapped]
        public TimeSpan SessionDuration => (DeactivatedAt ?? DateTime.UtcNow) - LoginAt;

        /// <summary>
        /// EndSession - Kết thúc session với lý do
        /// </summary>
        public virtual void EndSession(int userId, string reason = "Logout")
        {
            LogoutAt = DateTime.UtcNow;
            DeactivatedAt = DateTime.UtcNow;
            DeactivationReason = reason;
            IsActive = false;
            MarkAsUpdated(userId);
        }

        /// <summary>
        /// ForceDeactivate - Bắt buộc deactivate session (admin action)
        /// </summary>
        public virtual void ForceDeactivate(int userId, string reason = "ForceLogout")
        {
            DeactivatedAt = DateTime.UtcNow;
            DeactivationReason = reason;
            IsActive = false;
            MarkAsUpdated(userId);
        }

        /// <summary>
        /// MarkAsExpired - Đánh dấu session đã hết hạn
        /// </summary>
        public virtual void MarkAsExpired()
        {
            DeactivatedAt = DateTime.UtcNow;
            DeactivationReason = "Expired";
            IsActive = false;
            MarkAsUpdated();
        }

        /// <summary>
        /// RecordActivity - Ghi nhận hoạt động
        /// </summary>
        public virtual void RecordActivity(int userId)
        {
            // Chỉ record activity nếu session vẫn active
            if (!IsActive || IsExpired)
                return;

            LastActivityAt = DateTime.UtcNow;
            ActivityCount++;

            // Gia hạn session thêm 2 giờ nếu gần hết hạn (trong 30 phút cuối)
            if (ExpiresAt <= DateTime.UtcNow.AddMinutes(30))
            {
                ExpiresAt = DateTime.UtcNow.AddHours(2);
            }

            MarkAsUpdated(userId);
        }

        /// <summary>
        /// ExtendSession - Gia hạn session thêm thời gian
        /// </summary>
        public virtual void ExtendSession(int userId, TimeSpan additionalTime)
        {
            if (IsActive && !IsExpired)
            {
                ExpiresAt = ExpiresAt.Add(additionalTime);
                MarkAsUpdated(userId);
            }
        }

    }
}
