using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    public class ProjectTimeReportDto
    {
        public int ProjectId { get; set; } = -1;
        public string ProjectName { get; set; } = "";
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }
        public int TotalLogs { get; set; }
        public int EstimatedHours { get; set; }
        public int ActualHours { get; set; }
        public List<ProjectTimeReportUserSummary> UserSummaries { get; set; } = new List<ProjectTimeReportUserSummary>();
    }
}
