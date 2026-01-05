using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.UserManagement
{
    /// <summary>
    /// Request để đăng xuất
    /// </summary>
    public class LogoutRequest
    {
        [Required]
        public string SessionToken { get; set; } = "";
    }
}
