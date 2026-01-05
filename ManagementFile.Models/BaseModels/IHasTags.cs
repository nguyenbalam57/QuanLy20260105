using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.BaseModels
{
    /// <summary>
    /// IHasTags - Interface cho entities có tags
    /// Standardize tagging system
    /// </summary>
    public interface IHasTags
    {
        /// <summary>
        /// Tags - Danh sách tags dạng JSON array
        /// Dùng cho categorization và filtering
        /// </summary>
        string Tags { get; set; }

        /// <summary>
        /// GetTags - Lấy danh sách tags
        /// </summary>
        List<string> GetTags();

        /// <summary>
        /// SetTags - Set danh sách tags
        /// </summary>
        void SetTags(List<string> tags);

        /// <summary>
        /// AddTag - Thêm tag mới
        /// </summary>
        void AddTag(string tag);

        /// <summary>
        /// RemoveTag - Xóa tag
        /// </summary>
        void RemoveTag(string tag);

        /// <summary>
        /// HasTag - Kiểm tra có tag không
        /// </summary>
        bool HasTag(string tag);
    }
}
