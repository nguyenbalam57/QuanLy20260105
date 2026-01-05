using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// Admin dashboard overview data
    /// </summary>
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int TotalFiles { get; set; }
        public int ActiveProjects { get; set; }
        public int OverdueTasks { get; set; }
        public int PendingApprovals { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int NewProjectsThisMonth { get; set; }
        public int CompletedTasksThisMonth { get; set; }
        public int FilesUploadedThisMonth { get; set; }
        public long TotalStorageUsed { get; set; }
        public decimal TotalHoursLoggedThisMonth { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
