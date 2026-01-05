using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// User time statistics
    /// </summary>
    public class UserTimeStats
    {
        public int UserId { get; set; } = -1;
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public int LogCount { get; set; }
        public int TotalLogs { get; set; }
    }
}
