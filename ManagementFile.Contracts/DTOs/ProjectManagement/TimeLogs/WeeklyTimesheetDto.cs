using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs
{
    /// <summary>
    /// DTO cho weekly timesheet
    /// </summary>
    public class WeeklyTimesheetDto
    {
        public int UserId { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }

        public List<WeeklyTimesheetTaskDto> TaskEntries { get; set; }
            = new List<WeeklyTimesheetTaskDto>();

        public decimal TotalHours { get; set; }

        // Daily totals
        public List<decimal> DailyTotals => Enumerable.Range(0, 7)
            .Select(dayIndex => TaskEntries.Sum(t =>
                t.DailyEntries.FirstOrDefault(d => d.DayIndex == dayIndex)?.Hours ?? 0))
            .ToList();
    }

    public class WeeklyTimesheetTaskDto
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string TaskCode { get; set; } = "";
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";

        public decimal? HourlyRate { get; set; }

        public List<WeeklyDailyEntryDto> DailyEntries { get; set; }
            = new List<WeeklyDailyEntryDto>();

        public decimal TotalHours => DailyEntries.Sum(d => d.Hours);
    }

    public class WeeklyDailyEntryDto
    {
        public int DayIndex { get; set; } // 0-6 (Mon-Sun)
        public DateTime Date { get; set; }
        public decimal Hours { get; set; }
        public string Note { get; set; } = "";
        public List<int> LogIds { get; set; } = new List<int>();
    }

    public class TaskForTimesheetDto
    {
        public int TaskId { get; set; }
        public int? ParentTaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string TaskCode { get; set; } = "";
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public decimal DefaultHourlyRate { get; set; }
    }

    public class WeeklyTimesheetValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
