using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.UserManagement
{
    /// <summary>
    /// Request để cập nhật user
    /// </summary>
    public class UpdateUserRequest
    {

        [StringLength(256)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Họ tên không được trống")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Họ tên phải có từ 2-200 ký tự")]
        public string FullName { get; set; } = "";

        public UserRole Role { get; set; }

        public Department Department { get; set; }

        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string Position { get; set; }

        public int? ManagerId { get; set; }

        public string Language { get; set; }
        public string Avatar { get; set; }
    }
}
