using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// System health DTO
    /// </summary>
    public class SystemHealthDto
    {
        public string Status { get; set; } = "";
        public bool IsHealthy { get; set; }
        public bool DatabaseConnected { get; set; }
        public TimeSpan DatabaseResponseTime { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        public DateTime CheckedAt { get; set; }
        public long MemoryUsage { get; set; }
        public double CpuUsage { get; set; }
        public long DiskUsage { get; set; }
        public long DiskTotal { get; set; }
        public int ActiveConnections { get; set; }
        public int ActiveUsers { get; set; }
        public long TotalStorageUsed { get; set; }
        public TimeSpan Uptime { get; set; }
    }
}
