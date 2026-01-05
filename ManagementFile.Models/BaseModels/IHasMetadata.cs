using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.BaseModels
{
    /// <summary>
    /// IHasMetadata - Interface cho entities có metadata
    /// Standardize metadata handling
    /// </summary>
    public interface IHasMetadata
    {
        /// <summary>
        /// Metadata - Dữ liệu metadata dạng JSON
        /// Lưu trữ thông tin bổ sung không có trong schema cố định
        /// </summary>
        string Metadata { get; set; }

        /// <summary>
        /// GetMetadata - Deserialize metadata thành object
        /// </summary>
        T GetMetadata<T>() where T : class, new();

        /// <summary>
        /// SetMetadata - Serialize object thành metadata JSON
        /// </summary>
        void SetMetadata<T>(T data) where T : class;
    }
}
