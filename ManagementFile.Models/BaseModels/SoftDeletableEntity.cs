using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.BaseModels
{
    /// <summary>
    /// SoftDeletableEntity - Thực thể hỗ trợ soft delete
    /// Kế thừa từ VersionableEntity và bổ sung tính năng soft delete
    /// </summary>
    public abstract class SoftDeletableEntity : VersionableEntity
    {
        /// <summary>
        /// IsDeleted - Trạng thái đã xóa (soft delete)
        /// Đánh dấu record đã xóa mà không xóa vật lý khỏi database
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// DeletedAt - Thời gian thực hiện soft delete
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// DeletedBy - ID người thực hiện soft delete
        /// </summary>
        public int DeletedBy { get; set; } = -1;

        /// <summary>
        /// DeleteReason - Lý do thực hiện soft delete
        /// </summary>
        [StringLength(1000)]
        public string DeleteReason { get; set; } = "";

        /// <summary>
        /// SoftDelete - Thực hiện soft delete
        /// </summary>
        /// <param name="deletedBy">ID người thực hiện xóa</param>
        /// <param name="reason">Lý do xóa</param>
        public virtual void SoftDelete(int deletedBy = 0, string reason = "")
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            DeleteReason = reason;
            MarkAsUpdated(deletedBy);
        }

        /// <summary>
        /// Restore - Khôi phục record đã xóa
        /// </summary>
        /// <param name="restoredBy">ID người thực hiện khôi phục</param>
        public virtual void Restore(int restoredBy = 0)
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = -1;
            DeleteReason = "";
            MarkAsUpdated(restoredBy);
        }
    }
}
