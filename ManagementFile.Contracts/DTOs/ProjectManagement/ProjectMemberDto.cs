using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    public class ProjectMemberDto
    {
        public int Id { get; set; } 
        public int ProjectId { get; set; } 
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public UserRole ProjectRole { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public bool IsActive { get; set; }
        public decimal AllocationPercentage { get; set; }
        public decimal? HourlyRate { get; set; }
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } 
    }
}
