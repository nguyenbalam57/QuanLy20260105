using ManagementFile.Models.BaseModels;
using ManagementFile.Models.ProjectManagement;
using ManagementFile.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ManagementFile.Models.FileManagement
{
    /// <summary>
    /// ProjectFile - File trong dự án
    /// Quản lý thông tin file thuộc project với version control và metadata
    /// </summary>
    [Table("ProjectFiles")]
    [Index(nameof(ProjectId), nameof(FileName))]
    [Index(nameof(FolderId), nameof(IsActive))]
    [Index(nameof(FileType), nameof(IsActive))]
    public class ProjectFile : SoftDeletableEntity, IHasMetadata, IHasTags
    {
        /// <summary>
        /// ProjectId - ID dự án chứa file này
        /// </summary>
        [Required]
        public int ProjectId { get; set; } = -1;

        /// <summary>
        /// FolderId - ID thư mục chứa file (nếu có)
        /// </summary>
        public int FolderId { get; set; } = -1;

        /// <summary>
        /// FileName - Tên file gốc
        /// </summary>
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = "";

        /// <summary>
        /// DisplayName - Tên hiển thị của file
        /// </summary>
        [StringLength(255)]
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// FileExtension - Phần mở rộng file (.docx, .pdf, .jpg...)
        /// </summary>
        [StringLength(10)]
        public string FileExtension { get; set; } = "";

        /// <summary>
        /// FileType - Loại file (Document, Image, Code, Executable...)
        /// </summary>
        [StringLength(50)]
        public string FileType { get; set; } = "";

        /// <summary>
        /// MimeType - MIME type của file
        /// </summary>
        [StringLength(100)]
        public string MimeType { get; set; } = "";

        /// <summary>
        /// CurrentFileSize - Kích thước file hiện tại (bytes)
        /// </summary>
        public long CurrentFileSize { get; set; } = 0;

        /// <summary>
        /// CurrentFileHash - Hash của version hiện tại
        /// </summary>
        [StringLength(128)]
        public string CurrentFileHash { get; set; } = "";

        /// <summary>
        /// StoragePath - Đường dẫn lưu trữ vật lý
        /// </summary>
        [StringLength(1000)]
        public string StoragePath { get; set; } = "";

        /// <summary>
        /// RelativePath - Đường dẫn tương đối trong project
        /// </summary>
        [StringLength(1000)]
        public string RelativePath { get; set; } = "";

        /// <summary>
        /// Description - Mô tả file
        /// </summary>
        [StringLength(2000)]
        public string Description { get; set; } = "";

        /// <summary>
        /// IsActive - File có đang active không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// IsPublic - File có được public không
        /// </summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// IsReadOnly - File có được bảo vệ ghi không
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// RequireApproval - File có cần phê duyệt khi thay đổi không
        /// </summary>
        public bool RequireApproval { get; set; } = false;

        /// <summary>
        /// ApprovalStatus - Trạng thái phê duyệt
        /// Pending, Approved, Rejected
        /// </summary>
        [StringLength(20)]
        public string ApprovalStatus { get; set; } = "Approved";

        /// <summary>
        /// ApprovedBy - ID người phê duyệt
        /// </summary>
        public int ApprovedBy { get; set; } = -1;

        /// <summary>
        /// ApprovedAt - Thời gian phê duyệt
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// LastAccessedAt - Lần truy cập cuối cùng
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// LastAccessedBy - ID người truy cập cuối cùng
        /// </summary>
        public int LastAccessedBy { get; set; }

        /// <summary>
        /// DownloadCount - Số lần download
        /// </summary>
        public int DownloadCount { get; set; } = 0;

        /// <summary>
        /// ViewCount - Số lần view/xem
        /// </summary>
        public int ViewCount { get; set; } = 0;

        /// <summary>
        /// ShareCount - Số lần share
        /// </summary>
        public int ShareCount { get; set; } = 0;

        /// <summary>
        /// CheckoutBy - ID người đang checkout file (lock for editing)
        /// </summary>
        public int CheckoutBy { get; set; } 

        /// <summary>
        /// CheckoutAt - Thời gian checkout
        /// </summary>
        public DateTime? CheckoutAt { get; set; }

        /// <summary>
        /// ExpectedCheckinAt - Thời gian dự kiến checkin
        /// </summary>
        public DateTime? ExpectedCheckinAt { get; set; }

        /// <summary>
        /// AutoCheckinHours - Số giờ tự động checkin (0 = không tự động)
        /// </summary>
        public int AutoCheckinHours { get; set; } = 0;

        /// <summary>
        /// Metadata - Thông tin metadata bổ sung (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Metadata { get; set; } = "{}";

        /// <summary>
        /// Tags - Các tags của file (JSON array)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Tags { get; set; } = "";

        /// <summary>
        /// CustomProperties - Các thuộc tính tùy chỉnh (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string CustomProperties { get; set; } = "";

        /// <summary>
        /// ThumbnailPath - Đường dẫn thumbnail (cho image/video)
        /// </summary>
        [StringLength(1000)]
        public string ThumbnailPath { get; set; } = "";

        /// <summary>
        /// PreviewPath - Đường dẫn file preview
        /// </summary>
        [StringLength(1000)]
        public string PreviewPath { get; set; } = "";

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(ProjectId))]
        public virtual Project Project { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(FolderId))]
        public virtual ProjectFolder Folder { get; set; }

        [JsonIgnore]
        public virtual ICollection<FileVersion> FileVersions { get; set; } = new List<FileVersion>();

        [JsonIgnore]
        public virtual ICollection<FilePermission> FilePermissions { get; set; } = new List<FilePermission>();

        [JsonIgnore]
        public virtual ICollection<FileShare> FileShares { get; set; } = new List<FileShare>();

        #region IHasMetadata Implementation

        /// <summary>
        /// GetMetadata - Deserialize metadata thành object
        /// </summary>
        public T GetMetadata<T>() where T : class, new()
        {
            if (string.IsNullOrEmpty(Metadata))
                return new T();

            try
            {
                return JsonSerializer.Deserialize<T>(Metadata) ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>
        /// SetMetadata - Serialize object thành metadata JSON
        /// </summary>
        public void SetMetadata<T>(T data) where T : class
        {
            if (data == null)
            {
                Metadata = "";
                return;
            }

            try
            {
                Metadata = JsonSerializer.Serialize(data);
            }
            catch
            {
                Metadata = "";
            }
        }

        #endregion

        #region IHasTags Implementation

        /// <summary>
        /// GetTags - Lấy danh sách tags
        /// </summary>
        public List<string> GetTags()
        {
            if (string.IsNullOrEmpty(Tags))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// SetTags - Set danh sách tags
        /// </summary>
        public void SetTags(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                Tags = "";
                return;
            }

            try
            {
                // Remove duplicates and empty tags
                var cleanTags = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                                   .Select(t => t.Trim())
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .ToList();
                
                Tags = JsonSerializer.Serialize(cleanTags);
            }
            catch
            {
                Tags = "";
            }
        }

        /// <summary>
        /// AddTag - Thêm tag mới
        /// </summary>
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            var currentTags = GetTags();
            var cleanTag = tag.Trim();

            if (!currentTags.Contains(cleanTag, StringComparer.OrdinalIgnoreCase))
            {
                currentTags.Add(cleanTag);
                SetTags(currentTags);
            }
        }

        /// <summary>
        /// RemoveTag - Xóa tag
        /// </summary>
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            var currentTags = GetTags();
            var cleanTag = tag.Trim();

            if (currentTags.RemoveAll(t => string.Equals(t, cleanTag, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                SetTags(currentTags);
            }
        }

        /// <summary>
        /// HasTag - Kiểm tra có tag không
        /// </summary>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            var currentTags = GetTags();
            var cleanTag = tag.Trim();

            return currentTags.Any(t => string.Equals(t, cleanTag, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        /// <summary>
        /// Business Methods
        /// </summary>

        /// <summary>
        /// Checkout - Khóa file để chỉnh sửa
        /// </summary>
        /// <param name="userId">ID người checkout</param>
        /// <param name="expectedCheckinHours">Số giờ dự kiến checkin</param>
        public virtual void Checkout(int userId, int expectedCheckinHours = 24)
        {
            if (IsCheckedOut())
                throw new InvalidOperationException("File đã được checkout bởi người khác");

            CheckoutBy = userId;
            CheckoutAt = DateTime.UtcNow;
            ExpectedCheckinAt = DateTime.UtcNow.AddHours(expectedCheckinHours);
            MarkAsUpdated(userId);
        }

        /// <summary>
        /// Checkin - Mở khóa file sau khi chỉnh sửa
        /// </summary>
        /// <param name="userId">ID người checkin</param>
        public virtual void Checkin(int userId)
        {
            if (userId <= 0)
                throw new InvalidOperationException("File không được checkout bởi user này");

            CheckoutBy = -1;
            CheckoutAt = null;
            ExpectedCheckinAt = null;
            MarkAsUpdated(userId);
        }

        /// <summary>
        /// ForceCheckin - Ép buộc checkin (admin only)
        /// </summary>
        /// <param name="adminUserId">ID admin thực hiện</param>
        public virtual void ForceCheckin(int adminUserId)
        {
            CheckoutBy = -1;
            CheckoutAt = null;
            ExpectedCheckinAt = null;
            MarkAsUpdated(adminUserId);
        }

        /// <summary>
        /// IsCheckedOut - Kiểm tra file có đang được checkout không
        /// </summary>
        public virtual bool IsCheckedOut()
        {
            return CheckoutBy >= 0 && CheckoutAt.HasValue;
        }

        /// <summary>
        /// IsCheckedOutBy - Kiểm tra file có được checkout bởi user cụ thể không
        /// </summary>
        public virtual bool IsCheckedOutBy(int userId)
        {
            return IsCheckedOut() && CheckoutBy == userId;
        }

        /// <summary>
        /// IsOverdueCheckout - Kiểm tra checkout có quá hạn không
        /// </summary>
        public virtual bool IsOverdueCheckout()
        {
            return IsCheckedOut() && ExpectedCheckinAt.HasValue && DateTime.UtcNow > ExpectedCheckinAt.Value;
        }

        /// <summary>
        /// MarkAccessed - Đánh dấu file được truy cập
        /// </summary>
        /// <param name="userId">ID người truy cập</param>
        public virtual void MarkAccessed(int userId)
        {
            LastAccessedAt = DateTime.UtcNow;
            LastAccessedBy = userId;
            ViewCount++;
        }

        /// <summary>
        /// MarkDownloaded - Đánh dấu file được download
        /// </summary>
        /// <param name="userId">ID người download</param>
        public virtual void MarkDownloaded(int userId)
        {
            DownloadCount++;
            MarkAccessed(userId);
        }

        /// <summary>
        /// MarkShared - Đánh dấu file được share
        /// </summary>
        public virtual void MarkShared()
        {
            ShareCount++;
        }

        /// <summary>
        /// GetCurrentVersion - Lấy version hiện tại của file
        /// </summary>
        public virtual FileVersion GetCurrentVersion()
        {
            return FileVersions?.FirstOrDefault(v => v.IsCurrentVersion);
        }

        /// <summary>
        /// CreateNewVersion - Tạo version mới cho file
        /// </summary>
        /// <param name="versionNumber">Số version</param>
        /// <param name="userId">ID người tạo</param>
        /// <param name="changeType">Loại thay đổi</param>
        /// <param name="physicalPath">Đường dẫn file mới</param>
        /// <param name="fileSize">Kích thước file mới</param>
        /// <param name="fileHash">Hash file mới</param>
        /// <param name="notes">Ghi chú</param>
        public virtual FileVersion CreateNewVersion(
            string versionNumber,
            int userId,
            FileChangeType changeType,
            string physicalPath,
            long fileSize,
            string fileHash,
            string notes = "")
        {
            // Đánh dấu version cũ không còn current
            var currentVersion = GetCurrentVersion();
            if (currentVersion != null)
            {
                currentVersion.IsCurrentVersion = false;
            }

            // Tạo version mới
            var newVersion = new FileVersion
            {
                ProjectFileId = Id,
                VersionNumber = versionNumber,
                ChangeType = changeType,
                PhysicalPath = physicalPath,
                FileSize = fileSize,
                FileHash = fileHash,
                VersionNotes = notes,
                IsCurrentVersion = true,
                CreatedBy = userId,
                MasterEntityId = Id
            };

            FileVersions.Add(newVersion);

            // Cập nhật thông tin file hiện tại
            CurrentFileSize = fileSize;
            CurrentFileHash = fileHash;
            MarkAsUpdated(userId);

            return newVersion;
        }

        /// <summary>
        /// GetFullPath - Lấy đường dẫn đầy đủ của file
        /// </summary>
        public virtual string GetFullPath()
        {
            if (!string.IsNullOrEmpty(RelativePath))
                return Path.Combine(RelativePath, FileName);
            return FileName;
        }

        /// <summary>
        /// GetFileExtensionFromName - Tự động set extension từ filename
        /// </summary>
        public virtual void UpdateFileExtension()
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                FileExtension = Path.GetExtension(FileName).ToLowerInvariant();
            }
        }
    }
}