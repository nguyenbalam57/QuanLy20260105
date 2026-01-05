using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    public class ProjectSummaryDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public string ProjectCode { get; set; } = "";
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
        public decimal CompletionPercentage { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalMembers { get; set; }
        public int TotalFiles { get; set; }
        public long TotalFileSize { get; set; }
        public decimal EstimatedBudget { get; set; }
        public decimal ActualBudget { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public bool IsOverdue { get; set; }
        public int? DaysRemaining { get; set; }
    }

}
