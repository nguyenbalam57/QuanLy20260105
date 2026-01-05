using System;
using System.Collections.Generic;
using System.Configuration;
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

        public ApiConfiguration()
        {
            BaseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "http://localhost:5190";

            if (int.TryParse(ConfigurationManager.AppSettings["ApiTimeout"], out int timeout))
                Timeout = TimeSpan.FromSeconds(timeout);
            else
                Timeout = TimeSpan.FromSeconds(60);

            if (int.TryParse(ConfigurationManager.AppSettings["ApiRetryAttempts"], out int retryAttempts))
                RetryAttempts = retryAttempts;
            else
                RetryAttempts = 3;

            if (bool.TryParse(ConfigurationManager.AppSettings["EnableApiLogging"], out bool enableLogging))
                EnableLogging = enableLogging;
            else
                EnableLogging = true;

            System.Diagnostics.Debug.WriteLine($"🔧 ApiConfiguration loaded:");
            System.Diagnostics.Debug.WriteLine($"   BaseUrl: {BaseUrl}");
            System.Diagnostics.Debug.WriteLine($"   Timeout: {Timeout}");
            System.Diagnostics.Debug.WriteLine($"   RetryAttempts: {RetryAttempts}");
            System.Diagnostics.Debug.WriteLine($"   EnableLogging: {EnableLogging}");
        }
    }
}
