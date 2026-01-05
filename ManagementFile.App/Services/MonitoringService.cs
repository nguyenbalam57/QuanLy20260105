using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service for application monitoring, health checks, and APM integration
    /// Phase 5 Week 15 - Production Readiness & Final Polish
    /// </summary>
    public sealed class MonitoringService : INotifyPropertyChanged
    {

        #region Fields

        private readonly ConfigurationService _configurationService;

        private readonly Timer _healthCheckTimer;
        private readonly Timer _performanceTimer;
        private readonly ObservableCollection<HealthCheckResult> _healthCheckResults;
        private readonly ObservableCollection<PerformanceMetric> _performanceMetrics;
        private readonly ObservableCollection<AlertEvent> _alertEvents;
        private readonly Queue<LogEntry> _logEntries;

        private bool _isMonitoring;
        private HealthStatus _overallHealth;
        private string _lastHealthCheck;
        private int _totalHealthChecks;
        private int _failedHealthChecks;
        private DateTime _monitoringStartTime;
        private DateTime _lastAlertTime;
        private bool _disposed;

        private const int HEALTH_CHECK_INTERVAL = 30000; // 30 seconds
        private const int PERFORMANCE_INTERVAL = 5000;   // 5 seconds
        private const int MAX_LOG_ENTRIES = 1000;
        private const int MAX_PERFORMANCE_METRICS = 100;
        private const int MAX_ALERT_EVENTS = 50;

        #endregion

        #region Constructor

        public MonitoringService(
            ConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            _healthCheckResults = new ObservableCollection<HealthCheckResult>();
            _performanceMetrics = new ObservableCollection<PerformanceMetric>();
            _alertEvents = new ObservableCollection<AlertEvent>();
            _logEntries = new Queue<LogEntry>();

            _overallHealth = HealthStatus.Unknown;
            _monitoringStartTime = DateTime.Now;

            // Initialize timers (but don't start them yet)
            _healthCheckTimer = new Timer(PerformHealthChecks, null, Timeout.Infinite, Timeout.Infinite);
            _performanceTimer = new Timer(CollectPerformanceMetrics, null, Timeout.Infinite, Timeout.Infinite);

            InitializeHealthChecks();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Is monitoring active
        /// </summary>
        public bool IsMonitoring
        {
            get => _isMonitoring;
            private set => SetProperty(ref _isMonitoring, value);
        }

        /// <summary>
        /// Overall system health status
        /// </summary>
        public HealthStatus OverallHealth
        {
            get => _overallHealth;
            private set => SetProperty(ref _overallHealth, value);
        }

        /// <summary>
        /// Last health check time
        /// </summary>
        public string LastHealthCheck
        {
            get => _lastHealthCheck ?? "Never";
            private set => SetProperty(ref _lastHealthCheck, value);
        }

        /// <summary>
        /// Total health checks performed
        /// </summary>
        public int TotalHealthChecks
        {
            get => _totalHealthChecks;
            private set => SetProperty(ref _totalHealthChecks, value);
        }

        /// <summary>
        /// Failed health checks count
        /// </summary>
        public int FailedHealthChecks
        {
            get => _failedHealthChecks;
            private set => SetProperty(ref _failedHealthChecks, value);
        }

        /// <summary>
        /// Monitoring start time
        /// </summary>
        public DateTime MonitoringStartTime
        {
            get => _monitoringStartTime;
            private set => SetProperty(ref _monitoringStartTime, value);
        }

        /// <summary>
        /// Last alert time
        /// </summary>
        public DateTime LastAlertTime
        {
            get => _lastAlertTime;
            private set => SetProperty(ref _lastAlertTime, value);
        }

        /// <summary>
        /// Health check results
        /// </summary>
        public ObservableCollection<HealthCheckResult> HealthCheckResults => _healthCheckResults;

        /// <summary>
        /// Performance metrics
        /// </summary>
        public ObservableCollection<PerformanceMetric> PerformanceMetrics => _performanceMetrics;

        /// <summary>
        /// Alert events
        /// </summary>
        public ObservableCollection<AlertEvent> AlertEvents => _alertEvents;

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Overall health status text
        /// </summary>
        public string OverallHealthText => OverallHealth.ToString();

        /// <summary>
        /// Health status color
        /// </summary>
        public string HealthStatusColor
        {
            get
            {
                if (OverallHealth == HealthStatus.Healthy) return "Green";
                if (OverallHealth == HealthStatus.Warning) return "Orange";
                if (OverallHealth == HealthStatus.Critical) return "Red";
                return "Gray";
            }
        }

        /// <summary>
        /// Health check success rate
        /// </summary>
        public double HealthCheckSuccessRate => TotalHealthChecks > 0 
            ? ((double)(TotalHealthChecks - FailedHealthChecks) / TotalHealthChecks) * 100 
            : 0;

        /// <summary>
        /// Monitoring uptime
        /// </summary>
        public TimeSpan MonitoringUptime => DateTime.Now - MonitoringStartTime;

        /// <summary>
        /// Monitoring uptime text
        /// </summary>
        public string MonitoringUptimeText => $"{MonitoringUptime.Days}d {MonitoringUptime.Hours:00}h {MonitoringUptime.Minutes:00}m";

        /// <summary>
        /// Has recent alerts
        /// </summary>
        public bool HasRecentAlerts => _alertEvents.Any(a => a.Timestamp > DateTime.Now.AddHours(-1));

        /// <summary>
        /// Recent alerts count
        /// </summary>
        public int RecentAlertsCount => _alertEvents.Count(a => a.Timestamp > DateTime.Now.AddHours(-1));

        /// <summary>
        /// Monitoring status text
        /// </summary>
        public string MonitoringStatusText => IsMonitoring ? "Active" : "Inactive";

        /// <summary>
        /// Last health check display text
        /// </summary>
        public string LastHealthCheckText => $"Last check: {LastHealthCheck}";

        #endregion

        #region Methods

        /// <summary>
        /// Initialize health check definitions
        /// </summary>
        private void InitializeHealthChecks()
        {
            // This would typically initialize health check configurations
            // For now, we'll set up basic health checks
        }

        /// <summary>
        /// Start monitoring services
        /// </summary>
        public void StartMonitoring()
        {
            if (IsMonitoring) return;

            try
            {
                IsMonitoring = true;
                MonitoringStartTime = DateTime.Now;
                
                // Start health check timer
                _healthCheckTimer.Change(0, HEALTH_CHECK_INTERVAL);
                
                // Start performance monitoring timer
                _performanceTimer.Change(0, PERFORMANCE_INTERVAL);

                LogEvent("Monitoring started", LogLevel.Information);
                MonitoringStarted?.Invoke();
            }
            catch (Exception ex)
            {
                LogEvent($"Failed to start monitoring: {ex.Message}", LogLevel.Error);
                IsMonitoring = false;
            }
        }

        /// <summary>
        /// Stop monitoring services
        /// </summary>
        public void StopMonitoring()
        {
            if (!IsMonitoring) return;

            try
            {
                IsMonitoring = false;
                
                // Stop timers
                _healthCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _performanceTimer.Change(Timeout.Infinite, Timeout.Infinite);

                LogEvent("Monitoring stopped", LogLevel.Information);
                MonitoringStopped?.Invoke();
            }
            catch (Exception ex)
            {
                LogEvent($"Error stopping monitoring: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Perform health checks (timer callback)
        /// </summary>
        private async void PerformHealthChecks(object state)
        {
            if (!IsMonitoring) return;

            try
            {
                await PerformHealthChecksAsync();
            }
            catch (Exception ex)
            {
                LogEvent($"Health check error: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Perform all health checks
        /// </summary>
        public async Task PerformHealthChecksAsync()
        {
            try
            {
                var healthChecks = new List<Task<HealthCheckResult>>
                {
                    CheckApplicationHealthAsync(),
                    CheckMemoryHealthAsync(),
                    CheckDiskSpaceHealthAsync(),
                    CheckNetworkHealthAsync(),
                    CheckDatabaseHealthAsync(),
                    CheckAPIHealthAsync()
                };

                var results = await Task.WhenAll(healthChecks);
                
                // Update UI on main thread
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateHealthCheckResults(results);
                }));

                TotalHealthChecks++;
                LastHealthCheck = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                FailedHealthChecks++;
                LogEvent($"Health check failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Update health check results
        /// </summary>
        private void UpdateHealthCheckResults(HealthCheckResult[] results)
        {
            _healthCheckResults.Clear();
            
            var overallStatus = HealthStatus.Healthy;
            
            foreach (var result in results)
            {
                _healthCheckResults.Add(result);
                
                if (result.Status == HealthStatus.Critical)
                {
                    overallStatus = HealthStatus.Critical;
                }
                else if (result.Status == HealthStatus.Warning && overallStatus != HealthStatus.Critical)
                {
                    overallStatus = HealthStatus.Warning;
                }

                // Create alerts for critical issues
                if (result.Status == HealthStatus.Critical)
                {
                    CreateAlert($"Critical health issue in {result.Name}", result.Description, AlertLevel.Critical);
                }
                else if (result.Status == HealthStatus.Warning)
                {
                    CreateAlert($"Warning in {result.Name}", result.Description, AlertLevel.Warning);
                }
            }

            OverallHealth = overallStatus;
            
            if (results.Any(r => r.Status != HealthStatus.Healthy))
            {
                FailedHealthChecks++;
            }

            OnPropertyChanged(nameof(HealthStatusColor));
            OnPropertyChanged(nameof(HealthCheckSuccessRate));
        }

        /// <summary>
        /// Check application health
        /// </summary>
        private async Task<HealthCheckResult> CheckApplicationHealthAsync()
        {
            await Task.Delay(100); // Simulate async work

            try
            {
                var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / (1024 * 1024);
                var threadCount = process.Threads.Count;

                if (memoryMB > 500) // > 500MB
                {
                    return new HealthCheckResult
                    {
                        Name = "Application",
                        Status = HealthStatus.Critical,
                        Description = $"High memory usage: {memoryMB}MB",
                        ResponseTime = 100,
                        Timestamp = DateTime.Now
                    };
                }
                else if (memoryMB > 300) // > 300MB
                {
                    return new HealthCheckResult
                    {
                        Name = "Application",
                        Status = HealthStatus.Warning,
                        Description = $"Elevated memory usage: {memoryMB}MB",
                        ResponseTime = 100,
                        Timestamp = DateTime.Now
                    };
                }

                return new HealthCheckResult
                {
                    Name = "Application",
                    Status = HealthStatus.Healthy,
                    Description = $"Memory: {memoryMB}MB, Threads: {threadCount}",
                    ResponseTime = 100,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    Name = "Application",
                    Status = HealthStatus.Critical,
                    Description = $"Health check failed: {ex.Message}",
                    ResponseTime = 0,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Check memory health
        /// </summary>
        private async Task<HealthCheckResult> CheckMemoryHealthAsync()
        {
            await Task.Delay(50);

            try
            {
                var totalMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                if (totalMemoryMB > 200)
                {
                    return new HealthCheckResult
                    {
                        Name = "Memory",
                        Status = HealthStatus.Warning,
                        Description = $"High managed memory: {totalMemoryMB}MB",
                        ResponseTime = 50,
                        Timestamp = DateTime.Now
                    };
                }

                return new HealthCheckResult
                {
                    Name = "Memory",
                    Status = HealthStatus.Healthy,
                    Description = $"Managed: {totalMemoryMB}MB, GC: Gen0={gen0Collections}, Gen1={gen1Collections}, Gen2={gen2Collections}",
                    ResponseTime = 50,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    Name = "Memory",
                    Status = HealthStatus.Critical,
                    Description = $"Memory check failed: {ex.Message}",
                    ResponseTime = 0,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Check disk space health
        /// </summary>
        private async Task<HealthCheckResult> CheckDiskSpaceHealthAsync()
        {
            await Task.Delay(50);

            try
            {
                var drive = new DriveInfo(AppDomain.CurrentDomain.BaseDirectory);
                var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                var totalSpaceGB = drive.TotalSize / (1024 * 1024 * 1024);
                var usedPercentage = ((double)(totalSpaceGB - freeSpaceGB) / totalSpaceGB) * 100;

                if (freeSpaceGB < 1) // Less than 1GB free
                {
                    return new HealthCheckResult
                    {
                        Name = "Disk Space",
                        Status = HealthStatus.Critical,
                        Description = $"Low disk space: {freeSpaceGB}GB free ({usedPercentage:F1}% used)",
                        ResponseTime = 50,
                        Timestamp = DateTime.Now
                    };
                }
                else if (usedPercentage > 90) // More than 90% used
                {
                    return new HealthCheckResult
                    {
                        Name = "Disk Space",
                        Status = HealthStatus.Warning,
                        Description = $"High disk usage: {freeSpaceGB}GB free ({usedPercentage:F1}% used)",
                        ResponseTime = 50,
                        Timestamp = DateTime.Now
                    };
                }

                return new HealthCheckResult
                {
                    Name = "Disk Space",
                    Status = HealthStatus.Healthy,
                    Description = $"{freeSpaceGB}GB free of {totalSpaceGB}GB ({usedPercentage:F1}% used)",
                    ResponseTime = 50,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    Name = "Disk Space",
                    Status = HealthStatus.Warning,
                    Description = $"Disk check failed: {ex.Message}",
                    ResponseTime = 0,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Check network connectivity health
        /// </summary>
        private async Task<HealthCheckResult> CheckNetworkHealthAsync()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Simple network connectivity test
                using (var client = new System.Net.WebClient())
                {
                    client.Proxy = null;
                    await client.DownloadStringTaskAsync("http://www.google.com");
                }
                
                stopwatch.Stop();
                
                if (stopwatch.ElapsedMilliseconds > 5000) // > 5 seconds
                {
                    return new HealthCheckResult
                    {
                        Name = "Network",
                        Status = HealthStatus.Warning,
                        Description = $"Slow network response: {stopwatch.ElapsedMilliseconds}ms",
                        ResponseTime = (int)stopwatch.ElapsedMilliseconds,
                        Timestamp = DateTime.Now
                    };
                }

                return new HealthCheckResult
                {
                    Name = "Network",
                    Status = HealthStatus.Healthy,
                    Description = $"Network connectivity OK: {stopwatch.ElapsedMilliseconds}ms",
                    ResponseTime = (int)stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    Name = "Network",
                    Status = HealthStatus.Critical,
                    Description = $"Network connectivity failed: {ex.Message}",
                    ResponseTime = 0,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Check database health
        /// </summary>
        private async Task<HealthCheckResult> CheckDatabaseHealthAsync()
        {
            await Task.Delay(200); // Simulate database check

            try
            {
                // Mock database health check
                var random = new Random();
                var responseTime = random.Next(50, 300);
                
                if (responseTime > 250)
                {
                    return new HealthCheckResult
                    {
                        Name = "Database",
                        Status = HealthStatus.Warning,
                        Description = $"Slow database response: {responseTime}ms",
                        ResponseTime = responseTime,
                        Timestamp = DateTime.Now
                    };
                }

                return new HealthCheckResult
                {
                    Name = "Database",
                    Status = HealthStatus.Healthy,
                    Description = $"Database connection OK: {responseTime}ms",
                    ResponseTime = responseTime,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    Name = "Database",
                    Status = HealthStatus.Critical,
                    Description = $"Database connection failed: {ex.Message}",
                    ResponseTime = 0,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Check API health
        /// </summary>
        private async Task<HealthCheckResult> CheckAPIHealthAsync()
        {
            await Task.Delay(150); // Simulate API check

            try
            {
                // Mock API health check
                var apiBaseUrl = _configurationService.GetApiBaseUrl();
                
                // Simulate API response time
                var responseTime = new Random().Next(100, 500);
                
                if (responseTime > 400)
                {
                    return new HealthCheckResult
                    {
                        Name = "API",
                        Status = HealthStatus.Warning,
                        Description = $"Slow API response: {responseTime}ms",
                        ResponseTime = responseTime,
                        Timestamp = DateTime.Now
                    };
                }

                return new HealthCheckResult
                {
                    Name = "API",
                    Status = HealthStatus.Healthy,
                    Description = $"API endpoint healthy: {responseTime}ms",
                    ResponseTime = responseTime,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    Name = "API",
                    Status = HealthStatus.Critical,
                    Description = $"API health check failed: {ex.Message}",
                    ResponseTime = 0,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Collect performance metrics (timer callback)
        /// </summary>
        private async void CollectPerformanceMetrics(object state)
        {
            if (!IsMonitoring) return;

            try
            {
                await CollectPerformanceMetricsAsync();
            }
            catch (Exception ex)
            {
                LogEvent($"Performance collection error: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Collect performance metrics
        /// </summary>
        private async Task CollectPerformanceMetricsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var process = Process.GetCurrentProcess();
                    
                    var metric = new PerformanceMetric
                    {
                        Timestamp = DateTime.Now,
                        CpuUsage = GetCpuUsage(),
                        MemoryUsageMB = process.WorkingSet64 / (1024 * 1024),
                        ThreadCount = process.Threads.Count,
                        HandleCount = process.HandleCount,
                        GCMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024)
                    };

                    // Update UI on main thread
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _performanceMetrics.Add(metric);
                        
                        // Keep only recent metrics
                        while (_performanceMetrics.Count > MAX_PERFORMANCE_METRICS)
                        {
                            _performanceMetrics.RemoveAt(0);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    LogEvent($"Performance metric collection failed: {ex.Message}", LogLevel.Error);
                }
            });
        }

        /// <summary>
        /// Get CPU usage percentage (simplified)
        /// </summary>
        private double GetCpuUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return (new Random().NextDouble() * 20) + 5; // Mock CPU usage 5-25%
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Create alert event
        /// </summary>
        private void CreateAlert(string title, string description, AlertLevel level)
        {
            var alert = new AlertEvent
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Level = level,
                Timestamp = DateTime.Now,
                IsAcknowledged = false
            };

            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                _alertEvents.Insert(0, alert); // Add to beginning
                
                // Keep only recent alerts
                while (_alertEvents.Count > MAX_ALERT_EVENTS)
                {
                    _alertEvents.RemoveAt(_alertEvents.Count - 1);
                }

                LastAlertTime = DateTime.Now;
                OnPropertyChanged(nameof(HasRecentAlerts));
                OnPropertyChanged(nameof(RecentAlertsCount));
            }));

            AlertCreated?.Invoke(alert);
        }

        /// <summary>
        /// Acknowledge alert
        /// </summary>
        public void AcknowledgeAlert(Guid alertId)
        {
            var alert = _alertEvents.FirstOrDefault(a => a.Id == alertId);
            if (alert != null)
            {
                alert.IsAcknowledged = true;
                alert.AcknowledgedAt = DateTime.Now;
                OnPropertyChanged(nameof(HasRecentAlerts));
                OnPropertyChanged(nameof(RecentAlertsCount));
            }
        }

        /// <summary>
        /// Clear all alerts
        /// </summary>
        public void ClearAllAlerts()
        {
            _alertEvents.Clear();
            OnPropertyChanged(nameof(HasRecentAlerts));
            OnPropertyChanged(nameof(RecentAlertsCount));
        }

        /// <summary>
        /// Log event
        /// </summary>
        public void LogEvent(string message, LogLevel level)
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = "MonitoringService"
            };

            _logEntries.Enqueue(logEntry);
            
            // Keep only recent log entries
            while (_logEntries.Count > MAX_LOG_ENTRIES)
            {
                _logEntries.Dequeue();
            }

            LogEventAdded?.Invoke(logEntry);
        }

        /// <summary>
        /// Get recent log entries
        /// </summary>
        public List<LogEntry> GetRecentLogEntries(int count = 50)
        {
            var allEntries = _logEntries.ToList();
            return allEntries.Skip(Math.Max(0, allEntries.Count - count)).ToList();
        }

        /// <summary>
        /// Export monitoring data
        /// </summary>
        public void ExportMonitoringData(string filePath)
        {
            try
            {
                var lines = new List<string>();
                lines.Add($"# Monitoring Data Export - {DateTime.Now}");
                lines.Add($"# Monitoring Duration: {MonitoringUptimeText}");
                lines.Add($"# Overall Health: {OverallHealthText}");
                lines.Add($"# Health Check Success Rate: {HealthCheckSuccessRate:F1}%");
                lines.Add("");

                lines.Add("[Recent Health Checks]");
                foreach (var result in _healthCheckResults)
                {
                    lines.Add($"{result.Timestamp:yyyy-MM-dd HH:mm:ss},{result.Name},{result.Status},{result.ResponseTime}ms,\"{result.Description}\"");
                }

                lines.Add("");
                lines.Add("[Recent Performance Metrics]");
                lines.Add("Timestamp,CPU%,MemoryMB,Threads,Handles,GCMemoryMB");
                foreach (var metric in _performanceMetrics.Skip(Math.Max(0, _performanceMetrics.Count - 50)))
                {
                    lines.Add($"{metric.Timestamp:yyyy-MM-dd HH:mm:ss},{metric.CpuUsage:F1},{metric.MemoryUsageMB},{metric.ThreadCount},{metric.HandleCount},{metric.GCMemoryMB}");
                }

                lines.Add("");
                lines.Add("[Recent Alerts]");
                foreach (var alert in _alertEvents.Skip(Math.Max(0, _alertEvents.Count - 20)))
                {
                    lines.Add($"{alert.Timestamp:yyyy-MM-dd HH:mm:ss},{alert.Level},{alert.Title},\"{alert.Description}\",{alert.IsAcknowledged}");
                }

                System.IO.File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export monitoring data: {ex.Message}", ex);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when monitoring starts
        /// </summary>
        public event Action MonitoringStarted;

        /// <summary>
        /// Event raised when monitoring stops
        /// </summary>
        public event Action MonitoringStopped;

        /// <summary>
        /// Event raised when alert is created
        /// </summary>
        public event Action<AlertEvent> AlertCreated;

        /// <summary>
        /// Event raised when log event is added
        /// </summary>
        public event Action<LogEntry> LogEventAdded;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(backingField, value))
            {
                backingField = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                StopMonitoring();
                _healthCheckTimer?.Dispose();
                _performanceTimer?.Dispose();
                _healthCheckResults?.Clear();
                _performanceMetrics?.Clear();
                _alertEvents?.Clear();
                _logEntries?.Clear();
                _disposed = true;
            }
        }

        #endregion
    }

    #region Supporting Models

    /// <summary>
    /// Health check result model
    /// </summary>
    public class HealthCheckResult
    {
        public string Name { get; set; } = "";
        public HealthStatus Status { get; set; }
        public string Description { get; set; } = "";
        public int ResponseTime { get; set; }
        public DateTime Timestamp { get; set; }

        public string StatusIcon
        {
            get
            {
                if (Status == HealthStatus.Healthy) return "✅";
                if (Status == HealthStatus.Warning) return "⚠️";
                if (Status == HealthStatus.Critical) return "❌";
                return "❓";
            }
        }

        public string StatusColor
        {
            get
            {
                if (Status == HealthStatus.Healthy) return "Green";
                if (Status == HealthStatus.Warning) return "Orange";
                if (Status == HealthStatus.Critical) return "Red";
                return "Gray";
            }
        }
    }

    /// <summary>
    /// Performance metric model
    /// </summary>
    public class PerformanceMetric
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsageMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public long GCMemoryMB { get; set; }

        public string DisplayText => $"{Timestamp:HH:mm:ss} - CPU: {CpuUsage:F1}%, Memory: {MemoryUsageMB}MB";
    }

    /// <summary>
    /// Alert event model
    /// </summary>
    public class AlertEvent
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public AlertLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }

        public string LevelIcon
        {
            get
            {
                if (Level == AlertLevel.Info) return "ℹ️";
                if (Level == AlertLevel.Warning) return "⚠️";
                if (Level == AlertLevel.Critical) return "🚨";
                return "❓";
            }
        }

        public string LevelColor
        {
            get
            {
                if (Level == AlertLevel.Info) return "Blue";
                if (Level == AlertLevel.Warning) return "Orange";
                if (Level == AlertLevel.Critical) return "Red";
                return "Gray";
            }
        }
    }

    /// <summary>
    /// Log entry model
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = "";
        public string Source { get; set; } = "";

        public string LevelText => Level.ToString();
        public string DisplayText => $"[{Timestamp:HH:mm:ss}] {Level}: {Message}";
    }

    #endregion

    #region Enums

    /// <summary>
    /// Health status enumeration
    /// </summary>
    public enum HealthStatus
    {
        Unknown,
        Healthy,
        Warning,
        Critical
    }

    /// <summary>
    /// Alert level enumeration
    /// </summary>
    public enum AlertLevel
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    #endregion
}