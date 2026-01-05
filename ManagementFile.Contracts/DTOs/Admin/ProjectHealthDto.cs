using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// Project health DTO
    /// </summary>
    public class ProjectHealthDto
    {
        public int ProjectId { get; set; } = -1;
        public string ProjectCode { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal CompletionPercentage { get; set; }
        public decimal TaskCompletionRate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public decimal HealthScore { get; set; }
        public string HealthStatus { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsOverdue { get; set; }
        public decimal BudgetVariance { get; set; }
    }
}
