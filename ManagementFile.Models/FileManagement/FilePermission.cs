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
    /// FilePermission - Quyền truy cập file
    /// Quản lý permissions cho từng user/role trên file cụ thể
    /// </summary>
    [Table("FilePermissions")]
    [Index(nameof(ProjectFileId), nameof(UserId), nameof(IsActive), IsUnique = true)]
    [Index(nameof(ProjectFileId), nameof(RoleName), nameof(IsActive))]
    public class FilePermission : SoftDeletableEntity
    {
        /// <summary>
        /// ProjectFileId - ID file được phân quyền
        /// </summary>
        [Required]
        public int ProjectFileId { get; set; } = -1;

        /// <summary>
        /// UserId - ID user được phân quyền (null nếu phân quyền theo role)
        /// </summary>
        public int UserId { get; set; } = -1;

        /// <summary>
        /// RoleName - Tên role được phân quyền (null nếu phân quyền theo user)
        /// </summary>
        [StringLength(50)]
        public string RoleName { get; set; } = "";

        /// <summary>
        /// PermissionType - Loại quyền
        /// Individual, Role, Group, Public
        /// </summary>
        [Required]
        [StringLength(20)]
        public string PermissionType { get; set; } = "Individual";

        /// <summary>
        /// CanRead - Có quyền đọc không
        /// </summary>
        public bool CanRead { get; set; } = true;

        /// <summary>
        /// CanWrite - Có quyền ghi không
        /// </summary>
        public bool CanWrite { get; set; } = false;

        /// <summary>
        /// CanDelete - Có quyền xóa không
        /// </summary>
        public bool CanDelete { get; set; } = false;

        /// <summary>
        /// CanShare - Có quyền chia sẻ không
        /// </summary>
        public bool CanShare { get; set; } = false;

        /// <summary>
        /// CanManagePermissions - Có quyền quản lý permissions không
        /// </summary>
        public bool CanManagePermissions { get; set; } = false;

        /// <summary>
        /// CanDownload - Có quyền download không
        /// </summary>
        public bool CanDownload { get; set; } = true;

        /// <summary>
        /// CanPrint - Có quyền in không
        /// </summary>
        public bool CanPrint { get; set; } = true;

        /// <summary>
        /// CanComment - Có quyền comment không
        /// </summary>
        public bool CanComment { get; set; } = false;

        /// <summary>
        /// CanCheckout - Có quyền checkout không
        /// </summary>
        public bool CanCheckout { get; set; } = false;

        /// <summary>
        /// CanApprove - Có quyền approve không
        /// </summary>
        public bool CanApprove { get; set; } = false;

        /// <summary>
        /// IsActive - Permission có đang active không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ExpiresAt - Thời gian hết hạn permission (null = không hết hạn)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// GrantedBy - ID người cấp quyền
        /// </summary>
        public int GrantedBy { get; set; } = -1;

        /// <summary>
        /// GrantedAt - Thời gian cấp quyền
        /// </summary>
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// RevokedBy - ID người thu hồi quyền
        /// </summary>
        public int RevokedBy { get; set; } 

        /// <summary>
        /// RevokedAt - Thời gian thu hồi quyền
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Reason - Lý do cấp/thu hồi quyền
        /// </summary>
        [StringLength(1000)]
        public string Reason { get; set; } = "";

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(ProjectFileId))]
        public virtual ProjectFile ProjectFile { get; set; }

        /// <summary>
        /// Computed Properties
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

        [NotMapped]
        public bool IsRevoked => RevokedAt.HasValue;

        [NotMapped]
        public bool IsEffective => IsActive && !IsDeleted && !IsExpired && !IsRevoked;

        [NotMapped]
        public string DisplayName
        {
            get
            {
                if (UserId >= 0)
                    return $"User: {UserId}";
                if (!string.IsNullOrEmpty(RoleName))
                    return $"Role: {RoleName}";
                return $"Type: {PermissionType}";
            }
        }

        /// <summary>
        /// Business Methods
        /// </summary>

        /// <summary>
        /// RevokePermission - Thu hồi quyền
        /// </summary>
        public virtual void RevokePermission(int revokedBy, string reason = "")
        {
            if (IsRevoked)
                throw new InvalidOperationException("Permission đã được revoke rồi");

            RevokedBy = revokedBy;
            RevokedAt = DateTime.UtcNow;
            Reason = reason;
            IsActive = false;
            MarkAsUpdated(revokedBy);
        }

        /// <summary>
        /// RestorePermission - Khôi phục quyền đã thu hồi
        /// </summary>
        public virtual void RestorePermission(int restoredBy, string reason = "")
        {
            if (!IsRevoked)
                throw new InvalidOperationException("Permission chưa được revoke");

            RevokedBy = -1;
            RevokedAt = null;
            Reason = reason;
            IsActive = true;
            MarkAsUpdated(restoredBy);
        }

        /// <summary>
        /// ExtendExpiry - Gia hạn permission
        /// </summary>
        public virtual void ExtendExpiry(DateTime newExpiryDate, int extendedBy)
        {
            if (newExpiryDate <= DateTime.UtcNow)
                throw new ArgumentException("Ngày hết hạn phải lớn hơn hiện tại");

            ExpiresAt = newExpiryDate;
            MarkAsUpdated(extendedBy);
        }

        /// <summary>
        /// RemoveExpiry - Bỏ hạn sử dụng
        /// </summary>
        public virtual void RemoveExpiry(int updatedBy)
        {
            ExpiresAt = null;
            MarkAsUpdated(updatedBy);
        }

        /// <summary>
        /// UpdatePermissions - Cập nhật các quyền
        /// </summary>
        public virtual void UpdatePermissions(
            bool canRead = true,
            bool canWrite = false,
            bool canDelete = false,
            bool canShare = false,
            bool canManagePermissions = false,
            bool canDownload = true,
            bool canPrint = true,
            bool canComment = false,
            bool canCheckout = false,
            bool canApprove = false,
            int updatedBy = -1)
        {
            CanRead = canRead;
            CanWrite = canWrite;
            CanDelete = canDelete;
            CanShare = canShare;
            CanManagePermissions = canManagePermissions;
            CanDownload = canDownload;
            CanPrint = canPrint;
            CanComment = canComment;
            CanCheckout = canCheckout;
            CanApprove = canApprove;

            MarkAsUpdated(updatedBy);
        }

        /// <summary>
        /// SetReadOnlyPermissions - Set quyền chỉ đọc
        /// </summary>
        public virtual void SetReadOnlyPermissions(int updatedBy)
        {
            UpdatePermissions(
                canRead: true,
                canWrite: false,
                canDelete: false,
                canShare: false,
                canManagePermissions: false,
                canDownload: true,
                canPrint: true,
                canComment: false,
                canCheckout: false,
                canApprove: false,
                updatedBy: updatedBy);
        }

        /// <summary>
        /// SetFullPermissions - Set quyền đầy đủ
        /// </summary>
        public virtual void SetFullPermissions(int updatedBy)
        {
            UpdatePermissions(
                canRead: true,
                canWrite: true,
                canDelete: true,
                canShare: true,
                canManagePermissions: true,
                canDownload: true,
                canPrint: true,
                canComment: true,
                canCheckout: true,
                canApprove: true,
                updatedBy: updatedBy);
        }

        /// <summary>
        /// SetEditorPermissions - Set quyền editor (đọc, ghi, comment)
        /// </summary>
        public virtual void SetEditorPermissions(int updatedBy)
        {
            UpdatePermissions(
                canRead: true,
                canWrite: true,
                canDelete: false,
                canShare: false,
                canManagePermissions: false,
                canDownload: true,
                canPrint: true,
                canComment: true,
                canCheckout: true,
                canApprove: false,
                updatedBy: updatedBy);
        }

        /// <summary>
        /// SetReviewerPermissions - Set quyền reviewer (đọc, comment, approve)
        /// </summary>
        public virtual void SetReviewerPermissions(int updatedBy)
        {
            UpdatePermissions(
                canRead: true,
                canWrite: false,
                canDelete: false,
                canShare: false,
                canManagePermissions: false,
                canDownload: true,
                canPrint: true,
                canComment: true,
                canCheckout: false,
                canApprove: true,
                updatedBy: updatedBy);
        }

        /// <summary>
        /// HasPermission - Kiểm tra có quyền cụ thể không
        /// </summary>
        public virtual bool HasPermission(string permissionName)
        {
            if (!IsEffective)
                return false;

            return permissionName.ToLower() switch
            {
                "read" => CanRead,
                "write" => CanWrite,
                "delete" => CanDelete,
                "share" => CanShare,
                "managepermissions" => CanManagePermissions,
                "download" => CanDownload,
                "print" => CanPrint,
                "comment" => CanComment,
                "checkout" => CanCheckout,
                "approve" => CanApprove,
                _ => false
            };
        }

        /// <summary>
        /// GetPermissionLevel - Lấy mức độ permission
        /// </summary>
        public virtual string GetPermissionLevel()
        {
            if (!IsEffective)
                return "None";

            if (CanManagePermissions && CanDelete)
                return "Owner";
            
            if (CanWrite && CanCheckout)
                return "Editor";
            
            if (CanComment && CanApprove)
                return "Reviewer";
            
            if (CanRead)
                return "Reader";

            return "None";
        }
    }
}