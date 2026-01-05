using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.BaseModels
{
    /// <summary>
    /// VersionableEntity - Thực thể hỗ trợ version management chi tiết
    /// Kế thừa từ BaseEntity và bổ sung tính năng version control nâng cao
    /// </summary>
    public abstract class VersionableEntity : BaseEntity
    {
        /// <summary>
        /// VersionNumber - Số phiên bản hiển thị (v1.0, v1.1, v2.0...)
        /// Dùng để hiển thị version user-friendly, có thể custom format
        /// </summary>
        [StringLength(50)]
        public string VersionNumber { get; set; } = "v1.0";

        /// <summary>
        /// IsCurrentVersion - Đánh dấu có phải version hiện tại không
        /// Chỉ một version được đánh dấu current trong cùng một entity group
        /// </summary>
        public bool IsCurrentVersion { get; set; } = true;

        /// <summary>
        /// ParentVersionId - ID của version cha (nếu là version con)
        /// Tạo version tree để track evolution của entity
        /// </summary>
        public int ParentVersionId { get; set; } = -1;

        /// <summary>
        /// MasterEntityId - ID của entity gốc (bất biến)
        /// Nhóm tất cả versions của cùng một entity logic
        /// </summary>
        [Required]
        public int MasterEntityId { get; set; }

        /// <summary>
        /// VersionNotes - Ghi chú về thay đổi trong version này
        /// Mô tả what changed, why changed cho version history
        /// </summary>
        [StringLength(2000)]
        public string VersionNotes { get; set; } = "";

        /// <summary>
        /// IsLocked - Version có bị khóa không (không thể edit)
        /// Dùng để protect approved/released versions
        /// </summary>
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// LockedBy - ID người khóa version
        /// </summary>
        public int LockedBy { get; set; }  = -1;

        /// <summary>
        /// LockedAt - Thời gian khóa version
        /// </summary>
        public DateTime? LockedAt { get; set; }

        /// <summary>
        /// CreateNewVersion - Tạo version mới từ version hiện tại
        /// </summary>
        /// <param name="versionNumber">Số version mới</param>
        /// <param name="createdBy">ID người tạo</param>
        /// <param name="notes">Ghi chú về version mới</param>
        public virtual T CreateNewVersion<T>(string versionNumber, int createdBy, string notes = "") where T : VersionableEntity, new()
        {
            var newVersion = Clone<T>();
            newVersion.VersionNumber = versionNumber;
            newVersion.ParentVersionId = Id;
            newVersion.MasterEntityId = MasterEntityId > -1 ? Id : MasterEntityId;
            newVersion.VersionNotes = notes;
            newVersion.CreatedBy = createdBy;
            newVersion.IsCurrentVersion = true;

            // Đánh dấu version cũ không còn là current
            IsCurrentVersion = false;
            MarkAsUpdated(createdBy);

            return newVersion;
        }

        /// <summary>
        /// LockVersion - Khóa version để không thể chỉnh sửa
        /// </summary>
        /// <param name="lockedBy">ID người khóa</param>
        /// <param name="reason">Lý do khóa</param>
        public virtual void LockVersion(int lockedBy, string reason = "")
        {
            IsLocked = true;
            LockedBy = lockedBy;
            LockedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(reason))
                VersionNotes += $"\n[LOCKED: {reason}]";
            MarkAsUpdated(lockedBy);
        }

        /// <summary>
        /// UnlockVersion - Mở khóa version
        /// </summary>
        /// <param name="unlockedBy">ID người mở khóa</param>
        public virtual void UnlockVersion(int unlockedBy)
        {
            IsLocked = false;
            LockedBy = -1;
            LockedAt = null;
            MarkAsUpdated(unlockedBy);
        }
    }
}
