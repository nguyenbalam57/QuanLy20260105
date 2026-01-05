using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.SummaryAndDashboardModels
{
    /// <summary>
    /// ProjectSummary - Tổng hợp thông tin dự án
    /// Dùng cho dashboard và reporting
    /// </summary>
    public class ProjectSummary
    {
        public int ProjectId { get; set; } 
        public string ProjectCode { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public ProjectStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int TotalTasks { get; set; } = -1;
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TodoTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TotalMembers { get; set; }
        public decimal EstimatedBudget { get; set; }
        public decimal ActualBudget { get; set; }
        public int EstimatedHours { get; set; }
        public int ActualHours { get; set; }
        public bool IsOverdue { get; set; }
        public string ProjectManagerName { get; set; } = "";
        public string ClientName { get; set; } = "";
    }
}
