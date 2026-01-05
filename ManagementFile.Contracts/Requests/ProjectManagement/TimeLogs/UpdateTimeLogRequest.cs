using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    public class UpdateTimeLogRequest
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Description { get; set; } = "";
        public bool IsBillable { get; set; }
        public decimal? HourlyRate { get; set; }
    }
}
