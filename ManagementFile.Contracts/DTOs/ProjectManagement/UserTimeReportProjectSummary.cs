using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    public class UserTimeReportProjectSummary
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public int LogCount { get; set; }
    }
}
