using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.Projects
{
    public class PauseProjectRequest
    {
        public string Reason { get; set; } = "";
    }
}
