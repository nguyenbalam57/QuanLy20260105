
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManagementFile.Contracts.Enums;

namespace ManagementFile.Models.SummaryAndDashboardModels
{
    /// <summary>
    /// UserWorkload - Thống kê workload của user
    /// Dùng để balance work và performance tracking
    /// </summary>
    public class UserWorkload
    {
        public int UserId { get; set; } 
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public UserRole Role { get; set; }
        public Department Department { get; set; }
        public int TotalAssignedTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TodoTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int EstimatedHours { get; set; }
        public int ActualHours { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal EfficiencyRate { get; set; }
        public int ActiveProjects { get; set; }
        public DateTime? LastActivity { get; set; }
    }
}
