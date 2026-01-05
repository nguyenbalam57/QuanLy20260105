using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để filter tasks
    /// </summary>
    public class TaskFilterRequest
    {
        public int ProjectId { get; set; }
        public int? ReporterId { get; set; } 
        public int? AssignedToId { get; set; }
        public TaskStatuss? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DueDateStart { get; set; }
        public DateTime? DueDateEnd { get; set; }
        public string SearchTerm { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? IsActive { get; set; }
        public int? ParentTaskId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }
}
