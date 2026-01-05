using ManagementFile.Contracts.DTOs.UserManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Responses.UserManagement
{
    /// <summary>
    /// Response cho danh sách users với phân trang
    /// </summary>
    public class UserListResponse
    {
        public List<UserDto> Users { get; set; } = new List<UserDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
