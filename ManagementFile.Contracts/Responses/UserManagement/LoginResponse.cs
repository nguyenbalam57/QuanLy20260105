using ManagementFile.Contracts.DTOs.UserManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Responses.UserManagement
{
    /// <summary>
    /// Response sau khi đăng nhập thành công
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public UserDto User { get; set; } = null;
        public string SessionToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}
