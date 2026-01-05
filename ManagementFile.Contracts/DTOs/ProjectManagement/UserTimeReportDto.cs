using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    /// <summary>
    /// DTO cho báo cáo time tracking theo user
    /// </summary>
    public class UserTimeReportDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }

        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }
        public int TotalLogs { get; set; }

        public List<UserTimeReportProjectSummary> ProjectSummaries { get; set; }
            = new List<UserTimeReportProjectSummary>();
    }

}
