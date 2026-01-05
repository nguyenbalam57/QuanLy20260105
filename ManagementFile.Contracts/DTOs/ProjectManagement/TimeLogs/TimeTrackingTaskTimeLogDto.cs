using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs
{
    /// <summary>
    /// DTO đầy đủ cho Time Tracking (dùng cho Weekly Timesheet)
    /// </summary>
    public class TimeTrackingTaskTimeLogDto
    {
        public int Id { get; set; }

        // Task Info
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string TaskCode { get; set; } = "";
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";

        // User Info
        public int UserId { get; set; }
        public string UserName { get; set; } = "";

        // Time Info
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Duration { get; set; } // Minutes

        // Calculated Properties
        public decimal Hours => Duration / 60m;
        public string DurationFormatted => $"{Duration / 60}h {Duration % 60}m";
        public bool IsRunning { get; set; }

        // Financial Info
        public bool IsBillable { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal TotalCost => IsBillable && HourlyRate.HasValue
            ? Duration / 60m * HourlyRate.Value
            : 0;

        // Description
        public string Description { get; set; } = "";

        // Audit Fields
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
