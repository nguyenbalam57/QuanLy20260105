using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.BaseModels
{
    /// <summary>
    /// BaseEntity - Thực thể cơ sở cho tất cả các model
    /// Chứa các thuộc tính chung như ID, timestamps, versioning và audit tracking
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Id - Mã định danh duy nhất của thực thể
        /// Sử dụng GUID để tránh conflict và dễ dàng merge data từ nhiều nguồn
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// CreatedAt - Thời gian tạo thực thể
        /// Tự động set khi tạo record mới, không thể thay đổi sau đó
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// CreatedBy - ID người tạo thực thể
        /// Liên kết với User.Id để tracking ownership và responsibility
        /// </summary>
        [Required]
        public int CreatedBy { get; set; } = -1;

        /// <summary>
        /// UpdatedAt - Thời gian cập nhật cuối cùng
        /// Tự động update mỗi khi có thay đổi để tracking changes
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// UpdatedBy - ID người cập nhật cuối cùng
        /// Tracking user thực hiện thay đổi gần nhất
        /// </summary>
        public int UpdatedBy { get; set; } = -1;

        /// <summary>
        /// Version - Số phiên bản cho optimistic concurrency control
        /// Tự động tăng mỗi lần update để phát hiện concurrent modifications
        /// </summary>
        [ConcurrencyCheck]
        public long Version { get; set; } = 1;

        /// <summary>
        /// RowVersion - Timestamp cho SQL Server concurrency
        /// Sử dụng cho .NET Framework với SQL Server
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }

        /// <summary>
        /// MarkAsUpdated - Đánh dấu entity đã được cập nhật
        /// Tự động set UpdatedAt, UpdatedBy và tăng Version
        /// </summary>
        /// <param name="updatedBy">ID người thực hiện cập nhật</param>
        public virtual void MarkAsUpdated(int updatedBy = 0)
        {
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
            Version++;
        }

        /// <summary>
        /// Clone - Tạo bản sao entity với ID mới
        /// Hữu ích cho việc duplicate records hoặc create versions
        /// </summary>
        public virtual T Clone<T>() where T : BaseEntity, new()
        {
            var clone = new T
            {
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };
            return clone;
        }
    }
}
