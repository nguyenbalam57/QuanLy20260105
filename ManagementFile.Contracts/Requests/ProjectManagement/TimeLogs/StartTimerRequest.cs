using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs
{
    public class StartTimerRequest
    {
        public int TaskId { get; set; } = -1;
        public string Description { get; set; } = "";
        public bool IsBillable { get; set; } = true;
    }
}
