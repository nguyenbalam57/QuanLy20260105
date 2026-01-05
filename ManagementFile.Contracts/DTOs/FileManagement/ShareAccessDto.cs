using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// Share access log DTO
    /// </summary>
    public class ShareAccessDto
    {
        public int Id { get; set; } = -1;
        public string AccessType { get; set; } = "";
        public DateTime AccessedAt { get; set; }
        public string IPAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public int AccessedBy { get; set; } = -1;
        public bool IsSuccessful { get; set; }
    }
}
