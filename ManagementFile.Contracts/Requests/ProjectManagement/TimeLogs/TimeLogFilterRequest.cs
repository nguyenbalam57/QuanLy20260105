using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    public class TimeLogFilterRequest
    {
        public int TaskId { get; set; } = -1;
        public int UserId { get; set; } = -1;
        public int ProjectId { get; set; } = -1;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsBillable { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "StartTime";
        public string SortDirection { get; set; } = "desc";
    }
}
