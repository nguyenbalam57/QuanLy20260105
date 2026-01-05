using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs
{
    /// <summary>
    /// DTO cho báo cáo tổng hợp time tracking
    /// </summary>
    public class TimeTrackingSummaryDto
    {
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalLogs { get; set; }
        public decimal AverageHoursPerDay { get; set; }

        // Additional Metrics
        public decimal BillablePercentage => TotalHours > 0
            ? Math.Round(BillableHours / TotalHours * 100, 2)
            : 0;

        public decimal AverageRevenuePerHour => BillableHours > 0
            ? Math.Round(TotalRevenue / BillableHours, 2)
            : 0;
    }
}
