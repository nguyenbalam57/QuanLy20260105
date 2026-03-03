using ManagementFile.App.Models.ApiOptions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Services
{
    public class ApiConfiguration
    {
        public string BaseUrl { get; }
        public TimeSpan Timeout { get; }
        public int RetryAttempts { get; }
        public bool EnableLogging { get; }

        public ApiConfiguration(IOptions<ApiOptions> options)
        {
            var o = options.Value;

            BaseUrl = string.IsNullOrWhiteSpace(o.BaseUrl) ? "http://localhost:5190" : o.BaseUrl;

            Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds <= 0 ? 60 : o.TimeoutSeconds);

            RetryAttempts = o.RetryAttempts <= 0 ? 3 : o.RetryAttempts;

            EnableLogging = o.EnableLogging;

            System.Diagnostics.Debug.WriteLine($"🔧 ApiConfiguration loaded (appsetting.json:");
            System.Diagnostics.Debug.WriteLine($"   BaseUrl: {BaseUrl}");
            System.Diagnostics.Debug.WriteLine($"   Timeout: {Timeout}");
            System.Diagnostics.Debug.WriteLine($"   RetryAttempts: {RetryAttempts}");
            System.Diagnostics.Debug.WriteLine($"   EnableLogging: {EnableLogging}");
        }
    }
}
