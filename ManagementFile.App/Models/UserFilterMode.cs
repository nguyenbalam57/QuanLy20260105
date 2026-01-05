using ManagementFile.Contracts.Enums;
using System;

namespace ManagementFile.App.Models
{
    /// <summary>
    /// Chế độ lọc user cho SearchableUserComboBox
    /// </summary>
    public enum UserFilterMode
    {
        /// <summary>
        /// Tất cả users trong hệ thống
        /// </summary>
        AllUsers,
        
        /// <summary>
        /// Chỉ users chưa thuộc project hiện tại (dùng khi thêm member)
        /// </summary>
        AvailableUsersForProject,
        
        /// <summary>
        /// Chỉ users đã thuộc project hiện tại (dùng khi reassign task)
        /// </summary>
        ProjectMembersOnly,
        
        /// <summary>
        /// Users theo department cụ thể
        /// </summary>
        ByDepartment,
        
        /// <summary>
        /// Users theo role cụ thể  
        /// </summary>
        ByRole
    }

    /// <summary>   
    /// Tùy chọn tìm kiếm user
    /// </summary>
    public class UserSearchOptions
    {
        /// <summary>
        /// Chế độ lọc
        /// </summary>
        public UserFilterMode FilterMode { get; set; } = UserFilterMode.AllUsers;
        
        /// <summary>
        /// ID của project (dùng cho filter theo project)
        /// </summary>
        public int? ProjectId { get; set; }

        /// <summary>
        /// Department cần lọc (dùng cho filter theo department)
        /// </summary>
        public Department? Department { get; set; } = null;

        /// <summary>
        /// Role cần lọc (dùng cho filter theo role)
        /// </summary>
        public UserRole? Role { get; set; } = null;
        
        /// <summary>
        /// Số lượng kết quả tối đa
        /// </summary>
        public int MaxResults { get; set; } = 20;

        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Chỉ lấy user đang active
        /// </summary>
        public bool ActiveUsersOnly { get; set; } = true;

    }
}