using ManagementFile.Models.AuditAndLogging;
using ManagementFile.Models.BaseModels;
using ManagementFile.Models.NotificationsAndCommunications;
using ManagementFile.Contracts.Enums;
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
    /// User - Người dùng hệ thống
    /// Thông tin user với role-based access control
    /// </summary>
    [Table("Users")]
    [Index(nameof(Username), IsUnique = true)]
    public class User : MetadataEntity
    {
        /// <summary>
        /// Username - Tên đăng nhập duy nhất
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = "";

        /// <summary>
        /// Email - Địa chỉ email duy nhất
        /// </summary>
        [StringLength(256)]
        public string Email { get; set; }

        /// <summary>
        /// FullName - Họ và tên đầy đủ
        /// </summary>
        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = "";

        /// <summary>
        /// PasswordHash - Hash mật khẩu
        /// Sử dụng BCrypt hoặc tương đương
        /// </summary>
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = "";

        /// <summary>
        /// Salt - Salt cho password hash
        /// </summary>
        [StringLength(255)]
        public string Salt { get; set; } = "";

        /// <summary>
        /// Role - Vai trò trong hệ thống
        /// </summary>
        public UserRole Role { get; set; } = UserRole.Staff;

        /// <summary>
        /// Department - Phòng ban làm việc
        /// </summary>
        public Department Department { get; set; } = Department.OTHER;

        /// <summary>
        /// PhoneNumber - Số điện thoại
        /// </summary>
        [StringLength(20)]
        public string PhoneNumber { get; set; } = "";

        /// <summary>
        /// Position - Chức vụ cụ thể
        /// </summary>
        [StringLength(100)]
        public string Position { get; set; } = "";

        /// <summary>
        /// ManagerId - ID của quản lý trực tiếp
        /// </summary>
        public int ManagerId { get; set; }

        /// <summary>
        /// Avatar - Đường dẫn ảnh đại diện
        /// </summary>
        [StringLength(500)]
        public string Avatar { get; set; } = "";

        /// <summary>
        /// Language - Ngôn ngữ ưa thích
        /// </summary>
        [StringLength(10)]
        public string Language { get; set; } = "vi-VN";

        /// <summary>
        /// LastLoginAt - Lần đăng nhập cuối
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// LastLoginIP - IP đăng nhập cuối
        /// </summary>
        [StringLength(45)]
        public string LastLoginIP { get; set; } = "";

        /// <summary>
        /// LoginFailureCount - Số lần đăng nhập thất bại liên tiếp
        /// </summary>
        public int LoginFailureCount { get; set; } = 0;

        /// <summary>
        /// LockedUntil - Khóa tài khoản đến khi nào
        /// </summary>
        public DateTime? LockedUntil { get; set; }

        /// <summary>
        /// Navigation Properties
        /// </summary>
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

        public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        /// <summary>
        /// IsAccountLocked - Kiểm tra tài khoản có bị khóa không
        /// </summary>
        [NotMapped]
        public bool IsAccountLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

        /// <summary>
        /// HasRole - Kiểm tra user có role tối thiểu không
        /// </summary>
        public virtual bool HasRole(UserRole requiredRole)
        {
            return (int)Role >= (int)requiredRole;
        }

        /// <summary>
        /// GetDisplayName - Lấy tên hiển thị
        /// </summary>
        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Username;

        /// <summary>
        /// RecordLoginSuccess - Ghi nhận đăng nhập thành công
        /// </summary>
        public virtual void RecordLoginSuccess( string ipAddress)
        {
            LastLoginAt = DateTime.UtcNow;
            LastLoginIP = ipAddress;
            LoginFailureCount = 0;
            LockedUntil = null;
            MarkAsUpdated();
        }

        /// <summary>
        /// RecordLoginFailure - Ghi nhận đăng nhập thất bại
        /// </summary>
        public virtual void RecordLoginFailure()
        {
            LoginFailureCount++;

            // Khóa tài khoản sau 5 lần thất bại liên tiếp
            if (LoginFailureCount >= 5)
            {
                LockedUntil = DateTime.UtcNow.AddMinutes(30); // Khóa 30 phút
            }

            MarkAsUpdated();
        }

        /// <summary>
        /// UnlockAccount - Mở khóa tài khoản thủ công
        /// </summary>
        public virtual void UnlockAccount(int unlockedBy)
        {
            LockedUntil = null;
            LoginFailureCount = 0;
            MarkAsUpdated( unlockedBy);
        }
    }
}
