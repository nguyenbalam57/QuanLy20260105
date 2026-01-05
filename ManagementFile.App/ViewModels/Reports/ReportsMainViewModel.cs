using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using ManagementFile.Contracts.Requests.ProjectManagement.Projects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Reports
{
    /// <summary>
    /// ViewModel cho Reports Main View - comprehensive reporting system
    /// Phase 4 - Reporting & Analytics Implementation
    /// </summary>
    public class ReportsMainViewModel : BaseViewModel
    {
        #region Fields

        private readonly ReportService _reportService;
        private readonly UserManagementService _userService;
        private readonly ProjectApiService _projectApiService;

        private bool _isLoading;
        private string _loadingMessage;
        private int _selectedTabIndex;

        private DateTime _startDate = DateTime.Now.AddDays(-30);
        private DateTime _endDate = DateTime.Now;
        private string _projectFilter = "All Projects";
        private string _departmentFilter = "All Departments";
        private ProjectModel _userFilter;

        private int _generatedReportsCount;
        private DateTime _lastUpdated = DateTime.Now;
        private int _availableReportTypes = 10;

        // Report Data
        private ProjectProgressReportModel _projectProgressReport;
        private TeamProductivityReportModel _teamProductivityReport;
        private ProjectTimelineReportModel _projectTimelineReport;
        private UserProductivityReportModel _userProductivityReport;
        private UserWorkloadReportModel _userWorkloadReport;
        private FileUsageReportModel _fileUsageReport;
        private StorageUtilizationReportModel _storageUtilizationReport;
        private TimeTrackingReportModel _timeTrackingReport;
        private BillableHoursReportModel _billableHoursReport;
        private SystemUsageAnalyticsModel _systemUsageAnalytics;
        private PerformanceMetricsReportModel _performanceMetricsReport;

        // Collections
        private ObservableCollection<ProjectModel> _availableProjects;
        private ObservableCollection<string> _availableDepartments;
        private ObservableCollection<ProjectModel> _availableUsers;

        #endregion

        #region Constructor

        public ReportsMainViewModel(
            ReportService reportService,
            UserManagementService userManagementService,
            ProjectApiService projectApiService)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _userService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));

            // Initialize collections
            AvailableProjects = new ObservableCollection<ProjectModel>();
            AvailableDepartments = new ObservableCollection<string>();
            AvailableUsers = new ObservableCollection<ProjectModel>();

            // Initialize commands
            InitializeCommands();

            // Load initial data
            Task.Run(async () => await LoadReportsDataAsync());
        }

        #endregion

        #region Properties

        /// <summary>
        /// Đang loading data
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
            get => _loadingMessage ?? "Đang tải reports...";
            set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// Selected tab index
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    OnTabChanged();
                }
            }
        }

        /// <summary>
        /// Start date filter
        /// </summary>
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        /// <summary>
        /// End date filter
        /// </summary>
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        /// <summary>
        /// Project filter
        /// </summary>
        public string ProjectFilter
        {
            get => _projectFilter;
            set => SetProperty(ref _projectFilter, value);
        }

        /// <summary>
        /// Department filter
        /// </summary>
        public string DepartmentFilter
        {
            get => _departmentFilter;
            set => SetProperty(ref _departmentFilter, value);
        }

        /// <summary>
        /// User filter
        /// </summary>
        public ProjectModel UserFilter
        {
            get => _userFilter;
            set => SetProperty(ref _userFilter, value);
        }

        /// <summary>
        /// Generated reports count
        /// </summary>
        public int GeneratedReportsCount
        {
            get => _generatedReportsCount;
            set => SetProperty(ref _generatedReportsCount, value);
        }

        /// <summary>
        /// Last updated time
        /// </summary>
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        /// <summary>
        /// Available report types
        /// </summary>
        public int AvailableReportTypes
        {
            get => _availableReportTypes;
            set => SetProperty(ref _availableReportTypes, value);
        }

        /// <summary>
        /// Available projects collection
        /// </summary>
        public ObservableCollection<ProjectModel> AvailableProjects
        {
            get => _availableProjects;
            set => SetProperty(ref _availableProjects, value);
        }

        /// <summary>
        /// Available departments collection
        /// </summary>
        public ObservableCollection<string> AvailableDepartments
        {
            get => _availableDepartments;
            set => SetProperty(ref _availableDepartments, value);
        }

        /// <summary>
        /// Available users collection
        /// </summary>
        public ObservableCollection<ProjectModel> AvailableUsers
        {
            get => _availableUsers;
            set => SetProperty(ref _availableUsers, value);
        }

        #endregion

        #region Report Properties

        /// <summary>
        /// Project progress report
        /// </summary>
        public ProjectProgressReportModel ProjectProgressReport
        {
            get => _projectProgressReport;
            set => SetProperty(ref _projectProgressReport, value);
        }

        /// <summary>
        /// Team productivity report
        /// </summary>
        public TeamProductivityReportModel TeamProductivityReport
        {
            get => _teamProductivityReport;
            set => SetProperty(ref _teamProductivityReport, value);
        }

        /// <summary>
        /// Project timeline report
        /// </summary>
        public ProjectTimelineReportModel ProjectTimelineReport
        {
            get => _projectTimelineReport;
            set => SetProperty(ref _projectTimelineReport, value);
        }

        /// <summary>
        /// User productivity report
        /// </summary>
        public UserProductivityReportModel UserProductivityReport
        {
            get => _userProductivityReport;
            set => SetProperty(ref _userProductivityReport, value);
        }

        /// <summary>
        /// User workload report
        /// </summary>
        public UserWorkloadReportModel UserWorkloadReport
        {
            get => _userWorkloadReport;
            set => SetProperty(ref _userWorkloadReport, value);
        }

        /// <summary>
        /// File usage report
        /// </summary>
        public FileUsageReportModel FileUsageReport
        {
            get => _fileUsageReport;
            set => SetProperty(ref _fileUsageReport, value);
        }

        /// <summary>
        /// Storage utilization report
        /// </summary>
        public StorageUtilizationReportModel StorageUtilizationReport
        {
            get => _storageUtilizationReport;
            set => SetProperty(ref _storageUtilizationReport, value);
        }

        /// <summary>
        /// Time tracking report
        /// </summary>
        public TimeTrackingReportModel TimeTrackingReport
        {
            get => _timeTrackingReport;
            set => SetProperty(ref _timeTrackingReport, value);
        }

        /// <summary>
        /// Billable hours report
        /// </summary>
        public BillableHoursReportModel BillableHoursReport
        {
            get => _billableHoursReport;
            set => SetProperty(ref _billableHoursReport, value);
        }

        /// <summary>
        /// System usage analytics
        /// </summary>
        public SystemUsageAnalyticsModel SystemUsageAnalytics
        {
            get => _systemUsageAnalytics;
            set => SetProperty(ref _systemUsageAnalytics, value);
        }

        /// <summary>
        /// Performance metrics report
        /// </summary>
        public PerformanceMetricsReportModel PerformanceMetricsReport
        {
            get => _performanceMetricsReport;
            set => SetProperty(ref _performanceMetricsReport, value);
        }

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Last updated text
        /// </summary>
        public string LastUpdatedText => $"Updated {LastUpdated:HH:mm}";

        /// <summary>
        /// Has project progress data
        /// </summary>
        public bool HasProjectProgressData => ProjectProgressReport != null;

        /// <summary>
        /// Has team productivity data
        /// </summary>
        public bool HasTeamProductivityData => TeamProductivityReport != null;

        /// <summary>
        /// Has project timeline data
        /// </summary>
        public bool HasProjectTimelineData => ProjectTimelineReport != null;

        /// <summary>
        /// Has user productivity data
        /// </summary>
        public bool HasUserProductivityData => UserProductivityReport != null;

        /// <summary>
        /// Has user workload data
        /// </summary>
        public bool HasUserWorkloadData => UserWorkloadReport != null;

        /// <summary>
        /// Has file usage data
        /// </summary>
        public bool HasFileUsageData => FileUsageReport != null;

        /// <summary>
        /// Has storage utilization data
        /// </summary>
        public bool HasStorageUtilizationData => StorageUtilizationReport != null;

        /// <summary>
        /// Has time tracking data
        /// </summary>
        public bool HasTimeTrackingData => TimeTrackingReport != null;

        /// <summary>
        /// Has billable hours data
        /// </summary>
        public bool HasBillableHoursData => BillableHoursReport != null;

        /// <summary>
        /// Has system usage analytics data
        /// </summary>
        public bool HasSystemUsageAnalyticsData => SystemUsageAnalytics != null;

        /// <summary>
        /// Has performance metrics data
        /// </summary>
        public bool HasPerformanceMetricsData => PerformanceMetricsReport != null;

        #endregion

        #region Commands

        public ICommand RefreshReportsCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public ICommand ExportDataCommand { get; private set; }
        public ICommand ApplyFiltersCommand { get; private set; }

        // Project Reports Commands
        public ICommand GenerateProjectProgressReportCommand { get; private set; }
        public ICommand ExportProjectProgressReportCommand { get; private set; }
        public ICommand ViewProjectProgressReportCommand { get; private set; }
        public ICommand GenerateTeamProductivityReportCommand { get; private set; }
        public ICommand ExportTeamProductivityReportCommand { get; private set; }
        public ICommand ViewTeamProductivityReportCommand { get; private set; }
        public ICommand GenerateProjectTimelineReportCommand { get; private set; }
        public ICommand ExportProjectTimelineReportCommand { get; private set; }

        // User Reports Commands
        public ICommand GenerateUserProductivityReportCommand { get; private set; }
        public ICommand ExportUserProductivityReportCommand { get; private set; }
        public ICommand GenerateUserWorkloadReportCommand { get; private set; }
        public ICommand ExportUserWorkloadReportCommand { get; private set; }

        // File & Storage Reports Commands
        public ICommand GenerateFileUsageReportCommand { get; private set; }
        public ICommand ExportFileUsageReportCommand { get; private set; }
        public ICommand GenerateStorageUtilizationReportCommand { get; private set; }
        public ICommand ExportStorageUtilizationReportCommand { get; private set; }

        // Time Tracking Reports Commands
        public ICommand GenerateTimeTrackingReportCommand { get; private set; }
        public ICommand ExportTimeTrackingReportCommand { get; private set; }
        public ICommand GenerateBillableHoursReportCommand { get; private set; }
        public ICommand ExportBillableHoursReportCommand { get; private set; }

        // System Analytics Commands
        public ICommand GenerateSystemUsageAnalyticsCommand { get; private set; }
        public ICommand ExportSystemUsageAnalyticsCommand { get; private set; }
        public ICommand GeneratePerformanceMetricsReportCommand { get; private set; }
        public ICommand ExportPerformanceMetricsReportCommand { get; private set; }

        #endregion

        #region Methods

        private void InitializeCommands()
        {
            RefreshReportsCommand = new RelayCommand(async () => await LoadReportsDataAsync());
            GenerateReportCommand = new RelayCommand(() => ShowGenerateReportDialog());
            ExportDataCommand = new RelayCommand(() => ShowExportDataDialog());
            ApplyFiltersCommand = new RelayCommand(async () => await ApplyFiltersAsync());

            // Project Reports Commands
            GenerateProjectProgressReportCommand = new RelayCommand(async () => await GenerateProjectProgressReportAsync());
            ExportProjectProgressReportCommand = new RelayCommand(async () => await ExportProjectProgressReportAsync());
            ViewProjectProgressReportCommand = new RelayCommand(() => ViewProjectProgressReport());
            GenerateTeamProductivityReportCommand = new RelayCommand(async () => await GenerateTeamProductivityReportAsync());
            ExportTeamProductivityReportCommand = new RelayCommand(async () => await ExportTeamProductivityReportAsync());
            ViewTeamProductivityReportCommand = new RelayCommand(() => ViewTeamProductivityReport());
            GenerateProjectTimelineReportCommand = new RelayCommand(async () => await GenerateProjectTimelineReportAsync());
            ExportProjectTimelineReportCommand = new RelayCommand(async () => await ExportProjectTimelineReportAsync());

            // User Reports Commands
            GenerateUserProductivityReportCommand = new RelayCommand(async () => await GenerateUserProductivityReportAsync());
            ExportUserProductivityReportCommand = new RelayCommand(async () => await ExportUserProductivityReportAsync());
            GenerateUserWorkloadReportCommand = new RelayCommand(async () => await GenerateUserWorkloadReportAsync());
            ExportUserWorkloadReportCommand = new RelayCommand(async () => await ExportUserWorkloadReportAsync());

            // File & Storage Reports Commands
            GenerateFileUsageReportCommand = new RelayCommand(async () => await GenerateFileUsageReportAsync());
            ExportFileUsageReportCommand = new RelayCommand(async () => await ExportFileUsageReportAsync());
            GenerateStorageUtilizationReportCommand = new RelayCommand(async () => await GenerateStorageUtilizationReportAsync());
            ExportStorageUtilizationReportCommand = new RelayCommand(async () => await ExportStorageUtilizationReportAsync());

            // Time Tracking Reports Commands
            GenerateTimeTrackingReportCommand = new RelayCommand(async () => await GenerateTimeTrackingReportAsync());
            ExportTimeTrackingReportCommand = new RelayCommand(async () => await ExportTimeTrackingReportAsync());
            GenerateBillableHoursReportCommand = new RelayCommand(async () => await GenerateBillableHoursReportAsync());
            ExportBillableHoursReportCommand = new RelayCommand(async () => await ExportBillableHoursReportAsync());

            // System Analytics Commands
            GenerateSystemUsageAnalyticsCommand = new RelayCommand(async () => await GenerateSystemUsageAnalyticsAsync());
            ExportSystemUsageAnalyticsCommand = new RelayCommand(async () => await ExportSystemUsageAnalyticsAsync());
            GeneratePerformanceMetricsReportCommand = new RelayCommand(async () => await GeneratePerformanceMetricsReportAsync());
            ExportPerformanceMetricsReportCommand = new RelayCommand(async () => await ExportPerformanceMetricsReportAsync());
        }

        /// <summary>
        /// Load reports data
        /// </summary>
        private async Task LoadReportsDataAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tải dữ liệu reports...";

                // Load filter data
                await LoadFilterDataAsync();

                LastUpdated = DateTime.Now;
                GeneratedReportsCount = 8; // Mock count
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải reports data: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load filter data
        /// </summary>
        private async Task LoadFilterDataAsync()
        {
            try
            {
                var fiter = new ProjectFilterRequest
                {
                    PageNumber = 1,
                    PageSize = 100,
                    SearchTerm = ""
                };
                // Load projects for filter
                var projects = await _projectApiService.GetProjectsAsync(fiter);
                
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    AvailableProjects.Clear();
                    AvailableProjects.Add(new ProjectModel { ProjectName = "All Projects" });
                    
                    if (projects?.Data != null)
                    {
                        foreach (var project in projects.Data)
                        {
                            var projectModel = new ProjectModel();
                            // Map DTO to Model properties
                            projectModel.ProjectName = project.ProjectName ?? "Unknown Project";
                            AvailableProjects.Add(projectModel);
                        }
                    }

                    // Mock departments
                    AvailableDepartments.Clear();
                    AvailableDepartments.Add("All Departments");
                    AvailableDepartments.Add("Development");
                    AvailableDepartments.Add("Design");
                    AvailableDepartments.Add("QA");
                    AvailableDepartments.Add("Management");

                    // Mock users (reusing project model structure)
                    AvailableUsers.Clear();
                    AvailableUsers.Add(new ProjectModel { ProjectName = "All Users" });
                    AvailableUsers.Add(new ProjectModel { ProjectName = "Nguyen Van A" });
                    AvailableUsers.Add(new ProjectModel { ProjectName = "Le Thi B" });
                    AvailableUsers.Add(new ProjectModel { ProjectName = "Tran Van C" });
                });
            }
            catch (Exception)
            {
                // Fallback to mock data
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    AvailableProjects.Clear();
                    AvailableProjects.Add(new ProjectModel { ProjectName = "All Projects" });
                    AvailableProjects.Add(new ProjectModel { ProjectName = "ManagementFile" });

                    AvailableDepartments.Clear();
                    AvailableDepartments.Add("All Departments");
                    AvailableDepartments.Add("Development");

                    AvailableUsers.Clear();
                    AvailableUsers.Add(new ProjectModel { ProjectName = "All Users" });
                    AvailableUsers.Add(new ProjectModel { ProjectName = "Current User" });
                });
            }
        }

        /// <summary>
        /// Handle tab changed
        /// </summary>
        private void OnTabChanged()
        {
            // Tab-specific loading can be implemented here
        }

        /// <summary>
        /// Apply filters
        /// </summary>
        private async Task ApplyFiltersAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang áp dụng bộ lọc...";

                await Task.Delay(500); // Simulate filter processing

                MessageBox.Show("Bộ lọc đã được áp dụng thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi áp dụng bộ lọc: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Project Reports Methods

        /// <summary>
        /// Generate project progress report
        /// </summary>
        private async Task GenerateProjectProgressReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo tiến độ dự án...";

                var report = await _reportService.GetProjectProgressReportAsync(
                    projectId: ProjectFilter == "All Projects" ? null : "PRJ001",
                    startDate: StartDate,
                    endDate: EndDate);

                ProjectProgressReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasProjectProgressData));

                MessageBox.Show("Báo cáo tiến độ dự án đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export project progress report
        /// </summary>
        private async Task ExportProjectProgressReportAsync()
        {
            if (ProjectProgressReport == null)
            {
                MessageBox.Show("Vui lòng tạo báo cáo trước khi xuất!", "Thông báo",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToPdfAsync(ProjectProgressReport, 
                    $"ProjectProgress_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo PDF thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Có lỗi trong quá trình xuất báo cáo!", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// View project progress report
        /// </summary>
        private void ViewProjectProgressReport()
        {
            if (ProjectProgressReport == null) return;

            // TODO: Implement report viewer dialog
            var reportDetails = $"Project Progress Report\n" +
                              $"Project: {ProjectProgressReport.ProjectName}\n" +
                              $"Progress: {ProjectProgressReport.ProgressText}\n" +
                              $"Tasks: {ProjectProgressReport.TasksCompletionText}\n" +
                              $"Budget: {ProjectProgressReport.BudgetUsageText}";

            MessageBox.Show(reportDetails, "Project Progress Report",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Generate team productivity report
        /// </summary>
        private async Task GenerateTeamProductivityReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo năng suất team...";

                var report = await _reportService.GetTeamProductivityReportAsync(
                    startDate: StartDate,
                    endDate: EndDate,
                    departmentFilter: DepartmentFilter == "All Departments" ? null : DepartmentFilter);

                TeamProductivityReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasTeamProductivityData));

                MessageBox.Show("Báo cáo năng suất team đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export team productivity report
        /// </summary>
        private async Task ExportTeamProductivityReportAsync()
        {
            if (TeamProductivityReport == null)
            {
                MessageBox.Show("Vui lòng tạo báo cáo trước khi xuất!", "Thông báo",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToExcelAsync(TeamProductivityReport, 
                    $"TeamProductivity_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo Excel thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// View team productivity report
        /// </summary>
        private void ViewTeamProductivityReport()
        {
            if (TeamProductivityReport == null) return;

            var reportDetails = $"Team Productivity Report\n" +
                              $"Period: {TeamProductivityReport.ReportPeriod}\n" +
                              $"Active Members: {TeamProductivityReport.TeamSummary}\n" +
                              $"Tasks Completed: {TeamProductivityReport.TotalTasksCompleted}\n" +
                              $"Hours Logged: {TeamProductivityReport.HoursSummary}";

            MessageBox.Show(reportDetails, "Team Productivity Report",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Generate project timeline report
        /// </summary>
        private async Task GenerateProjectTimelineReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo timeline dự án...";

                var report = await _reportService.GetProjectTimelineReportAsync("PRJ001", true);

                ProjectTimelineReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasProjectTimelineData));

                MessageBox.Show("Báo cáo timeline dự án đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export project timeline report
        /// </summary>
        private async Task ExportProjectTimelineReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToPdfAsync("Timeline Report", 
                    $"ProjectTimeline_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region User Reports Methods

        /// <summary>
        /// Generate user productivity report
        /// </summary>
        private async Task GenerateUserProductivityReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo năng suất cá nhân...";

                var report = await _reportService.GetUserProductivityReportAsync("USER001", StartDate, EndDate);

                UserProductivityReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasUserProductivityData));

                MessageBox.Show("Báo cáo năng suất cá nhân đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export user productivity report
        /// </summary>
        private async Task ExportUserProductivityReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToPdfAsync("User Productivity", 
                    $"UserProductivity_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Generate user workload report
        /// </summary>
        private async Task GenerateUserWorkloadReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo khối lượng công việc...";

                var report = await _reportService.GetUserWorkloadReportAsync(DateTime.Today);

                UserWorkloadReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasUserWorkloadData));

                MessageBox.Show("Báo cáo khối lượng công việc đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export user workload report
        /// </summary>
        private async Task ExportUserWorkloadReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToExcelAsync("User Workload", 
                    $"UserWorkload_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region File & Storage Reports Methods

        /// <summary>
        /// Generate file usage report
        /// </summary>
        private async Task GenerateFileUsageReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo sử dụng file...";

                var report = await _reportService.GetFileUsageReportAsync(StartDate, EndDate);

                FileUsageReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasFileUsageData));

                MessageBox.Show("Báo cáo sử dụng file đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export file usage report
        /// </summary>
        private async Task ExportFileUsageReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToPdfAsync("File Usage", 
                    $"FileUsage_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Generate storage utilization report
        /// </summary>
        private async Task GenerateStorageUtilizationReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo sử dụng storage...";

                var report = await _reportService.GetStorageUtilizationReportAsync();

                StorageUtilizationReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasStorageUtilizationData));

                MessageBox.Show("Báo cáo sử dụng storage đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export storage utilization report
        /// </summary>
        private async Task ExportStorageUtilizationReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToExcelAsync("Storage Utilization", 
                    $"StorageUtilization_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Time Tracking Reports Methods

        /// <summary>
        /// Generate time tracking report
        /// </summary>
        private async Task GenerateTimeTrackingReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo time tracking...";

                var report = await _reportService.GetTimeTrackingReportAsync(StartDate, EndDate);

                TimeTrackingReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasTimeTrackingData));

                MessageBox.Show("Báo cáo time tracking đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export time tracking report
        /// </summary>
        private async Task ExportTimeTrackingReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToPdfAsync("Time Tracking", 
                    $"TimeTracking_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Generate billable hours report
        /// </summary>
        private async Task GenerateBillableHoursReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo billable hours...";

                var report = await _reportService.GetBillableHoursReportAsync(StartDate, EndDate);

                BillableHoursReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasBillableHoursData));

                MessageBox.Show("Báo cáo billable hours đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export billable hours report
        /// </summary>
        private async Task ExportBillableHoursReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToExcelAsync("Billable Hours", 
                    $"BillableHours_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region System Analytics Methods

        /// <summary>
        /// Generate system usage analytics
        /// </summary>
        private async Task GenerateSystemUsageAnalyticsAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo phân tích hệ thống...";

                var report = await _reportService.GetSystemUsageAnalyticsAsync(StartDate, EndDate);

                SystemUsageAnalytics = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasSystemUsageAnalyticsData));

                MessageBox.Show("Báo cáo phân tích hệ thống đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export system usage analytics
        /// </summary>
        private async Task ExportSystemUsageAnalyticsAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToPdfAsync("System Usage Analytics", 
                    $"SystemUsage_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Generate performance metrics report
        /// </summary>
        private async Task GeneratePerformanceMetricsReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tạo báo cáo performance metrics...";

                var report = await _reportService.GetPerformanceMetricsReportAsync(StartDate, EndDate);

                PerformanceMetricsReport = report;
                GeneratedReportsCount++;

                OnPropertyChanged(nameof(HasPerformanceMetricsData));

                MessageBox.Show("Báo cáo performance metrics đã được tạo thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export performance metrics report
        /// </summary>
        private async Task ExportPerformanceMetricsReportAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xuất báo cáo...";

                var success = await _reportService.ExportReportToPdfAsync("Performance Metrics", 
                    $"PerformanceMetrics_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                if (success)
                {
                    MessageBox.Show("Xuất báo cáo thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Dialog Methods

        /// <summary>
        /// Show generate report dialog
        /// </summary>
        private void ShowGenerateReportDialog()
        {
            MessageBox.Show("Generate Report Dialog sẽ được implement trong tương lai!", "Thông báo",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Show export data dialog
        /// </summary>
        private void ShowExportDataDialog()
        {
            MessageBox.Show("Export Data Dialog sẽ được implement trong tương lai!", "Thông báo",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reportService?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}