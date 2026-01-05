using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.BaseModels
{
    /// <summary>
    /// ActivatableEntity - Thực thể có thể kích hoạt/vô hiệu hóa
    /// Kế thừa từ SoftDeletableEntity và bổ sung tính năng activate/deactivate
    /// </summary>
    public abstract class ActivatableEntity : SoftDeletableEntity
    {
        /// <summary>
        /// IsActive - Trạng thái hoạt động
        /// Enable/disable entity mà không cần soft delete
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ActivatedAt - Thời gian kích hoạt gần nhất
        /// </summary>
        public DateTime? ActivatedAt { get; set; }

        /// <summary>
        /// ActivatedBy - ID người kích hoạt gần nhất
        /// </summary
        public int ActivatedBy { get; set; } = -1;

        /// <summary>
        /// DeactivatedAt - Thời gian vô hiệu hóa gần nhất
        /// </summary>
        public DateTime? DeactivatedAt { get; set; }

        /// <summary>
        /// DeactivatedBy - ID người vô hiệu hóa gần nhất
        /// </summary>
        public int DeactivatedBy { get; set; } = -1;

        /// <summary>
        /// DeactivateReason - Lý do vô hiệu hóa
        /// </summary>
        [StringLength(1000)]
        public string DeactivateReason { get; set; } = "";

        /// <summary>
        /// Activate - Kích hoạt entity
        /// </summary>
        /// <param name="activatedBy">ID người thực hiện kích hoạt</param>
        public virtual void Activate(int activatedBy = -1)
        {
            IsActive = true;
            ActivatedAt = DateTime.UtcNow;
            ActivatedBy = activatedBy;
            DeactivatedAt = null;
            DeactivatedBy = -1;
            DeactivateReason = "";
            MarkAsUpdated(activatedBy);
        }

        /// <summary>
        /// Deactivate - Vô hiệu hóa entity
        /// </summary>
        /// <param name="deactivatedBy">ID người thực hiện vô hiệu hóa</param>
        /// <param name="reason">Lý do vô hiệu hóa</param>
        public virtual void Deactivate(int deactivatedBy = -1, string reason = "")
        {
            IsActive = false;
            DeactivatedAt = DateTime.UtcNow;
            DeactivatedBy = deactivatedBy;
            DeactivateReason = reason;
            ActivatedAt = null;
            ActivatedBy = -1;
            MarkAsUpdated(deactivatedBy);
        }

        /// <summary>
        /// ToggleActive - Chuyển đổi trạng thái active
        /// </summary>
        /// <param name="changedBy">ID người thực hiện thay đổi</param>
        /// <param name="reason">Lý do thay đổi (chỉ dùng khi deactivate)</param>
        public virtual void ToggleActive(int changedBy = -1, string reason = "")
        {
            if (IsActive)
                Deactivate(changedBy, reason);
            else
                Activate(changedBy);
        }
    }
}
