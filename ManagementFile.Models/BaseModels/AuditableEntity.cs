using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.BaseModels
{
    /// <summary>
    /// AuditableEntity - Thực thể có đầy đủ audit trail
    /// Kế thừa từ ActivatableEntity và bổ sung thông tin audit chi tiết
    /// </summary>
    public abstract class AuditableEntity : ActivatableEntity
    {
        /// <summary>
        /// IPAddress - Địa chỉ IP của thao tác cuối cùng
        /// Dùng cho security tracking và location monitoring
        /// </summary>
        [StringLength(45)] // IPv6 support
        public string IPAddress { get; set; } = "";

        /// <summary>
        /// UserAgent - Thông tin trình duyệt/ứng dụng
        /// Tracking platform/browser cho technical debugging
        /// </summary>
        [StringLength(500)]
        public string UserAgent { get; set; } = "";

        /// <summary>
        /// SessionId - ID của phiên làm việc
        /// Liên kết changes với user session
        /// </summary>
        public int SessionId { get; set; } = -1;

        /// <summary>
        /// ChangeNotes - Ghi chú về thay đổi
        /// Lưu comment/notes về changes cho audit purpose
        /// </summary>
        [StringLength(2000)]
        public string ChangeNotes { get; set; } = "";

        /// <summary>
        /// UpdateAuditInfo - Cập nhật thông tin audit
        /// Helper method để update audit information
        /// </summary>
        /// <param name="updatedBy">ID người thực hiện cập nhật</param>
        /// <param name="ipAddress">Địa chỉ IP</param>
        /// <param name="userAgent">User agent</param>
        /// <param name="sessionId">Session ID</param>
        /// <param name="notes">Ghi chú</param>
        public virtual void UpdateAuditInfo(int updatedBy = -1, string ipAddress = "",
            string userAgent = "", int sessionId = -1, string notes = "")
        {
            UpdatedBy = updatedBy;
            IPAddress = ipAddress;
            UserAgent = userAgent;
            SessionId = sessionId;
            ChangeNotes = notes;
            MarkAsUpdated(updatedBy);
        }
    }
}
