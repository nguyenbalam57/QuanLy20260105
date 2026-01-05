using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    /// <summary>
    /// Request để lưu toàn bộ weekly timesheet
    /// </summary>
    public class SaveWeeklyTimesheetRequest
    {
        public DateTime WeekStartDate { get; set; }
        public List<WeeklyTimesheetEntryRequest> Entries { get; set; }
            = new List<WeeklyTimesheetEntryRequest>();
        public bool SubmitForApproval { get; set; } = false;
    }

    public class WeeklyTimesheetEntryRequest
    {
        public int TaskId { get; set; }
        public decimal? HourlyRate { get; set; }

        // 7 ngày trong tuần (0=Monday, 6=Sunday)
        public List<DailyTimeEntryRequest> DailyEntries { get; set; }
            = new List<DailyTimeEntryRequest>();
    }

    public class DailyTimeEntryRequest
    {
        public int DayIndex { get; set; } // 0-6 (Mon-Sun)
        public decimal Hours { get; set; }
        public string Note { get; set; } = "";
    }
}
