using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// Task statistics DTO
    /// </summary>
    public class TaskStatisticsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int NewTasks { get; set; }
        public Dictionary<string, int> TasksByStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TasksByPriority { get; set; } = new Dictionary<string, int>();
        public decimal AverageProgress { get; set; }
        public decimal CompletionRate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
