using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    public class CreateTimeLogRequest
    {
        public int TaskId { get; set; } = -1;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Description { get; set; } = "";
        public bool IsBillable { get; set; } = true;
        public decimal? HourlyRate { get; set; }
    }
}
