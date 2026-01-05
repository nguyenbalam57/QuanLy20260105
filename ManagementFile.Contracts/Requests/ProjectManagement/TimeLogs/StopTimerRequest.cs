using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    public class StopTimerRequest
    {
        public int TimeLogId { get; set; } = -1;
        public string Description { get; set; }
    }
}
