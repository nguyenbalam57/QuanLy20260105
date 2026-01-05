using ManagementFile.App.ViewModels;
using ManagementFile.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace ManagementFile.App.ViewModels.Dashboard
{
    /// <summary>
    /// Smart Dashboard ViewModel - Phase 7 Advanced Integration
    /// Tích hợp metrics từ tất cả 5 phases thành unified dashboard
    /// </summary>
    public class SmartDashboardViewModel : BaseViewModel
    {
        #region Fields

        private readonly ServiceManager _serviceManager;
        private readonly DataCache _dataCache;
        private readonly EventBus _eventBus;
        private readonly NavigationService _navigationService;

        private readonly DispatcherTimer _refreshTimer;

        // Dashboard State
        private bool _isLoading;
        private string _loadingMessage;
        private DateTime _lastUpdated;
        private string _systemStatus = "Initializing";

        // Aggregated Metrics
        private DashboardMetrics _overallMetrics;
        private ObservableCollection<PhaseMetricCard> _phaseMetrics;
        private ObservableCollection<QuickActionItem> _quickActions;
        private ObservableCollection<RecentActivityItem> _recentActivities;
        private ObservableCollection<SystemAlertItem> _systemAlerts;

        #endregion

        #region Constructor

        public SmartDashboardViewModel(
            ServiceManager serviceManager,
            DataCache dataCache,
            EventBus eventBus,
            NavigationService navigationService)
        {
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Initialize collections
            PhaseMetrics = new ObservableCollection<PhaseMetricCard>();
            QuickActions = new ObservableCollection<QuickActionItem>();
            RecentActivities = new ObservableCollection<RecentActivityItem>();
            SystemAlerts = new ObservableCollection<SystemAlertItem>();

            // Initialize commands
            InitializeCommands();

            // Setup real-time refresh timer
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // Refresh every 30 seconds
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            // Subscribe to cross-phase events
            SubscribeToEvents();

            // Load initial dashboard data
            Task.Run(async () => await LoadDashboardDataAsync());
            
        }

        #endregion

        #region Properties

        /// <summary>
        /// Is loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Loading message
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage ?? "Loading dashboard...";
            set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                SetProperty(ref _lastUpdated, value);
                OnPropertyChanged(nameof(LastUpdatedDisplay));
            }
        }

        /// <summary>
        /// Display-friendly last updated text
        /// </summary>
        public string LastUpdatedDisplay => $"Last updated: {LastUpdated:HH:mm:ss}";

        /// <summary>
        /// Overall system status
        /// </summary>
        public string SystemStatus
        {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        /// <summary>
        /// Overall dashboard metrics
        /// </summary>
        public DashboardMetrics OverallMetrics
        {
            get => _overallMetrics;
            set => SetProperty(ref _overallMetrics, value);
        }

        /// <summary>
        /// Phase-specific metric cards
        /// </summary>
        public ObservableCollection<PhaseMetricCard> PhaseMetrics
        {
            get => _phaseMetrics;
            set => SetProperty(ref _phaseMetrics, value);
        }

        /// <summary>
        /// Quick action items
        /// </summary>
        public ObservableCollection<QuickActionItem> QuickActions
        {
            get => _quickActions;
            set => SetProperty(ref _quickActions, value);
        }

        /// <summary>
        /// Recent activity items
        /// </summary>
        public ObservableCollection<RecentActivityItem> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }

        /// <summary>
        /// System alert items
        /// </summary>
        public ObservableCollection<SystemAlertItem> SystemAlerts
        {
            get => _systemAlerts;
            set => SetProperty(ref _systemAlerts, value);
        }

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// System status color
        /// </summary>
        public string SystemStatusColor
        {
            get
            {
                switch (SystemStatus)
                {
                    case "Healthy":
                        return "#4CAF50";
                    case "Warning":
                        return "#FF9800";
                    case "Critical":
                        return "#F44336";
                    default:
                        return "#9E9E9E";
                }
            }
        }

        /// <summary>
        /// Is real-time monitoring active
        /// </summary>
        public bool IsMonitoringActive => _refreshTimer?.IsEnabled ?? false;

        /// <summary>
        /// Critical alerts count
        /// </summary>
        public int CriticalAlertsCount => SystemAlerts?.Count(a => a.Level == AlertLevel.Critical) ?? 0;

        #endregion

        #region Commands

        public ICommand RefreshDashboardCommand { get; private set; }
        public ICommand StartMonitoringCommand { get; private set; }
        public ICommand StopMonitoringCommand { get; private set; }
        public ICommand NavigateToPhaseCommand { get; private set; }
        public ICommand ExecuteQuickActionCommand { get; private set; }
        public ICommand ClearAlertsCommand { get; private set; }
        public ICommand ExportDashboardCommand { get; private set; }

        #endregion

        #region Methods

        private void InitializeCommands()
        {
            RefreshDashboardCommand = new AsyncRelayCommand(ExecuteRefreshDashboardAsync);
            StartMonitoringCommand = new RelayCommand(ExecuteStartMonitoring);
            StopMonitoringCommand = new RelayCommand(ExecuteStopMonitoring);
            NavigateToPhaseCommand = new RelayCommand<string>(ExecuteNavigateToPhase);
            ExecuteQuickActionCommand = new RelayCommand<QuickActionItem>(ExecuteQuickAction);
            ClearAlertsCommand = new RelayCommand(ExecuteClearAlerts);
            ExportDashboardCommand = new RelayCommand(ExecuteExportDashboard);
        }

        /// <summary>
        /// Load comprehensive dashboard data from all phases
        /// </summary>
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Loading dashboard metrics...";

                // Load data from all phases
                await LoadPhase1MetricsAsync(); // Admin System
                await LoadPhase2MetricsAsync(); // Project Management
                await LoadPhase3MetricsAsync(); // Client Interface
                await LoadPhase4MetricsAsync(); // Reporting & Analytics
                await LoadPhase5MetricsAsync(); // Optimization & Production

                // Aggregate overall metrics
                AggregateOverallMetrics();

                // Load quick actions and recent activities
                LoadQuickActions();
                LoadRecentActivities();
                LoadSystemAlerts();

                SystemStatus = DetermineSystemStatus();
                LastUpdated = DateTime.Now;

                LoadingMessage = "Dashboard loaded successfully";
            }
            catch (Exception ex)
            {
                SystemStatus = "Error";
                LoadingMessage = $"Error loading dashboard: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Dashboard loading error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load Phase 1 - Admin System metrics
        /// </summary>
        private async Task LoadPhase1MetricsAsync()
        {
            try
            {
                await Task.Delay(100); // Simulate data loading

                var adminMetrics = new PhaseMetricCard
                {
                    PhaseId = "Phase1",
                    PhaseName = "Admin System",
                    PhaseIcon = "👥",
                    PrimaryMetric = "Users",
                    PrimaryValue = GetMockUserCount().ToString(),
                    SecondaryMetrics = new[]
                    {
                        new MetricItem { Label = "Active Sessions", Value = GetMockActiveSessionsCount().ToString() },
                        new MetricItem { Label = "System Health", Value = "Good" },
                        new MetricItem { Label = "Storage Used", Value = "45.2 GB" }
                    },
                    Status = "Healthy",
                    LastUpdate = DateTime.Now
                };

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingMetric = PhaseMetrics.FirstOrDefault(p => p.PhaseId == "Phase1");
                    if (existingMetric != null)
                    {
                        PhaseMetrics.Remove(existingMetric);
                    }
                    PhaseMetrics.Add(adminMetrics);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Phase 1 metrics loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Phase 2 - Project Management metrics
        /// </summary>
        private async Task LoadPhase2MetricsAsync()
        {
            try
            {
                await Task.Delay(100);

                var projectMetrics = new PhaseMetricCard
                {
                    PhaseId = "Phase2",
                    PhaseName = "Projects",
                    PhaseIcon = "📋",
                    PrimaryMetric = "Active Projects",
                    PrimaryValue = GetMockActiveProjectsCount().ToString(),
                    SecondaryMetrics = new[]
                    {
                        new MetricItem { Label = "Total Tasks", Value = GetMockTasksCount().ToString() },
                        new MetricItem { Label = "Completion Rate", Value = "78%" },
                        new MetricItem { Label = "Overdue Tasks", Value = GetMockOverdueTasksCount().ToString() }
                    },
                    Status = "Active",
                    LastUpdate = DateTime.Now
                };

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingMetric = PhaseMetrics.FirstOrDefault(p => p.PhaseId == "Phase2");
                    if (existingMetric != null)
                    {
                        PhaseMetrics.Remove(existingMetric);
                    }
                    PhaseMetrics.Add(projectMetrics);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Phase 2 metrics loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Phase 3 - Client Interface metrics
        /// </summary>
        private async Task LoadPhase3MetricsAsync()
        {
            try
            {
                await Task.Delay(100);

                var clientMetrics = new PhaseMetricCard
                {
                    PhaseId = "Phase3",
                    PhaseName = "Personal Workspace",
                    PhaseIcon = "👤",
                    PrimaryMetric = "My Tasks",
                    PrimaryValue = GetMockMyTasksCount().ToString(),
                    SecondaryMetrics = new[]
                    {
                        new MetricItem { Label = "Files", Value = GetMockMyFilesCount().ToString() },
                        new MetricItem { Label = "Notifications", Value = GetMockNotificationsCount().ToString() },
                        new MetricItem { Label = "Collaboration", Value = "Active" }
                    },
                    Status = "Productive",
                    LastUpdate = DateTime.Now
                };

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingMetric = PhaseMetrics.FirstOrDefault(p => p.PhaseId == "Phase3");
                    if (existingMetric != null)
                    {
                        PhaseMetrics.Remove(existingMetric);
                    }
                    PhaseMetrics.Add(clientMetrics);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Phase 3 metrics loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Phase 4 - Reporting & Analytics metrics
        /// </summary>
        private async Task LoadPhase4MetricsAsync()
        {
            try
            {
                await Task.Delay(100);

                var reportMetrics = new PhaseMetricCard
                {
                    PhaseId = "Phase4",
                    PhaseName = "Analytics",
                    PhaseIcon = "📊",
                    PrimaryMetric = "Reports Generated",
                    PrimaryValue = GetMockReportsCount().ToString(),
                    SecondaryMetrics = new[]
                    {
                        new MetricItem { Label = "Data Points", Value = "125.4K" },
                        new MetricItem { Label = "Export Success", Value = "94%" },
                        new MetricItem { Label = "Trending", Value = "+15%" }
                    },
                    Status = "Analyzing",
                    LastUpdate = DateTime.Now
                };

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingMetric = PhaseMetrics.FirstOrDefault(p => p.PhaseId == "Phase4");
                    if (existingMetric != null)
                    {
                        PhaseMetrics.Remove(existingMetric);
                    }
                    PhaseMetrics.Add(reportMetrics);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Phase 4 metrics loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load Phase 5 - Optimization & Production metrics
        /// </summary>
        private async Task LoadPhase5MetricsAsync()
        {
            try
            {
                await Task.Delay(100);

                var optimizationMetrics = new PhaseMetricCard
                {
                    PhaseId = "Phase5",
                    PhaseName = "Production",
                    PhaseIcon = "🚀",
                    PrimaryMetric = "System Health",
                    PrimaryValue = GetMockSystemHealthScore().ToString(),
                    SecondaryMetrics = new[]
                    {
                        new MetricItem { Label = "Performance", Value = "Excellent" },
                        new MetricItem { Label = "Security", Value = "Secure" },
                        new MetricItem { Label = "Uptime", Value = "99.8%" }
                    },
                    Status = "Optimal",
                    LastUpdate = DateTime.Now
                };

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var existingMetric = PhaseMetrics.FirstOrDefault(p => p.PhaseId == "Phase5");
                    if (existingMetric != null)
                    {
                        PhaseMetrics.Remove(existingMetric);
                    }
                    PhaseMetrics.Add(optimizationMetrics);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Phase 5 metrics loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Aggregate metrics from all phases
        /// </summary>
        private void AggregateOverallMetrics()
        {
            try
            {
                OverallMetrics = new DashboardMetrics
                {
                    TotalUsers = GetMockUserCount(),
                    ActiveProjects = GetMockActiveProjectsCount(),
                    TotalTasks = GetMockTasksCount(),
                    CompletionRate = CalculateOverallCompletionRate(),
                    SystemHealth = CalculateSystemHealthScore().ToString(),
                    PerformanceScore = GetMockSystemHealthScore(),
                    SecurityStatus = "Secure",
                    LastCalculated = DateTime.Now
                };

                OnPropertyChanged(nameof(OverallMetrics));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Metrics aggregation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load quick actions for dashboard
        /// </summary>
        private void LoadQuickActions()
        {
            try
            {
                QuickActions.Clear();

                QuickActions.Add(new QuickActionItem
                {
                    Id = "create-project",
                    Title = "New Project",
                    Description = "Create a new project",
                    Icon = "📋",
                    Category = "Project"
                });

                QuickActions.Add(new QuickActionItem
                {
                    Id = "generate-report",
                    Title = "Generate Report",
                    Description = "Create analytics report",
                    Icon = "📊",
                    Category = "Reports"
                });

                QuickActions.Add(new QuickActionItem
                {
                    Id = "manage-users",
                    Title = "Manage Users",
                    Description = "User administration",
                    Icon = "👥",
                    Category = "Admin"
                });

                QuickActions.Add(new QuickActionItem
                {
                    Id = "system-health",
                    Title = "System Health",
                    Description = "Check system status",
                    Icon = "🚀",
                    Category = "Production"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Quick actions loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load recent activities
        /// </summary>
        private void LoadRecentActivities()
        {
            try
            {
                RecentActivities.Clear();

                RecentActivities.Add(new RecentActivityItem
                {
                    Id = "activity-1",
                    Title = "Project Alpha created",
                    Description = "New project created by admin",
                    Timestamp = DateTime.Now.AddMinutes(-15),
                    Type = ActivityType.ProjectCreated,
                    Icon = "📋"
                });

                RecentActivities.Add(new RecentActivityItem
                {
                    Id = "activity-2", 
                    Title = "User login",
                    Description = "john.doe logged in",
                    Timestamp = DateTime.Now.AddMinutes(-30),
                    Type = ActivityType.UserActivity,
                    Icon = "👤"
                });

                RecentActivities.Add(new RecentActivityItem
                {
                    Id = "activity-3",
                    Title = "Report generated",
                    Description = "Monthly performance report",
                    Timestamp = DateTime.Now.AddHours(-1),
                    Type = ActivityType.ReportGenerated,
                    Icon = "📊"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Recent activities loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load system alerts
        /// </summary>
        private void LoadSystemAlerts()
        {
            try
            {
                SystemAlerts.Clear();

                // Add sample alerts based on system status
                if (GetMockSystemHealthScore() < 85)
                {
                    SystemAlerts.Add(new SystemAlertItem
                    {
                        Id = "alert-1",
                        Title = "Performance Warning",
                        Message = "System performance below optimal",
                        Level = AlertLevel.Warning,
                        Timestamp = DateTime.Now.AddMinutes(-10),
                        Category = "Performance"
                    });
                }

                if (GetMockOverdueTasksCount() > 5)
                {
                    SystemAlerts.Add(new SystemAlertItem
                    {
                        Id = "alert-2",
                        Title = "Overdue Tasks",
                        Message = $"{GetMockOverdueTasksCount()} tasks are overdue",
                        Level = AlertLevel.Warning,
                        Timestamp = DateTime.Now.AddMinutes(-30),
                        Category = "Tasks"
                    });
                }

                OnPropertyChanged(nameof(CriticalAlertsCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ System alerts loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe to cross-phase events
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                // Subscribe to user selection events from Admin
                _eventBus.Subscribe<UserSelectedEvent>(OnUserSelected);

                // Subscribe to project selection events
                _eventBus.Subscribe<ProjectSelectedEvent>(OnProjectSelected);

                // Subscribe to notification events
                _eventBus.Subscribe<NotificationEvent>(OnNotificationReceived);

                // Subscribe to data update events
                _eventBus.Subscribe<DataUpdateEvent>(OnDataUpdated);

                // Subscribe to performance alerts
                _eventBus.Subscribe<PerformanceAlertEvent>(OnPerformanceAlert);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Event subscription error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle user selection from Admin phase
        /// </summary>
        private void OnUserSelected(UserSelectedEvent userEvent)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentActivities.Insert(0, new RecentActivityItem
                    {
                        Id = $"user-selected-{DateTime.Now.Ticks}",
                        Title = "User Selected",
                        Description = "User selected in Admin panel",
                        Timestamp = DateTime.Now,
                        Type = ActivityType.UserActivity,
                        Icon = "👥"
                    });

                    // Keep only recent 10 activities
                    while (RecentActivities.Count > 10)
                    {
                        RecentActivities.RemoveAt(RecentActivities.Count - 1);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ User selected event handling error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle project selection
        /// </summary>
        private void OnProjectSelected(ProjectSelectedEvent projectEvent)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentActivities.Insert(0, new RecentActivityItem
                    {
                        Id = $"project-selected-{DateTime.Now.Ticks}",
                        Title = "Project Selected",
                        Description = "Project selected for viewing",
                        Timestamp = DateTime.Now,
                        Type = ActivityType.ProjectActivity,
                        Icon = "📋"
                    });

                    while (RecentActivities.Count > 10)
                    {
                        RecentActivities.RemoveAt(RecentActivities.Count - 1);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Project selected event handling error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle notification events
        /// </summary>
        private void OnNotificationReceived(NotificationEvent notificationEvent)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentActivities.Insert(0, new RecentActivityItem
                    {
                        Id = $"notification-{DateTime.Now.Ticks}",
                        Title = notificationEvent.Title,
                        Description = notificationEvent.Message,
                        Timestamp = DateTime.Now,
                        Type = ActivityType.Notification,
                        Icon = "🔔"
                    });

                    while (RecentActivities.Count > 10)
                    {
                        RecentActivities.RemoveAt(RecentActivities.Count - 1);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Notification event handling error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle data update events
        /// </summary>
        private void OnDataUpdated(DataUpdateEvent dataEvent)
        {
            try
            {
                // Trigger dashboard refresh when data is updated
                Task.Run(async () => await LoadDashboardDataAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Data update event handling error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle performance alerts
        /// </summary>
        private void OnPerformanceAlert(PerformanceAlertEvent alertEvent)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SystemAlerts.Insert(0, new SystemAlertItem
                    {
                        Id = $"perf-alert-{DateTime.Now.Ticks}",
                        Title = alertEvent.AlertType,
                        Message = alertEvent.Message,
                        Level = AlertLevel.Warning,
                        Timestamp = DateTime.Now,
                        Category = "Performance"
                    });

                    OnPropertyChanged(nameof(CriticalAlertsCount));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Performance alert handling error: {ex.Message}");
            }
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Refresh dashboard data
        /// </summary>
        private async Task ExecuteRefreshDashboardAsync()
        {
            await LoadDashboardDataAsync();
        }

        /// <summary>
        /// Start real-time monitoring
        /// </summary>
        private void ExecuteStartMonitoring()
        {
            try
            {
                _refreshTimer.Start();
                SystemStatus = "Monitoring";
                OnPropertyChanged(nameof(IsMonitoringActive));
                
                System.Diagnostics.Debug.WriteLine("🔄 Dashboard monitoring started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Start monitoring error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop real-time monitoring
        /// </summary>
        private void ExecuteStopMonitoring()
        {
            try
            {
                _refreshTimer.Stop();
                OnPropertyChanged(nameof(IsMonitoringActive));
                
                System.Diagnostics.Debug.WriteLine("⏹️ Dashboard monitoring stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Stop monitoring error: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigate to specific phase
        /// </summary>
        private void ExecuteNavigateToPhase(string phaseId)
        {
            try
            {
                
                switch (phaseId)
                {
                    case "Phase1":
                        _navigationService.NavigateToTab("Admin");
                        break;
                    case "Phase2":
                        _navigationService.NavigateToTab("Projects");
                        break;
                    case "Phase3":
                        _navigationService.NavigateToTab("Client");
                        break;
                    case "Phase4":
                        _navigationService.NavigateToTab("Reports");
                        break;
                    case "Phase5":
                        _navigationService.NavigateToTab("Production");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"⚠️ Unknown phase: {phaseId}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Phase navigation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute quick action
        /// </summary>
        private void ExecuteQuickAction(QuickActionItem action)
        {
            try
            {
                if (action == null) return;


                switch (action.Id)
                {
                    case "create-project":
                        _navigationService.NavigateToTab("Projects");
                        // Trigger new project dialog
                        break;
                    case "generate-report":
                        _navigationService.NavigateToTab("Reports");
                        break;
                    case "manage-users":
                        _navigationService.NavigateToTab("Admin");
                        break;
                    case "system-health":
                        _navigationService.NavigateToTab("Production");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"⚠️ Unknown quick action: {action.Id}");
                        break;
                }

                // Add to recent activities
                RecentActivities.Insert(0, new RecentActivityItem
                {
                    Id = $"quick-action-{DateTime.Now.Ticks}",
                    Title = $"Quick Action: {action.Title}",
                    Description = action.Description,
                    Timestamp = DateTime.Now,
                    Type = ActivityType.QuickAction,
                    Icon = action.Icon
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Quick action execution error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear system alerts
        /// </summary>
        private void ExecuteClearAlerts()
        {
            try
            {
                SystemAlerts.Clear();
                OnPropertyChanged(nameof(CriticalAlertsCount));
                
                System.Diagnostics.Debug.WriteLine("🗑️ System alerts cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Clear alerts error: {ex.Message}");
            }
        }

        /// <summary>
        /// Export dashboard data
        /// </summary>
        private void ExecuteExportDashboard()
        {
            try
            {
                // Mock export functionality
                System.Diagnostics.Debug.WriteLine("📄 Exporting dashboard data...");
                
                // Add to recent activities
                RecentActivities.Insert(0, new RecentActivityItem
                {
                    Id = $"export-{DateTime.Now.Ticks}",
                    Title = "Dashboard Exported",
                    Description = "Dashboard data exported successfully",
                    Timestamp = DateTime.Now,
                    Type = ActivityType.Export,
                    Icon = "📄"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Dashboard export error: {ex.Message}");
            }
        }

        #endregion

        #region Timer Events

        /// <summary>
        /// Refresh timer tick handler
        /// </summary>
        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                await LoadDashboardDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Refresh timer error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Determine overall system status
        /// </summary>
        private string DetermineSystemStatus()
        {
            try
            {
                var healthScore = CalculateSystemHealthScore();
                
                if (healthScore >= 90) return "Healthy";
                if (healthScore >= 70) return "Warning";
                return "Critical";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Calculate overall completion rate
        /// </summary>
        private double CalculateOverallCompletionRate()
        {
            try
            {
                // Mock calculation based on various metrics
                var tasksCompleted = GetMockTasksCount() - GetMockOverdueTasksCount();
                var totalTasks = GetMockTasksCount();
                
                if (totalTasks == 0) return 0;
                
                return Math.Round((double)tasksCompleted / totalTasks * 100, 1);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculate system health score
        /// </summary>
        private int CalculateSystemHealthScore()
        {
            try
            {
                // Aggregate health from all phases
                var scores = new List<int>
                {
                    85, // Admin System
                    GetMockActiveProjectsCount() > 0 ? 90 : 70, // Projects
                    80, // Client Interface
                    95, // Reports
                    GetMockSystemHealthScore() // Production
                };

                return (int)scores.Average();
            }
            catch
            {
                return 75; // Default safe value
            }
        }

        // Mock data methods
        private int GetMockUserCount() => 25;
        private int GetMockActiveSessionsCount() => 8;
        private int GetMockActiveProjectsCount() => 12;
        private int GetMockTasksCount() => 89;
        private int GetMockOverdueTasksCount() => 7;
        private int GetMockMyTasksCount() => 15;
        private int GetMockMyFilesCount() => 47;
        private int GetMockNotificationsCount() => 3;
        private int GetMockReportsCount() => 23;
        private int GetMockSystemHealthScore() => 87;

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Stop monitoring when disposing
                if (_refreshTimer != null)
                {
                    _refreshTimer.Stop();
                }
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    #region Supporting Models

    /// <summary>
    /// Overall dashboard metrics
    /// </summary>
    public class DashboardMetrics
    {
        public int TotalUsers { get; set; }
        public int ActiveProjects { get; set; }
        public int TotalTasks { get; set; }
        public double CompletionRate { get; set; }
        public string SystemHealth { get; set; } = "";
        public int PerformanceScore { get; set; }
        public string SecurityStatus { get; set; } = "";
        public DateTime LastCalculated { get; set; }
    }

    /// <summary>
    /// Phase metric card
    /// </summary>
    public class PhaseMetricCard : INotifyPropertyChanged
    {
        private string _status = "";

        public string PhaseId { get; set; } = "";
        public string PhaseName { get; set; } = "";
        public string PhaseIcon { get; set; } = "";
        public string PrimaryMetric { get; set; } = "";
        public string PrimaryValue { get; set; } = "";
        public MetricItem[] SecondaryMetrics { get; set; } = new MetricItem[0];
        
        public string Status 
        { 
            get => _status;
            set 
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
            }
        }
        
        public DateTime LastUpdate { get; set; }

        public string StatusColor
        {
            get
            {
                var statusLower = Status.ToLower();
                if (statusLower == "healthy" || statusLower == "active" || statusLower == "productive" || 
                    statusLower == "analyzing" || statusLower == "optimal")
                {
                    return "#4CAF50";
                }
                if (statusLower == "warning")
                {
                    return "#FF9800";
                }
                if (statusLower == "critical" || statusLower == "error")
                {
                    return "#F44336";
                }
                return "#9E9E9E";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Metric item for secondary metrics
    /// </summary>
    public class MetricItem
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
    }

    /// <summary>
    /// Quick action item
    /// </summary>
    public class QuickActionItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Category { get; set; } = "";
    }

    /// <summary>
    /// Recent activity item
    /// </summary>
    public class RecentActivityItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public ActivityType Type { get; set; }
        public string Icon { get; set; } = "";

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Timestamp;
                if (timeSpan.TotalMinutes < 1) return "Just now";
                if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
                return $"{(int)timeSpan.TotalDays}d ago";
            }
        }
    }

    /// <summary>
    /// System alert item
    /// </summary>
    public class SystemAlertItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public AlertLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Category { get; set; } = "";

        public string LevelColor
        {
            get
            {
                switch (Level)
                {
                    case AlertLevel.Critical:
                        return "#F44336";
                    case AlertLevel.Warning:
                        return "#FF9800";
                    case AlertLevel.Info:
                        return "#2196F3";
                    default:
                        return "#9E9E9E";
                }
            }
        }

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Timestamp;
                if (timeSpan.TotalMinutes < 1) return "Just now";
                if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
                return $"{(int)timeSpan.TotalDays}d ago";
            }
        }
    }

    /// <summary>
    /// Activity type enumeration
    /// </summary>
    public enum ActivityType
    {
        UserActivity,
        ProjectActivity,
        ProjectCreated,
        TaskCompleted,
        ReportGenerated,
        Notification,
        QuickAction,
        Export
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

    #endregion
}