using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.Admin
{
    /// <summary>
    /// Request để export data
    /// </summary>
    public class DataExportRequest
    {
        [Required]
        public string DataType { get; set; } = ""; // Users, Projects, Tasks, Files, etc.
        public string Format { get; set; } = "CSV"; // CSV, Excel
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> Columns { get; set; }
        public Dictionary<string, object> Filters { get; set; }
    }
}
