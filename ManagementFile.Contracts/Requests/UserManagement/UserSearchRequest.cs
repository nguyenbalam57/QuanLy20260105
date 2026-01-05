using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.UserManagement
{
    /// <summary>
    /// Request để tìm kiếm users
    /// </summary>
    public class UserSearchRequest
    {
        public string SearchTerm { get; set; }
        public UserRole? Role { get; set; }
        public Department? Department { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "FullName";
        public string SortDirection { get; set; } = "asc";
    }
}
