using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.UserManagement
{
    /// <summary>
    /// Request để reset mật khẩu
    /// </summary>
    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = "";
    }
}
