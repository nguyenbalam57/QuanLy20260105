using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service for performance monitoring and optimization
    /// Phase 5 - Polish & Optimization Implementation
    /// </summary>
    public sealed class OptimizationService : INotifyPropertyChanged, IDisposable
    {

        #region Fields

        private System.Timers.Timer _performanceMonitorTimer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;
        private readonly Process _currentProcess;
        
        private double _cpuUsage;
        private long _memoryUsage;
        private int _threadCount;
        private long _workingSetMemory;
        private double _applicationStartupTime;
        private DateTime _lastPerformanceUpdate;
        
        private readonly Queue<PerformanceSnapshot> _performanceHistory;
        private readonly Dictionary<string, TimeSpan> _operationTimings;
        private readonly List<string> _performanceIssues;

        private bool _isMonitoring;
        private bool _disposed;

        #endregion

        #region Constructor

        public OptimizationService()
        {
            _currentProcess = Process.GetCurrentProcess();
            _performanceHistory = new Queue<PerformanceSnapshot>();
            _operationTimings = new Dictionary<string, TimeSpan>();
            _performanceIssues = new List<string>();

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception)
            {
                // Performance counters may not be available in all environments
            }

            InitializePerformanceMonitoring();
            RecordApplicationStartup();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Current CPU usage percentage
        /// </summary>
        public double CpuUsage
        {
            get => _cpuUsage;
            private set => SetProperty(ref _cpuUsage, value);
        }

        /// <summary>
        /// Current memory usage in MB
        /// </summary>
        public long MemoryUsage
        {
            get => _memoryUsage;
            private set => SetProperty(ref _memoryUsage, value);
        }

        /// <summary>
        /// Current thread count
        /// </summary>
        public int ThreadCount
        {
            get => _threadCount;
            private set => SetProperty(ref _threadCount, value);
        }

        /// <summary>
        /// Working set memory in bytes
        /// </summary>
        public long WorkingSetMemory
        {
            get => _workingSetMemory;
            private set => SetProperty(ref _workingSetMemory, value);
        }

        /// <summary>
        /// Application startup time in seconds
        /// </summary>
        public double ApplicationStartupTime
        {
            get => _applicationStartupTime;
            private set => SetProperty(ref _applicationStartupTime, value);
        }

        /// <summary>
        /// Last performance update time
        /// </summary>
        public DateTime LastPerformanceUpdate
        {
            get => _lastPerformanceUpdate;
            private set => SetProperty(ref _lastPerformanceUpdate, value);
        }

        /// <summary>
        /// Is performance monitoring active
        /// </summary>
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set => SetProperty(ref _isMonitoring, value);
        }

        /// <summary>
        /// Performance history snapshots
        /// </summary>
        public IEnumerable<PerformanceSnapshot> PerformanceHistory => _performanceHistory.AsEnumerable();

        /// <summary>
        /// Current performance issues
        /// </summary>
        public IEnumerable<string> PerformanceIssues => _performanceIssues.AsEnumerable();

        /// <summary>
        /// Operation timing statistics
        /// </summary>
        public IReadOnlyDictionary<string, TimeSpan> OperationTimings => _operationTimings;

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Memory usage in MB text
        /// </summary>
        public string MemoryUsageText => $"{MemoryUsage:N0} MB";

        /// <summary>
        /// CPU usage text
        /// </summary>
        public string CpuUsageText => $"{CpuUsage:F1}%";

        /// <summary>
        /// Thread count text
        /// </summary>
        public string ThreadCountText => $"{ThreadCount} threads";

        /// <summary>
        /// Application startup time text
        /// </summary>
        public string StartupTimeText => $"{ApplicationStartupTime:F2}s";

        /// <summary>
        /// Performance status summary
        /// </summary>
        public string PerformanceStatusText
        {
            get
            {
                if (MemoryUsage > 200) return "High Memory Usage";
                if (CpuUsage > 80) return "High CPU Usage";
                if (PerformanceIssues.Any()) return $"{PerformanceIssues.Count()} Issues Detected";
                return "Performance Optimal";
            }
        }

        /// <summary>
        /// Performance status color
        /// </summary>
        public string PerformanceStatusColor
        {
            get
            {
                if (MemoryUsage > 200 || CpuUsage > 80) return "Red";
                if (MemoryUsage > 150 || CpuUsage > 60) return "Orange";
                return "Green";
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize performance monitoring
        /// </summary>
        private void InitializePerformanceMonitoring()
        {
            _performanceMonitorTimer = new System.Timers.Timer(5000); // Update every 5 seconds
            _performanceMonitorTimer.Elapsed += OnPerformanceMonitorTimer;
            _performanceMonitorTimer.AutoReset = true;
            
            StartMonitoring();
        }

        /// <summary>
        /// Record application startup time
        /// </summary>
        private void RecordApplicationStartup()
        {
            var startTime = _currentProcess.StartTime;
            ApplicationStartupTime = (DateTime.Now - startTime).TotalSeconds;
        }

        /// <summary>
        /// Start performance monitoring
        /// </summary>
        public void StartMonitoring()
        {
            if (!_isMonitoring && !_disposed)
            {
                _performanceMonitorTimer?.Start();
                IsMonitoring = true;
                UpdatePerformanceMetrics();
            }
        }

        /// <summary>
        /// Stop performance monitoring
        /// </summary>
        public void StopMonitoring()
        {
            if (_isMonitoring)
            {
                _performanceMonitorTimer?.Stop();
                IsMonitoring = false;
            }
        }

        /// <summary>
        /// Performance monitor timer event
        /// </summary>
        private void OnPerformanceMonitorTimer(object sender, ElapsedEventArgs e)
        {
            Application.Current?.Dispatcher.BeginInvoke(new Action(UpdatePerformanceMetrics));
        }

        /// <summary>
        /// Update performance metrics
        /// </summary>
        public void UpdatePerformanceMetrics()
        {
            try
            {
                // Update process metrics
                _currentProcess.Refresh();
                MemoryUsage = _currentProcess.WorkingSet64 / (1024 * 1024); // Convert to MB
                ThreadCount = _currentProcess.Threads.Count;
                WorkingSetMemory = _currentProcess.WorkingSet64;

                // Update system metrics
                try
                {
                    if (_cpuCounter != null)
                        CpuUsage = _cpuCounter.NextValue();
                }
                catch (Exception)
                {
                    // Fallback if performance counters fail
                    CpuUsage = 0;
                }

                LastPerformanceUpdate = DateTime.Now;

                // Add to performance history
                var snapshot = new PerformanceSnapshot
                {
                    Timestamp = DateTime.Now,
                    CpuUsage = CpuUsage,
                    MemoryUsage = MemoryUsage,
                    ThreadCount = ThreadCount
                };

                _performanceHistory.Enqueue(snapshot);

                // Keep only last 100 snapshots
                while (_performanceHistory.Count > 100)
                {
                    _performanceHistory.Dequeue();
                }

                // Check for performance issues
                CheckPerformanceIssues();
                
                NotifyPropertiesChanged();
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Performance monitoring error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check for performance issues
        /// </summary>
        private void CheckPerformanceIssues()
        {
            _performanceIssues.Clear();

            // Memory usage checks
            if (MemoryUsage > 250)
                _performanceIssues.Add("Critical: Memory usage above 250MB");
            else if (MemoryUsage > 200)
                _performanceIssues.Add("Warning: High memory usage detected");

            // CPU usage checks
            if (CpuUsage > 90)
                _performanceIssues.Add("Critical: CPU usage above 90%");
            else if (CpuUsage > 70)
                _performanceIssues.Add("Warning: High CPU usage detected");

            // Thread count checks
            if (ThreadCount > 50)
                _performanceIssues.Add("Warning: High thread count detected");

            // Check for memory leaks (increasing trend)
            if (_performanceHistory.Count >= 10)
            {
                var recent = _performanceHistory.Skip(_performanceHistory.Count - 10).ToList();
                var memoryTrend = recent.Last().MemoryUsage - recent.First().MemoryUsage;
                
                if (memoryTrend > 50) // More than 50MB increase
                    _performanceIssues.Add("Warning: Possible memory leak detected");
            }
        }

        /// <summary>
        /// Start timing an operation
        /// </summary>
        public IDisposable StartOperation(string operationName)
        {
            return new OperationTimer(operationName, this);
        }

        /// <summary>
        /// Record operation timing
        /// </summary>
        internal void RecordOperationTiming(string operationName, TimeSpan duration)
        {
            _operationTimings[operationName] = duration;

            // Check for slow operations
            if (duration.TotalMilliseconds > 5000) // 5 seconds
                _performanceIssues.Add($"Slow operation detected: {operationName} took {duration.TotalSeconds:F2}s");

            NotifyPropertyChanged(nameof(OperationTimings));
        }

        /// <summary>
        /// Get performance recommendations
        /// </summary>
        public List<string> GetPerformanceRecommendations()
        {
            var recommendations = new List<string>();

            if (MemoryUsage > 150)
                recommendations.Add("Consider implementing lazy loading for large datasets");

            if (ThreadCount > 30)
                recommendations.Add("Review thread usage and implement thread pooling");

            if (CpuUsage > 60)
                recommendations.Add("Consider moving heavy operations to background threads");

            if (_operationTimings.Any(kvp => kvp.Value.TotalMilliseconds > 3000))
                recommendations.Add("Optimize slow operations with caching or async patterns");

            if (ApplicationStartupTime > 5)
                recommendations.Add("Improve application startup time with lazy initialization");

            if (!recommendations.Any())
                recommendations.Add("Application performance is optimal!");

            return recommendations;
        }

        /// <summary>
        /// Force garbage collection
        /// </summary>
        public void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // Update metrics after GC
            UpdatePerformanceMetrics();
        }

        /// <summary>
        /// Get memory usage by generation
        /// </summary>
        public Dictionary<int, long> GetMemoryByGeneration()
        {
            return new Dictionary<int, long>
            {
                { 0, GC.CollectionCount(0) },
                { 1, GC.CollectionCount(1) },
                { 2, GC.CollectionCount(2) }
            };
        }

        /// <summary>
        /// Clear performance history
        /// </summary>
        public void ClearPerformanceHistory()
        {
            _performanceHistory.Clear();
            _operationTimings.Clear();
            _performanceIssues.Clear();
            NotifyPropertiesChanged();
        }

        /// <summary>
        /// Notify all properties changed
        /// </summary>
        private void NotifyPropertiesChanged()
        {
            NotifyPropertyChanged(nameof(MemoryUsageText));
            NotifyPropertyChanged(nameof(CpuUsageText));
            NotifyPropertyChanged(nameof(ThreadCountText));
            NotifyPropertyChanged(nameof(PerformanceStatusText));
            NotifyPropertyChanged(nameof(PerformanceStatusColor));
            NotifyPropertyChanged(nameof(PerformanceIssues));
            NotifyPropertyChanged(nameof(PerformanceHistory));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(backingField, value))
            {
                backingField = value;
                NotifyPropertyChanged(propertyName);
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
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
                
                _performanceMonitorTimer?.Dispose();
                _cpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                _currentProcess?.Dispose();
                
                _disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Performance snapshot for historical tracking
    /// </summary>
    public class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public int ThreadCount { get; set; }

        public string TimestampText => Timestamp.ToString("HH:mm:ss");
        public string MemoryText => $"{MemoryUsage:N0} MB";
        public string CpuText => $"{CpuUsage:F1}%";
    }

    /// <summary>
    /// Operation timer for measuring performance
    /// </summary>
    internal class OperationTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly OptimizationService _service;
        private readonly Stopwatch _stopwatch;

        public OperationTimer(string operationName, OptimizationService service)
        {
            _operationName = operationName;
            _service = service;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _service.RecordOperationTiming(_operationName, _stopwatch.Elapsed);
        }
    }
}