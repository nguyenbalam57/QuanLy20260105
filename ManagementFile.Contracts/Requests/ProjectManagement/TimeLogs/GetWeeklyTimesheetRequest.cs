using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    /// <summary>
    /// Request để lấy weekly timesheet
    /// </summary>
    public class GetWeeklyTimesheetRequest
    {
        public DateTime WeekStartDate { get; set; }
        public int? UserId { get; set; } // Null = current user
        public int? ProjectId { get; set; }
    }
}
