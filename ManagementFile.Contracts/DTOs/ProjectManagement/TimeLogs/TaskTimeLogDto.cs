using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs
{
    /// <summary>
    /// Task Time Log DTO
    /// </summary>
    public class TaskTimeLogDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Duration { get; set; } // in minutes (phút)
        public string Description { get; set; } = "";
        public bool IsBillable { get; set; }
        public decimal? HourlyRate { get; set; }
        public string DurationFormatted { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
    }
}
