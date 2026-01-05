using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    /// <summary>
    /// Request để copy timesheet từ tuần khác
    /// </summary>
    public class CopyWeekTimesheetRequest
    {
        public DateTime SourceWeekStartDate { get; set; }
        public DateTime TargetWeekStartDate { get; set; }
        public bool IncludeNotes { get; set; } = false;
    }
}
