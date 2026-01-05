using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// Top performer DTO
    /// </summary>
    public class TopPerformerDto
    {
        public int Rank { get; set; }
        public int UserId { get; set; } 
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public int CompletedTasks { get; set; }
        public decimal TotalHoursLogged { get; set; }
        public decimal TotalHours { get; set; }
        public int ProjectsContributed { get; set; }
        public decimal PerformanceScore { get; set; }
        public decimal Score { get; set; }
        public string Department { get; set; } = "";
        public string Role { get; set; } = "";
    }
}
