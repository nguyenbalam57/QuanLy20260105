using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Models.ApiOptions
{
    public sealed class ApiOptions
    {
        public const string SectionName = "Api";

        public string BaseUrl { get; set; } = "http://localhost:5190";
        public int TimeoutSeconds { get; set; } = 60;
        public int RetryAttempts { get; set; } = 3;
        public bool EnableLogging { get; set; } = true;
    }
}
