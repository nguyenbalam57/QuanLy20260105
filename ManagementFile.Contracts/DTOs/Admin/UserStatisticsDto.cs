using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// User statistics DTO
    /// </summary>
    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int NewUsers { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> UsersByDepartment { get; set; } = new Dictionary<string, int>();
        public Dictionary<DateTime, int> RegistrationTrend { get; set; } = new Dictionary<DateTime, int>();
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
