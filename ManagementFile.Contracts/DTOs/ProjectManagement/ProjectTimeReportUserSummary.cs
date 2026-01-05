using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    public class ProjectTimeReportUserSummary
    {
        public int UserId { get; set; } = -1;
        public string UserName { get; set; } = "";
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public int LogCount { get; set; }
    }
}
