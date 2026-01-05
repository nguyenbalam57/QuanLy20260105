using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.Models.SummaryAndDashboardModels
{
    /// <summary>
    /// TaskSummary - Tổng hợp thông tin task
    /// </summary>
    public class TaskSummary
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public TaskStatuss Status { get; set; }
        public TaskPriority Priority { get; set; }
        public string AssignedToName { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public decimal Progress { get; set; }
        public int EstimatedHours { get; set; }
        public int ActualHours { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysRemaining { get; set; }
    }
}
