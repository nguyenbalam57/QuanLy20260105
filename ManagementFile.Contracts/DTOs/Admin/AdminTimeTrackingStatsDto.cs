using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// Time tracking statistics for admin
    /// </summary>
    public class AdminTimeTrackingStatsDto
    {
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalLogs { get; set; }
        public int ActiveUsers { get; set; }
        public List<UserTimeStats> TopUsersByHours { get; set; } = new List<UserTimeStats>();
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
