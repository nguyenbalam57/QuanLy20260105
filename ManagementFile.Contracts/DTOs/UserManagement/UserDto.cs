using System;
using System.Collections.Generic;
using System.Text;
using ManagementFile.Contracts.Enums;

namespace ManagementFile.Contracts.DTOs.UserManagement
{
    /// <summary>
    /// DTO cho User (không bao gồm sensitive data)
    /// </summary>
    public class UserDto
    {
        public int Id { get; set; } 
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public UserRole Role { get; set; }
        public Department Department { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string Position { get; set; } = "";
        public int ManagerId { get; set; }
        public string Avatar { get; set; } = "";
        public string Language { get; set; } = "";
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DisplayName { get; set; } = "";
        public bool IsAccountLocked { get; set; }
    }


}
