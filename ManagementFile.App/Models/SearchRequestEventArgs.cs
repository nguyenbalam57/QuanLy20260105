using System;

namespace ManagementFile.App.Models
{
    /// <summary>
    /// Dữ liệu sự kiện cho các loại search request
    /// </summary>
    public class SearchRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Văn bản tìm kiếm
        /// </summary>
        public string SearchText { get; set; } = "";
    }

    /// <summary>
    /// Dữ liệu sự kiện cho user search request
    /// </summary>
    public class UserSearchRequestEventArgs : SearchRequestEventArgs
    {
        /// <summary>
        /// Tùy chọn tìm kiếm user
        /// </summary>
        public UserSearchOptions Options { get; set; }

        /// <summary>
        /// Độ dài tối thiểu cho văn bản tìm kiếm
        /// </summary>
        public int MinLength { get; set; } = 2;

        /// <summary>
        /// ID của user cần tìm (nếu tìm theo ID)
        /// </summary>
        public int? UserId { get; set; }
    }
}