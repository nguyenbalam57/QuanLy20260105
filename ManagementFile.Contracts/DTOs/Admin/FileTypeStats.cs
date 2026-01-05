using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Admin
{
    /// <summary>
    /// File type statistics
    /// </summary>
    public class FileTypeStats
    {
        public int Count { get; set; }
        public long TotalSize { get; set; }
        public long AverageSize { get; set; }
    }
}
