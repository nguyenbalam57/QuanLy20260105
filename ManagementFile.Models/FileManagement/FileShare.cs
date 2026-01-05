using ManagementFile.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ManagementFile.Models.FileManagement
{
    /// <summary>
    /// FileShare - Chia sẻ file
    /// Quản lý việc chia sẻ file với external users hoặc tạo public links
    /// </summary>
    [Table("FileShares")]
    [Index(nameof(ProjectFileId), nameof(IsActive))]
    [Index(nameof(ShareToken), IsUnique = true)]
    [Index(nameof(SharedWithEmail), nameof(IsActive))]
    public class FileShare : SoftDeletableEntity
    {
        /// <summary>
        /// ProjectFileId - ID file được chia sẻ
        /// </summary>
        [Required]
        public int ProjectFileId { get; set; } = -1;

        /// <summary>
        /// ShareToken - Token duy nhất để truy cập file
        /// </summary>
        [Required]
        [StringLength(128)]
        public string ShareToken { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// ShareType - Loại chia sẻ
        /// Public, Email, Internal, External
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ShareType { get; set; } = "Public";

        /// <summary>
        /// SharedWithUserId - ID user được chia sẻ (nếu internal)
        /// </summary>
        public int SharedWithUserId { get; set; } = -1;

        /// <summary>
        /// SharedWithEmail - Email được chia sẻ (nếu external)
        /// </summary>
        [StringLength(256)]
        public string SharedWithEmail { get; set; } = "";

        /// <summary>
        /// SharedWithName - Tên người được chia sẻ
        /// </summary>
        [StringLength(200)]
        public string SharedWithName { get; set; } = "";

        /// <summary>
        /// ShareTitle - Tiêu đề chia sẻ
        /// </summary>
        [StringLength(200)]
        public string ShareTitle { get; set; } = "";

        /// <summary>
        /// ShareMessage - Tin nhắn kèm theo
        /// </summary>
        [StringLength(1000)]
        public string ShareMessage { get; set; } = "";

        /// <summary>
        /// IsActive - Share có đang active không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// RequirePassword - Có yêu cầu password không
        /// </summary>
        public bool RequirePassword { get; set; } = false;

        /// <summary>
        /// PasswordHash - Hash của password (nếu có)
        /// </summary>
        [StringLength(256)]
        public string PasswordHash { get; set; } = "";

        /// <summary>
        /// AllowDownload - Có cho phép download không
        /// </summary>
        public bool AllowDownload { get; set; } = true;

        /// <summary>
        /// AllowPreview - Có cho phép preview không
        /// </summary>
        public bool AllowPreview { get; set; } = true;

        /// <summary>
        /// AllowComment - Có cho phép comment không
        /// </summary>
        public bool AllowComment { get; set; } = false;

        /// <summary>
        /// AllowPrint - Có cho phép print không
        /// </summary>
        public bool AllowPrint { get; set; } = true;

        /// <summary>
        /// TrackAccess - Có track truy cập không
        /// </summary>
        public bool TrackAccess { get; set; } = true;

        /// <summary>
        /// MaxDownloads - Số lần download tối đa (0 = unlimited)
        /// </summary>
        public int MaxDownloads { get; set; } = 0;

        /// <summary>
        /// CurrentDownloads - Số lần đã download
        /// </summary>
        public int CurrentDownloads { get; set; } = 0;

        /// <summary>
        /// MaxViews - Số lần view tối đa (0 = unlimited)
        /// </summary>
        public int MaxViews { get; set; } = 0;

        /// <summary>
        /// CurrentViews - Số lần đã view
        /// </summary>
        public int CurrentViews { get; set; } = 0;

        /// <summary>
        /// ExpiresAt - Thời gian hết hạn share (null = không hết hạn)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// LastAccessedAt - Lần truy cập cuối
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// LastAccessedIP - IP truy cập cuối
        /// </summary>
        [StringLength(45)]
        public string LastAccessedIP { get; set; } = "";

        /// <summary>
        /// LastAccessedUserAgent - User agent truy cập cuối
        /// </summary>
        [StringLength(500)]
        public string LastAccessedUserAgent { get; set; } = "";

        /// <summary>
        /// NotifyOnAccess - Có thông báo khi có truy cập không
        /// </summary>
        public bool NotifyOnAccess { get; set; } = false;

        /// <summary>
        /// NotifyOnDownload - Có thông báo khi có download không
        /// </summary>
        public bool NotifyOnDownload { get; set; } = false;

        /// <summary>
        /// ShareUrl - URL chia sẻ đầy đủ
        /// </summary>
        [StringLength(1000)]
        public string ShareUrl { get; set; } = "";

        /// <summary>
        /// QRCodeData - Dữ liệu QR code (base64)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string QRCodeData { get; set; } = "";

        /// <summary>
        /// CustomProperties - Các thuộc tính tùy chỉnh (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string CustomProperties { get; set; } = "";

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(ProjectFileId))]
        public virtual ProjectFile ProjectFile { get; set; }

        [JsonIgnore]
        public virtual ICollection<FileShareAccess> ShareAccesses { get; set; } = new List<FileShareAccess>();

        /// <summary>
        /// Computed Properties
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

        [NotMapped]
        public bool IsDownloadLimitReached => MaxDownloads > 0 && CurrentDownloads >= MaxDownloads;

        [NotMapped]
        public bool IsViewLimitReached => MaxViews > 0 && CurrentViews >= MaxViews;

        [NotMapped]
        public bool IsAccessible => IsActive && !IsDeleted && !IsExpired && !IsDownloadLimitReached;

        [NotMapped]
        public int RemainingDownloads => MaxDownloads > 0 ? Math.Max(0, MaxDownloads - CurrentDownloads) : int.MaxValue;

        [NotMapped]
        public int RemainingViews => MaxViews > 0 ? Math.Max(0, MaxViews - CurrentViews) : int.MaxValue;

        [NotMapped]
        public TimeSpan? TimeUntilExpiry => ExpiresAt.HasValue ? ExpiresAt.Value - DateTime.UtcNow : null;

        /// <summary>
        /// Business Methods
        /// </summary>

        /// <summary>
        /// GenerateNewToken - Tạo token mới
        /// </summary>
        public virtual void GenerateNewToken()
        {
            ShareToken = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// SetPassword - Set password cho share
        /// </summary>
        public virtual void SetPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                RequirePassword = false;
                PasswordHash = "";
            }
            else
            {
                RequirePassword = true;
                // Hash password - using simple hash for now, should use proper BCrypt in production
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        /// <summary>
        /// VerifyPassword - Verify password
        /// </summary>
        public virtual bool VerifyPassword(string password)
        {
            if (!RequirePassword || string.IsNullOrEmpty(PasswordHash))
                return true;

            if (string.IsNullOrEmpty(password))
                return false;

            // Simple verification - should use proper BCrypt in production
            var hashedInput = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            return hashedInput == PasswordHash;
        }

        /// <summary>
        /// RecordAccess - Ghi nhận truy cập
        /// </summary>
        public virtual FileShareAccess RecordAccess(string accessType, string ipAddress = "", string userAgent = "", int accessedBy = -1)
        {
            LastAccessedAt = DateTime.UtcNow;
            LastAccessedIP = ipAddress;
            LastAccessedUserAgent = userAgent;

            if (accessType.ToLower() == "download")
            {
                CurrentDownloads++;
            }
            else if (accessType.ToLower() == "view")
            {
                CurrentViews++;
            }

            var shareAccess = new FileShareAccess
            {
                FileShareId = Id,
                AccessType = accessType,
                AccessedAt = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                AccessedBy = accessedBy,
                CreatedBy = accessedBy
            };

            ShareAccesses.Add(shareAccess);
            return shareAccess;
        }
    }

    /// <summary>
    /// FileShareAccess - Log truy cập file share
    /// Ghi lại chi tiết các lần truy cập file share
    /// </summary>
    [Table("FileShareAccesses")]
    [Index(nameof(FileShareId), nameof(AccessedAt))]
    public class FileShareAccess : BaseEntity
    {
        /// <summary>
        /// FileShareId - ID file share
        /// </summary>
        [Required]
        [StringLength(450)]
        public int FileShareId { get; set; } 

        /// <summary>
        /// AccessType - Loại truy cập (View, Download, Preview)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string AccessType { get; set; } = "";

        /// <summary>
        /// AccessedAt - Thời gian truy cập
        /// </summary>
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IPAddress - IP truy cập
        /// </summary>
        [StringLength(45)]
        public string IPAddress { get; set; } = "";

        /// <summary>
        /// UserAgent - User agent
        /// </summary>
        [StringLength(500)]
        public string UserAgent { get; set; } = "";

        /// <summary>
        /// AccessedBy - ID user truy cập (nếu có)
        /// </summary>
        public int AccessedBy { get; set; } 

        /// <summary>
        /// SessionId - ID session
        /// </summary>
        [StringLength(450)]
        public string SessionId { get; set; } = "";

        /// <summary>
        /// IsSuccessful - Truy cập có thành công không
        /// </summary>
        public bool IsSuccessful { get; set; } = true;

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(FileShareId))]
        public virtual FileShare FileShare { get; set; }
    }
}