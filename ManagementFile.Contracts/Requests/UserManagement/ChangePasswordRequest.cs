using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.UserManagement
{
    /// <summary>
    /// Request để đổi mật khẩu
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mật khẩu cũ không được trống")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu mới không được trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có từ 6-100 ký tự")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Xác nhận mật khẩu không được trống")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = "";
    }
}
