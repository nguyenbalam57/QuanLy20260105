using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.UserManagement
{
    /// <summary>
    /// Request để đăng nhập
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username hoặc email không được trống")]
        public string UsernameOrEmail { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu không được trống")]
        public string Password { get; set; } = "";

        /// <summary>
        /// Ghi nhớ đăng nhập (extend session time)
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }
}
