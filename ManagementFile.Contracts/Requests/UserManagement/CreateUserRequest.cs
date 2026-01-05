using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.UserManagement
{
    /// <summary>
    /// Request để tạo user mới
    /// </summary>
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Username không được trống")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username phải có từ 3-50 ký tự")]
        public string Username { get; set; } = "";

        [StringLength(256)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Họ tên không được trống")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Họ tên phải có từ 2-200 ký tự")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu không được trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6-100 ký tự")]
        public string Password { get; set; } = "";

        public UserRole Role { get; set; } = UserRole.Staff;
        public Department Department { get; set; } = Department.OTHER;

        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string Position { get; set; }

        public int? ManagerId { get; set; }

        public string Language { get; set; } = "vi-VN";
    }
}
