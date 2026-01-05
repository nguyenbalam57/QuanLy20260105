using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ManagementFile.App.Models;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service cho Reporting & Analytics system
    /// Phase 4 - Reporting & Analytics Implementation
    /// </summary>
    public sealed class ReportService
    {
        #region DI

        public ReportService() { }

        #endregion

        #region Project Reports

        /// <summary>
        /// Get comprehensive project progress report
        /// </summary>
        public async Task<ProjectProgressReportModel> GetProjectProgressReportAsync(string projectId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // TODO: Replace with actual API call
                // var report = await _apiService.GetAsync<ProjectProgressReportModel>($"api/reports/project-progress?projectId={projectId}&start={startDate}&end={endDate}");

                await Task.Delay(500); // Simulate API call

                return CreateMockProjectProgressReport();
            }
            catch (Exception)
            {
                // Fallback to mock data
                return CreateMockProjectProgressReport();
            }
        }

        /// <summary>
        /// Get team productivity report
        /// </summary>
        public async Task<TeamProductivityReportModel> GetTeamProductivityReportAsync(DateTime startDate, DateTime endDate, string departmentFilter = null)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(500);

                return CreateMockTeamProductivityReport();
            }
            catch (Exception)
            {
                return CreateMockTeamProductivityReport();
            }
        }

        /// <summary>
        /// Get project timeline analysis
        /// </summary>
        public async Task<ProjectTimelineReportModel> GetProjectTimelineReportAsync(string projectId, bool includeDelayed = true)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(300);

                return CreateMockProjectTimelineReport();
            }
            catch (Exception)
            {
                return CreateMockProjectTimelineReport();
            }
        }

        #endregion

        #region User Reports

        /// <summary>
        /// Get user productivity report
        /// </summary>
        public async Task<UserProductivityReportModel> GetUserProductivityReportAsync(string userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(400);

                return CreateMockUserProductivityReport();
            }
            catch (Exception)
            {
                return CreateMockUserProductivityReport();
            }
        }

        /// <summary>
        /// Get user workload analysis
        /// </summary>
        public async Task<UserWorkloadReportModel> GetUserWorkloadReportAsync(DateTime? targetDate = null)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(350);

                return CreateMockUserWorkloadReport();
            }
            catch (Exception)
            {
                return CreateMockUserWorkloadReport();
            }
        }

        #endregion

        #region File & Storage Reports

        /// <summary>
        /// Get file usage statistics
        /// </summary>
        public async Task<FileUsageReportModel> GetFileUsageReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(450);

                return CreateMockFileUsageReport();
            }
            catch (Exception)
            {
                return CreateMockFileUsageReport();
            }
        }

        /// <summary>
        /// Get storage utilization report
        /// </summary>
        public async Task<StorageUtilizationReportModel> GetStorageUtilizationReportAsync()
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(300);

                return CreateMockStorageUtilizationReport();
            }
            catch (Exception)
            {
                return CreateMockStorageUtilizationReport();
            }
        }

        #endregion

        #region Time Tracking Reports

        /// <summary>
        /// Get time tracking summary report
        /// </summary>
        public async Task<TimeTrackingReportModel> GetTimeTrackingReportAsync(DateTime startDate, DateTime endDate, string projectFilter = null)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(400);

                return CreateMockTimeTrackingReport();
            }
            catch (Exception)
            {
                return CreateMockTimeTrackingReport();
            }
        }

        /// <summary>
        /// Get billable hours report
        /// </summary>
        public async Task<BillableHoursReportModel> GetBillableHoursReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(350);

                return CreateMockBillableHoursReport();
            }
            catch (Exception)
            {
                return CreateMockBillableHoursReport();
            }
        }

        #endregion

        #region System Analytics

        /// <summary>
        /// Get system usage analytics
        /// </summary>
        public async Task<SystemUsageAnalyticsModel> GetSystemUsageAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(500);

                return CreateMockSystemUsageAnalytics();
            }
            catch (Exception)
            {
                return CreateMockSystemUsageAnalytics();
            }
        }

        /// <summary>
        /// Get performance metrics report
        /// </summary>
        public async Task<PerformanceMetricsReportModel> GetPerformanceMetricsReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // TODO: Replace with actual API call
                await Task.Delay(300);

                return CreateMockPerformanceMetricsReport();
            }
            catch (Exception)
            {
                return CreateMockPerformanceMetricsReport();
            }
        }

        #endregion

        #region Export Functions

        /// <summary>
        /// Export report to PDF
        /// </summary>
        public async Task<bool> ExportReportToPdfAsync<T>(T reportData, string fileName, string templateType = "standard")
        {
            try
            {
                // TODO: Implement PDF export functionality
                await Task.Delay(1000); // Simulate export time

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Export report to Excel
        /// </summary>
        public async Task<bool> ExportReportToExcelAsync<T>(T reportData, string fileName)
        {
            try
            {
                // TODO: Implement Excel export functionality
                await Task.Delay(800); // Simulate export time

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Export report to CSV
        /// </summary>
        public async Task<bool> ExportReportToCsvAsync<T>(IEnumerable<T> reportData, string fileName)
        {
            try
            {
                // TODO: Implement CSV export functionality
                await Task.Delay(500); // Simulate export time

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Mock Data Generation

        /// <summary>
        /// Create mock project progress report
        /// </summary>
        private ProjectProgressReportModel CreateMockProjectProgressReport()
        {
            var random = new Random();
            
            return new ProjectProgressReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "Project Progress Analysis",
                GeneratedAt = DateTime.Now,
                ProjectId = "PRJ001",
                ProjectName = "ManagementFile Development",
                OverallProgress = 78.5m,
                CompletedTasks = 42,
                TotalTasks = 54,
                OverdueTasks = 3,
                TotalHoursLogged = 324.5m,
                EstimatedHoursRemaining = 89.2m,
                BudgetUsed = 15750.0m,
                TotalBudget = 25000.0m,
                ProjectStartDate = DateTime.Now.AddDays(-90),
                ProjectEndDate = DateTime.Now.AddDays(30),
                
                TaskCompletionTrend = Enumerable.Range(0, 30)
                    .Select(i => new DataPointModel
                    {
                        Date = DateTime.Now.AddDays(-29 + i),
                        Value = Math.Max(0, 45 + random.Next(-3, 8))
                    }).ToList(),

                TeamMembers = new List<TeamMemberProgressModel>
                {
                    new TeamMemberProgressModel { MemberName = "Nguyen Van A", TasksCompleted = 12, TasksInProgress = 3, HoursLogged = 89.5m, ProductivityScore = 92 },
                    new TeamMemberProgressModel { MemberName = "Le Thi B", TasksCompleted = 15, TasksInProgress = 2, HoursLogged = 95.2m, ProductivityScore = 88 },
                    new TeamMemberProgressModel { MemberName = "Tran Van C", TasksCompleted = 8, TasksInProgress = 4, HoursLogged = 67.8m, ProductivityScore = 76 },
                    new TeamMemberProgressModel { MemberName = "Pham Thi D", TasksCompleted = 7, TasksInProgress = 1, HoursLogged = 72.0m, ProductivityScore = 84 }
                },

                MilestoneStatus = new List<MilestoneStatusModel>
                {
                    new MilestoneStatusModel { MilestoneName = "Phase 1 - Admin Core", Status = "Completed", CompletionDate = DateTime.Now.AddDays(-60), PlannedDate = DateTime.Now.AddDays(-65) },
                    new MilestoneStatusModel { MilestoneName = "Phase 2 - Project Management", Status = "Completed", CompletionDate = DateTime.Now.AddDays(-30), PlannedDate = DateTime.Now.AddDays(-35) },
                    new MilestoneStatusModel { MilestoneName = "Phase 3 - Client Interface", Status = "In Progress", CompletionDate = null, PlannedDate = DateTime.Now.AddDays(-5) },
                    new MilestoneStatusModel { MilestoneName = "Phase 4 - Reports & Analytics", Status = "Planned", CompletionDate = null, PlannedDate = DateTime.Now.AddDays(15) }
                }
            };
        }

        /// <summary>
        /// Create mock team productivity report
        /// </summary>
        private TeamProductivityReportModel CreateMockTeamProductivityReport()
        {
            var random = new Random();

            return new TeamProductivityReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "Team Productivity Analysis",
                GeneratedAt = DateTime.Now,
                ReportPeriod = "Last 30 Days",
                TotalTeamMembers = 12,
                ActiveMembers = 10,
                TotalTasksCompleted = 156,
                TotalHoursLogged = 890.5m,
                AverageProductivityScore = 84.2m,

                DepartmentProductivity = new List<DepartmentProductivityModel>
                {
                    new DepartmentProductivityModel { DepartmentName = "Development", MemberCount = 6, TasksCompleted = 89, HoursLogged = 445.2m, ProductivityScore = 87.5m },
                    new DepartmentProductivityModel { DepartmentName = "Design", MemberCount = 3, TasksCompleted = 34, HoursLogged = 198.7m, ProductivityScore = 82.3m },
                    new DepartmentProductivityModel { DepartmentName = "QA", MemberCount = 2, TasksCompleted = 23, HoursLogged = 156.8m, ProductivityScore = 79.1m },
                    new DepartmentProductivityModel { DepartmentName = "Management", MemberCount = 1, TasksCompleted = 10, HoursLogged = 89.8m, ProductivityScore = 88.9m }
                },

                TopPerformers = new List<TopPerformerModel>
                {
                    new TopPerformerModel { UserName = "Nguyen Van A", TasksCompleted = 18, HoursLogged = 95.2m, ProductivityScore = 94.5m },
                    new TopPerformerModel { UserName = "Le Thi B", TasksCompleted = 16, HoursLogged = 88.7m, ProductivityScore = 92.1m },
                    new TopPerformerModel { UserName = "Tran Van C", TasksCompleted = 15, HoursLogged = 91.3m, ProductivityScore = 89.8m }
                },

                ProductivityTrend = Enumerable.Range(0, 30)
                    .Select(i => new DataPointModel
                    {
                        Date = DateTime.Now.AddDays(-29 + i),
                        Value = Math.Max(70, 85 + random.Next(-8, 10))
                    }).ToList()
            };
        }

        /// <summary>
        /// Create mock project timeline report
        /// </summary>
        private ProjectTimelineReportModel CreateMockProjectTimelineReport()
        {
            return new ProjectTimelineReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "Project Timeline Analysis",
                GeneratedAt = DateTime.Now,
                ProjectName = "ManagementFile Development",
                PlannedStartDate = DateTime.Now.AddDays(-100),
                ActualStartDate = DateTime.Now.AddDays(-95),
                PlannedEndDate = DateTime.Now.AddDays(25),
                EstimatedEndDate = DateTime.Now.AddDays(35),
                TimelineVariance = 10,

                PhaseTimeline = new List<PhaseTimelineModel>
                {
                    new PhaseTimelineModel { PhaseName = "Phase 1 - Admin Core", PlannedDuration = 21, ActualDuration = 18, Status = "Completed", DelayDays = -3 },
                    new PhaseTimelineModel { PhaseName = "Phase 2 - Project Management", PlannedDuration = 28, ActualDuration = 25, Status = "Completed", DelayDays = -3 },
                    new PhaseTimelineModel { PhaseName = "Phase 3 - Client Interface", PlannedDuration = 21, ActualDuration = 24, Status = "In Progress", DelayDays = 3 },
                    new PhaseTimelineModel { PhaseName = "Phase 4 - Reports & Analytics", PlannedDuration = 14, ActualDuration = 0, Status = "Not Started", DelayDays = 0 }
                },

                CriticalPath = new List<CriticalPathTaskModel>
                {
                    new CriticalPathTaskModel { TaskName = "Database Design", Duration = 5, StartDate = DateTime.Now.AddDays(-90), EndDate = DateTime.Now.AddDays(-85), IsCritical = true },
                    new CriticalPathTaskModel { TaskName = "API Development", Duration = 12, StartDate = DateTime.Now.AddDays(-85), EndDate = DateTime.Now.AddDays(-73), IsCritical = true },
                    new CriticalPathTaskModel { TaskName = "UI Implementation", Duration = 18, StartDate = DateTime.Now.AddDays(-73), EndDate = DateTime.Now.AddDays(-55), IsCritical = true }
                }
            };
        }

        /// <summary>
        /// Create mock user productivity report
        /// </summary>
        private UserProductivityReportModel CreateMockUserProductivityReport()
        {
            var random = new Random();

            return new UserProductivityReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "Personal Productivity Report",
                GeneratedAt = DateTime.Now,
                UserId = "USER001",
                UserName = "Nguyen Van A",
                ReportPeriod = "Last 30 Days",
                TasksCompleted = 18,
                TasksInProgress = 3,
                TasksOverdue = 1,
                TotalHoursLogged = 95.2m,
                BillableHours = 89.8m,
                ProductivityScore = 88.5m,

                DailyProductivity = Enumerable.Range(0, 30)
                    .Select(i => new DailyProductivityModel
                    {
                        Date = DateTime.Now.AddDays(-29 + i),
                        HoursLogged = Math.Max(0, 6.5m + (decimal)random.NextDouble() * 4 - 2),
                        TasksCompleted = random.Next(0, 4),
                        ProductivityScore = Math.Max(0, 85 + random.Next(-15, 20))
                    }).ToList(),

                ProjectContribution = new List<ProjectContributionModel>
                {
                    new ProjectContributionModel { ProjectName = "ManagementFile", HoursLogged = 65.2m, TasksCompleted = 12, Percentage = 68.5m },
                    new ProjectContributionModel { ProjectName = "Mobile App", HoursLogged = 20.5m, TasksCompleted = 4, Percentage = 21.5m },
                    new ProjectContributionModel { ProjectName = "Website Redesign", HoursLogged = 9.5m, TasksCompleted = 2, Percentage = 10.0m }
                },

                SkillsUtilization = new List<SkillUtilizationModel>
                {
                    new SkillUtilizationModel { SkillName = "C# Development", HoursSpent = 45.2m, Proficiency = 92, UtilizationRate = 75.3m },
                    new SkillUtilizationModel { SkillName = "WPF/XAML", HoursSpent = 28.7m, Proficiency = 85, UtilizationRate = 65.8m },
                    new SkillUtilizationModel { SkillName = "Database Design", HoursSpent = 15.3m, Proficiency = 78, UtilizationRate = 45.2m }
                }
            };
        }

        /// <summary>
        /// Create mock user workload report
        /// </summary>
        private UserWorkloadReportModel CreateMockUserWorkloadReport()
        {
            return new UserWorkloadReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "Team Workload Analysis",
                GeneratedAt = DateTime.Now,
                AnalysisDate = DateTime.Today,
                
                UserWorkloads = new List<UserWorkloadAnalysisModel>
                {
                    new UserWorkloadAnalysisModel { UserName = "Nguyen Van A", ActiveTasks = 5, TotalHours = 40.0m, Utilization = 100.0m, WorkloadStatus = "Optimal" },
                    new UserWorkloadAnalysisModel { UserName = "Le Thi B", ActiveTasks = 7, TotalHours = 45.5m, Utilization = 113.8m, WorkloadStatus = "Overloaded" },
                    new UserWorkloadAnalysisModel { UserName = "Tran Van C", ActiveTasks = 3, TotalHours = 28.5m, Utilization = 71.3m, WorkloadStatus = "Under-utilized" },
                    new UserWorkloadAnalysisModel { UserName = "Pham Thi D", ActiveTasks = 4, TotalHours = 38.2m, Utilization = 95.5m, WorkloadStatus = "Optimal" }
                },

                WorkloadDistribution = new WorkloadDistributionModel
                {
                    OptimalWorkload = 2,
                    Overloaded = 1,
                    Underutilized = 1,
                    AverageUtilization = 95.2m
                }
            };
        }

        /// <summary>
        /// Create mock file usage report
        /// </summary>
        private FileUsageReportModel CreateMockFileUsageReport()
        {
            var random = new Random();

            return new FileUsageReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "File Usage Statistics",
                GeneratedAt = DateTime.Now,
                TotalFiles = 1547,
                TotalSize = 2.45m, // GB
                FilesCreated = 89,
                FilesModified = 234,
                FilesAccessed = 1205,

                FileTypeDistribution = new List<FileTypeUsageModel>
                {
                    new FileTypeUsageModel { FileType = "Document", Count = 456, Size = 0.89m, Percentage = 29.5m },
                    new FileTypeUsageModel { FileType = "Image", Count = 623, Size = 1.23m, Percentage = 40.3m },
                    new FileTypeUsageModel { FileType = "Video", Count = 78, Size = 0.89m, Percentage = 5.0m },
                    new FileTypeUsageModel { FileType = "Code", Count = 234, Size = 0.12m, Percentage = 15.1m },
                    new FileTypeUsageModel { FileType = "Other", Count = 156, Size = 0.32m, Percentage = 10.1m }
                },

                TopAccessedFiles = new List<FileAccessModel>
                {
                    new FileAccessModel { FileName = "Project Specification.docx", AccessCount = 89, LastAccessed = DateTime.Now.AddHours(-2) },
                    new FileAccessModel { FileName = "Database Schema.png", AccessCount = 67, LastAccessed = DateTime.Now.AddHours(-4) },
                    new FileAccessModel { FileName = "API Documentation.pdf", AccessCount = 54, LastAccessed = DateTime.Now.AddHours(-1) }
                },

                StorageTrend = Enumerable.Range(0, 30)
                    .Select(i => new DataPointModel
                    {
                        Date = DateTime.Now.AddDays(-29 + i),
                        Value = Math.Max(2.0m, 2.45m + (decimal)random.NextDouble() * 0.5m - 0.25m)
                    }).ToList()
            };
        }

        /// <summary>
        /// Create mock storage utilization report
        /// </summary>
        private StorageUtilizationReportModel CreateMockStorageUtilizationReport()
        {
            return new StorageUtilizationReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "Storage Utilization Analysis",
                GeneratedAt = DateTime.Now,
                TotalCapacity = 100.0m, // GB
                UsedSpace = 45.7m, // GB
                AvailableSpace = 54.3m, // GB
                UtilizationPercentage = 45.7m,

                ProjectStorageUsage = new List<ProjectStorageModel>
                {
                    new ProjectStorageModel { ProjectName = "ManagementFile", UsedSpace = 15.2m, FileCount = 456, Percentage = 33.3m },
                    new ProjectStorageModel { ProjectName = "Mobile App", UsedSpace = 12.8m, FileCount = 324, Percentage = 28.0m },
                    new ProjectStorageModel { ProjectName = "Website", UsedSpace = 8.9m, FileCount = 234, Percentage = 19.5m },
                    new ProjectStorageModel { ProjectName = "Documentation", UsedSpace = 5.3m, FileCount = 189, Percentage = 11.6m },
                    new ProjectStorageModel { ProjectName = "Archive", UsedSpace = 3.5m, FileCount = 145, Percentage = 7.6m }
                },

                StorageGrowthPrediction = new List<StoragePredictionModel>
                {
                    new StoragePredictionModel { Month = "Current", PredictedUsage = 45.7m },
                    new StoragePredictionModel { Month = "Month +1", PredictedUsage = 48.2m },
                    new StoragePredictionModel { Month = "Month +2", PredictedUsage = 51.1m },
                    new StoragePredictionModel { Month = "Month +3", PredictedUsage = 54.5m },
                    new StoragePredictionModel { Month = "Month +6", PredictedUsage = 63.8m }
                }
            };
        }

        /// <summary>
        /// Create mock time tracking report
        /// </summary>
        private TimeTrackingReportModel CreateMockTimeTrackingReport()
        {
            var random = new Random();

            return new TimeTrackingReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "Time Tracking Summary",
                GeneratedAt = DateTime.Now,
                TotalHours = 456.8m,
                BillableHours = 398.5m,
                NonBillableHours = 58.3m,
                BillablePercentage = 87.2m,

                ProjectTimeBreakdown = new List<ReportProjectTimeModel>
                {
                    new ReportProjectTimeModel { ProjectName = "ManagementFile", Hours = 245.6m, BillableHours = 220.3m, Percentage = 53.8m },
                    new ReportProjectTimeModel { ProjectName = "Mobile App", Hours = 123.4m, BillableHours = 105.7m, Percentage = 27.0m },
                    new ReportProjectTimeModel { ProjectName = "Website", Hours = 87.8m, BillableHours = 72.5m, Percentage = 19.2m }
                },

                UserTimeContribution = new List<UserTimeContributionModel>
                {
                    new UserTimeContributionModel { UserName = "Nguyen Van A", TotalHours = 95.2m, BillableHours = 87.4m, BillableRate = 91.8m },
                    new UserTimeContributionModel { UserName = "Le Thi B", TotalHours = 88.7m, BillableHours = 79.2m, BillableRate = 89.3m },
                    new UserTimeContributionModel { UserName = "Tran Van C", TotalHours = 91.3m, BillableHours = 84.6m, BillableRate = 92.7m }
                },

                DailyTimeLog = Enumerable.Range(0, 30)
                    .Select(i => new DailyTimeLogModel
                    {
                        Date = DateTime.Now.AddDays(-29 + i),
                        TotalHours = Math.Max(0, 15.0m + (decimal)random.NextDouble() * 10 - 5),
                        BillableHours = Math.Max(0, 13.0m + (decimal)random.NextDouble() * 8 - 4)
                    }).ToList()
            };
        }

        /// <summary>
        /// Create mock billable hours report
        /// </summary>
        private BillableHoursReportModel CreateMockBillableHoursReport()
        {
            return new BillableHoursReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "Billable Hours Analysis",
                GeneratedAt = DateTime.Now,
                TotalBillableHours = 398.5m,
                TotalNonBillableHours = 58.3m,
                BillableRate = 87.2m,
                EstimatedRevenue = 39850.0m,

                ClientBillableHours = new List<ClientBillableModel>
                {
                    new ClientBillableModel { ClientName = "Client A", BillableHours = 156.7m, HourlyRate = 120.0m, Revenue = 18804.0m },
                    new ClientBillableModel { ClientName = "Client B", BillableHours = 134.2m, HourlyRate = 100.0m, Revenue = 13420.0m },
                    new ClientBillableModel { ClientName = "Internal", BillableHours = 107.6m, HourlyRate = 80.0m, Revenue = 8608.0m }
                },

                MonthlyBillableComparison = new List<MonthlyBillableModel>
                {
                    new MonthlyBillableModel { Month = "Jan", BillableHours = 142.3m, Revenue = 17076.0m },
                    new MonthlyBillableModel { Month = "Feb", BillableHours = 156.7m, Revenue = 18804.0m },
                    new MonthlyBillableModel { Month = "Mar", BillableHours = 198.5m, Revenue = 23820.0m }
                }
            };
        }

        /// <summary>
        /// Create mock system usage analytics
        /// </summary>
        private SystemUsageAnalyticsModel CreateMockSystemUsageAnalytics()
        {
            var random = new Random();

            return new SystemUsageAnalyticsModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "System Usage Analytics",
                GeneratedAt = DateTime.Now,
                TotalUsers = 48,
                ActiveUsers = 42,
                TotalSessions = 1547,
                AverageSessionDuration = 142.5m, // minutes
                TotalPageViews = 15478,

                FeatureUsageStats = new List<FeatureUsageModel>
                {
                    new FeatureUsageModel { FeatureName = "Project Management", UsageCount = 2547, UsagePercentage = 35.2m },
                    new FeatureUsageModel { FeatureName = "File Management", UsageCount = 1896, UsagePercentage = 26.3m },
                    new FeatureUsageModel { FeatureName = "Time Tracking", UsageCount = 1234, UsagePercentage = 17.1m },
                    new FeatureUsageModel { FeatureName = "Reports", UsageCount = 789, UsagePercentage = 10.9m },
                    new FeatureUsageModel { FeatureName = "User Management", UsageCount = 756, UsagePercentage = 10.5m }
                },

                UserActivityTrend = Enumerable.Range(0, 30)
                    .Select(i => new DataPointModel
                    {
                        Date = DateTime.Now.AddDays(-29 + i),
                        Value = Math.Max(0, 42 + random.Next(-8, 10))
                    }).ToList(),

                PeakUsageHours = new List<HourlyUsageModel>
                {
                    new HourlyUsageModel { Hour = 9, UserCount = 35 },
                    new HourlyUsageModel { Hour = 10, UserCount = 42 },
                    new HourlyUsageModel { Hour = 11, UserCount = 38 },
                    new HourlyUsageModel { Hour = 14, UserCount = 41 },
                    new HourlyUsageModel { Hour = 15, UserCount = 37 }
                }
            };
        }

        /// <summary>
        /// Create mock performance metrics report
        /// </summary>
        private PerformanceMetricsReportModel CreateMockPerformanceMetricsReport()
        {
            return new PerformanceMetricsReportModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportName = "System Performance Metrics",
                GeneratedAt = DateTime.Now,
                AverageResponseTime = 245.7m, // milliseconds
                SystemUptime = 99.8m, // percentage
                ErrorRate = 0.12m, // percentage
                ThroughputRequests = 15420,

                ResponseTimeBreakdown = new List<ResponseTimeModel>
                {
                    new ResponseTimeModel { Endpoint = "/api/projects", AverageResponseTime = 156.3m, RequestCount = 2547 },
                    new ResponseTimeModel { Endpoint = "/api/tasks", AverageResponseTime = 134.7m, RequestCount = 3456 },
                    new ResponseTimeModel { Endpoint = "/api/users", AverageResponseTime = 89.2m, RequestCount = 1234 },
                    new ResponseTimeModel { Endpoint = "/api/files", AverageResponseTime = 289.5m, RequestCount = 987 }
                },

                ErrorAnalysis = new List<ErrorAnalysisModel>
                {
                    new ErrorAnalysisModel { ErrorType = "404 Not Found", Count = 12, Percentage = 45.2m },
                    new ErrorAnalysisModel { ErrorType = "500 Server Error", Count = 8, Percentage = 30.1m },
                    new ErrorAnalysisModel { ErrorType = "403 Forbidden", Count = 4, Percentage = 15.1m },
                    new ErrorAnalysisModel { ErrorType = "400 Bad Request", Count = 3, Percentage = 9.6m }
                },

                ResourceUtilization = new ResourceUtilizationModel
                {
                    CpuUsage = 45.7m,
                    MemoryUsage = 62.3m,
                    DiskUsage = 34.8m,
                    NetworkUsage = 23.4m
                }
            };
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            // Cleanup if needed
        }

        #endregion
    }
}