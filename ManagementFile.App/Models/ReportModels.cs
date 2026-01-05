using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ManagementFile.App.Models
{
    #region Base Report Models

    /// <summary>
    /// Base report model
    /// </summary>
    public abstract class BaseReportModel : INotifyPropertyChanged
    {
        public string ReportId { get; set; } = "";
        public string ReportName { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
        public string ReportType { get; set; } = "";

        // UI Helper Properties
        public string GeneratedAtText => GeneratedAt.ToString("MMM dd, yyyy HH:mm");
        public string ReportDisplayName => $"{ReportName} ({GeneratedAtText})";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Data point model for charts
    /// </summary>
    public class DataPointModel
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public string Label { get; set; } = "";

        // UI Helper Properties
        public string DateText => Date.ToString("MMM dd");
        public string ValueText => Value.ToString("F1");
    }

    #endregion

    #region Project Progress Report Models

    /// <summary>
    /// Project progress report model
    /// </summary>
    public class ProjectProgressReportModel : BaseReportModel
    {
        public string ProjectId { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public decimal OverallProgress { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalTasks { get; set; }
        public int OverdueTasks { get; set; }
        public decimal TotalHoursLogged { get; set; }
        public decimal EstimatedHoursRemaining { get; set; }
        public decimal BudgetUsed { get; set; }
        public decimal TotalBudget { get; set; }
        public DateTime ProjectStartDate { get; set; }
        public DateTime ProjectEndDate { get; set; }

        public List<DataPointModel> TaskCompletionTrend { get; set; } = new List<DataPointModel>();
        public List<TeamMemberProgressModel> TeamMembers { get; set; } = new List<TeamMemberProgressModel>();
        public List<MilestoneStatusModel> MilestoneStatus { get; set; } = new List<MilestoneStatusModel>();

        // UI Helper Properties
        public string ProgressText => $"{OverallProgress:F1}%";
        public string TasksCompletionText => $"{CompletedTasks}/{TotalTasks}";
        public string BudgetUsageText => $"${BudgetUsed:N0} / ${TotalBudget:N0}";
        public string HoursText => $"{TotalHoursLogged:F1}h logged, {EstimatedHoursRemaining:F1}h remaining";
        
        public Brush ProgressColor
        {
            get
            {
                if (OverallProgress >= 90) return new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Green
                if (OverallProgress >= 70) return new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Blue
                if (OverallProgress >= 50) return new SolidColorBrush(Color.FromRgb(243, 156, 18)); // Orange
                return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
            }
        }

        public string OverdueWarning => OverdueTasks > 0 ? $"⚠️ {OverdueTasks} overdue tasks" : "✅ No overdue tasks";
        public Brush OverdueColor => OverdueTasks > 0 ? new SolidColorBrush(Color.FromRgb(231, 76, 60)) : new SolidColorBrush(Color.FromRgb(39, 174, 96));
    }

    /// <summary>
    /// Team member progress model
    /// </summary>
    public class TeamMemberProgressModel
    {
        public string MemberName { get; set; } = "";
        public int TasksCompleted { get; set; }
        public int TasksInProgress { get; set; }
        public decimal HoursLogged { get; set; }
        public decimal ProductivityScore { get; set; }

        // UI Helper Properties
        public string TasksSummary => $"{TasksCompleted} completed, {TasksInProgress} in progress";
        public string HoursText => $"{HoursLogged:F1}h";
        public string ProductivityText => $"{ProductivityScore:F0}%";
        
        public Brush ProductivityColor
        {
            get
            {
                if (ProductivityScore >= 90) return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                if (ProductivityScore >= 80) return new SolidColorBrush(Color.FromRgb(52, 152, 219));
                if (ProductivityScore >= 70) return new SolidColorBrush(Color.FromRgb(243, 156, 18));
                return new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
        }
    }

    /// <summary>
    /// Milestone status model
    /// </summary>
    public class MilestoneStatusModel
    {
        public string MilestoneName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? CompletionDate { get; set; }
        public DateTime PlannedDate { get; set; }

        // UI Helper Properties
        public string StatusIcon
        {
            get
            {
                switch (Status?.ToLower())
                {
                    case "completed": return "✅";
                    case "in progress": return "🔄";
                    case "delayed": return "⚠️";
                    case "planned": return "📅";
                    default: return "❓";
                }
            }
        }

        public Brush StatusColor
        {
            get
            {
                switch (Status?.ToLower())
                {
                    case "completed": return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                    case "in progress": return new SolidColorBrush(Color.FromRgb(52, 152, 219));
                    case "delayed": return new SolidColorBrush(Color.FromRgb(231, 76, 60));
                    case "planned": return new SolidColorBrush(Color.FromRgb(149, 165, 166));
                    default: return new SolidColorBrush(Color.FromRgb(149, 165, 166));
                }
            }
        }

        public string CompletionText => CompletionDate?.ToString("MMM dd, yyyy") ?? "Not completed";
        public string PlannedText => PlannedDate.ToString("MMM dd, yyyy");
        public int DelayDays => CompletionDate.HasValue ? (CompletionDate.Value - PlannedDate).Days : 0;
        public string DelayText => CompletionDate.HasValue 
            ? (DelayDays >= 0 ? $"+{DelayDays} days" : $"{DelayDays} days") 
            : "On schedule";
    }

    #endregion

    #region Team Productivity Report Models

    /// <summary>
    /// Team productivity report model
    /// </summary>
    public class TeamProductivityReportModel : BaseReportModel
    {
        public string ReportPeriod { get; set; } = "";
        public int TotalTeamMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalTasksCompleted { get; set; }
        public decimal TotalHoursLogged { get; set; }
        public decimal AverageProductivityScore { get; set; }

        public List<DepartmentProductivityModel> DepartmentProductivity { get; set; } = new List<DepartmentProductivityModel>();
        public List<TopPerformerModel> TopPerformers { get; set; } = new List<TopPerformerModel>();
        public List<DataPointModel> ProductivityTrend { get; set; } = new List<DataPointModel>();

        // UI Helper Properties
        public string TeamSummary => $"{ActiveMembers}/{TotalTeamMembers} active members";
        public string TasksSummary => $"{TotalTasksCompleted} tasks completed";
        public string HoursSummary => $"{TotalHoursLogged:F1} hours logged";
        public string ProductivitySummary => $"{AverageProductivityScore:F1}% avg productivity";
    }

    /// <summary>
    /// Department productivity model
    /// </summary>
    public class DepartmentProductivityModel
    {
        public string DepartmentName { get; set; } = "";
        public int MemberCount { get; set; }
        public int TasksCompleted { get; set; }
        public decimal HoursLogged { get; set; }
        public decimal ProductivityScore { get; set; }

        // UI Helper Properties
        public string MemberText => $"{MemberCount} members";
        public string TasksText => $"{TasksCompleted} tasks";
        public string HoursText => $"{HoursLogged:F1}h";
        public string ProductivityText => $"{ProductivityScore:F1}%";
    }

    /// <summary>
    /// Top performer model
    /// </summary>
    public class TopPerformerModel
    {
        public string UserName { get; set; } = "";
        public int TasksCompleted { get; set; }
        public decimal HoursLogged { get; set; }
        public decimal ProductivityScore { get; set; }

        // UI Helper Properties
        public string PerformanceSummary => $"{TasksCompleted} tasks, {HoursLogged:F1}h, {ProductivityScore:F1}%";
    }

    #endregion

    #region Project Timeline Report Models

    /// <summary>
    /// Project timeline report model
    /// </summary>
    public class ProjectTimelineReportModel : BaseReportModel
    {
        public string ProjectName { get; set; } = "";
        public DateTime PlannedStartDate { get; set; }
        public DateTime ActualStartDate { get; set; }
        public DateTime PlannedEndDate { get; set; }
        public DateTime EstimatedEndDate { get; set; }
        public int TimelineVariance { get; set; } // days

        public List<PhaseTimelineModel> PhaseTimeline { get; set; } = new List<PhaseTimelineModel>();
        public List<CriticalPathTaskModel> CriticalPath { get; set; } = new List<CriticalPathTaskModel>();

        // UI Helper Properties
        public string TimelineVarianceText => $"{(TimelineVariance >= 0 ? "+" : "")}{TimelineVariance} days";
        public Brush TimelineColor => TimelineVariance <= 0 ? 
            new SolidColorBrush(Color.FromRgb(39, 174, 96)) : 
            new SolidColorBrush(Color.FromRgb(231, 76, 60));
    }

    /// <summary>
    /// Phase timeline model
    /// </summary>
    public class PhaseTimelineModel
    {
        public string PhaseName { get; set; } = "";
        public int PlannedDuration { get; set; }
        public int ActualDuration { get; set; }
        public string Status { get; set; } = "";
        public int DelayDays { get; set; }

        // UI Helper Properties
        public string DurationText => ActualDuration > 0 ? $"{ActualDuration}/{PlannedDuration} days" : $"{PlannedDuration} days planned";
        public string DelayText => $"{(DelayDays >= 0 ? "+" : "")}{DelayDays} days";
        
        public Brush DelayColor => DelayDays <= 0 ? 
            new SolidColorBrush(Color.FromRgb(39, 174, 96)) : 
            new SolidColorBrush(Color.FromRgb(231, 76, 60));
    }

    /// <summary>
    /// Critical path task model
    /// </summary>
    public class CriticalPathTaskModel
    {
        public string TaskName { get; set; } = "";
        public int Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCritical { get; set; }

        // UI Helper Properties
        public string DurationText => $"{Duration} days";
        public string DateRangeText => $"{StartDate:MMM dd} - {EndDate:MMM dd}";
        public string CriticalIcon => IsCritical ? "🔴" : "🔵";
    }

    #endregion

    #region User Productivity Report Models

    /// <summary>
    /// User productivity report model
    /// </summary>
    public class UserProductivityReportModel : BaseReportModel
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string ReportPeriod { get; set; } = "";
        public int TasksCompleted { get; set; }
        public int TasksInProgress { get; set; }
        public int TasksOverdue { get; set; }
        public decimal TotalHoursLogged { get; set; }
        public decimal BillableHours { get; set; }
        public decimal ProductivityScore { get; set; }

        public List<DailyProductivityModel> DailyProductivity { get; set; } = new List<DailyProductivityModel>();
        public List<ProjectContributionModel> ProjectContribution { get; set; } = new List<ProjectContributionModel>();
        public List<SkillUtilizationModel> SkillsUtilization { get; set; } = new List<SkillUtilizationModel>();

        // UI Helper Properties
        public string TasksSummary => $"{TasksCompleted} completed, {TasksInProgress} in progress, {TasksOverdue} overdue";
        public string HoursSummary => $"{TotalHoursLogged:F1}h total, {BillableHours:F1}h billable";
        public decimal BillableRate => TotalHoursLogged > 0 ? (BillableHours / TotalHoursLogged * 100) : 0;
        public string BillableRateText => $"{BillableRate:F1}%";
    }

    /// <summary>
    /// Daily productivity model
    /// </summary>
    public class DailyProductivityModel
    {
        public DateTime Date { get; set; }
        public decimal HoursLogged { get; set; }
        public int TasksCompleted { get; set; }
        public decimal ProductivityScore { get; set; }

        // UI Helper Properties
        public string DateText => Date.ToString("MMM dd");
        public string HoursText => $"{HoursLogged:F1}h";
        public string ProductivityText => $"{ProductivityScore:F0}%";
    }

    /// <summary>
    /// Project contribution model
    /// </summary>
    public class ProjectContributionModel
    {
        public string ProjectName { get; set; } = "";
        public decimal HoursLogged { get; set; }
        public int TasksCompleted { get; set; }
        public decimal Percentage { get; set; }

        // UI Helper Properties
        public string ContributionSummary => $"{HoursLogged:F1}h, {TasksCompleted} tasks ({Percentage:F1}%)";
    }

    /// <summary>
    /// Skills utilization model
    /// </summary>
    public class SkillUtilizationModel
    {
        public string SkillName { get; set; } = "";
        public decimal HoursSpent { get; set; }
        public int Proficiency { get; set; } // 0-100
        public decimal UtilizationRate { get; set; } // percentage

        // UI Helper Properties
        public string HoursText => $"{HoursSpent:F1}h";
        public string ProficiencyText => $"{Proficiency}%";
        public string UtilizationText => $"{UtilizationRate:F1}%";
    }

    #endregion

    #region User Workload Report Models

    /// <summary>
    /// User workload report model
    /// </summary>
    public class UserWorkloadReportModel : BaseReportModel
    {
        public DateTime AnalysisDate { get; set; }
        public List<UserWorkloadAnalysisModel> UserWorkloads { get; set; } = new List<UserWorkloadAnalysisModel>();
        public WorkloadDistributionModel WorkloadDistribution { get; set; } = new WorkloadDistributionModel();

        // UI Helper Properties
        public string AnalysisDateText => AnalysisDate.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// User workload analysis model
    /// </summary>
    public class UserWorkloadAnalysisModel
    {
        public string UserName { get; set; } = "";
        public int ActiveTasks { get; set; }
        public decimal TotalHours { get; set; }
        public decimal Utilization { get; set; } // percentage
        public string WorkloadStatus { get; set; } = "";

        // UI Helper Properties
        public string WorkloadSummary => $"{ActiveTasks} tasks, {TotalHours:F1}h ({Utilization:F1}%)";
        
        public Brush StatusColor
        {
            get
            {
                switch (WorkloadStatus?.ToLower())
                {
                    case "optimal": return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                    case "overloaded": return new SolidColorBrush(Color.FromRgb(231, 76, 60));
                    case "under-utilized": return new SolidColorBrush(Color.FromRgb(243, 156, 18));
                    default: return new SolidColorBrush(Color.FromRgb(149, 165, 166));
                }
            }
        }
    }

    /// <summary>
    /// Workload distribution model
    /// </summary>
    public class WorkloadDistributionModel
    {
        public int OptimalWorkload { get; set; }
        public int Overloaded { get; set; }
        public int Underutilized { get; set; }
        public decimal AverageUtilization { get; set; }

        // UI Helper Properties
        public int TotalUsers => OptimalWorkload + Overloaded + Underutilized;
        public string DistributionSummary => $"{OptimalWorkload} optimal, {Overloaded} overloaded, {Underutilized} under-utilized";
        public string AverageUtilizationText => $"{AverageUtilization:F1}%";
    }

    #endregion

    #region File Usage Report Models

    /// <summary>
    /// File usage report model
    /// </summary>
    public class FileUsageReportModel : BaseReportModel
    {
        public int TotalFiles { get; set; }
        public decimal TotalSize { get; set; } // GB
        public int FilesCreated { get; set; }
        public int FilesModified { get; set; }
        public int FilesAccessed { get; set; }

        public List<FileTypeUsageModel> FileTypeDistribution { get; set; } = new List<FileTypeUsageModel>();
        public List<FileAccessModel> TopAccessedFiles { get; set; } = new List<FileAccessModel>();
        public List<DataPointModel> StorageTrend { get; set; } = new List<DataPointModel>();

        // UI Helper Properties
        public string FilesText => $"{TotalFiles} files";
        public string SizeText => $"{TotalSize:F2} GB";
        public string ActivityText => $"{FilesCreated} created, {FilesModified} modified, {FilesAccessed} accessed";
    }

    /// <summary>
    /// File type usage model
    /// </summary>
    public class FileTypeUsageModel
    {
        public string FileType { get; set; } = "";
        public int Count { get; set; }
        public decimal Size { get; set; } // GB
        public decimal Percentage { get; set; }

        // UI Helper Properties
        public string CountText => $"{Count} files";
        public string SizeText => $"{Size:F2} GB";
        public string PercentageText => $"{Percentage:F1}%";
    }

    /// <summary>
    /// File access model
    /// </summary>
    public class FileAccessModel
    {
        public string FileName { get; set; } = "";
        public int AccessCount { get; set; }
        public DateTime LastAccessed { get; set; }

        // UI Helper Properties
        public string AccessText => $"{AccessCount} accesses";
        public string LastAccessedText => LastAccessed.ToString("MMM dd, HH:mm");
    }

    #endregion

    #region Storage Utilization Report Models

    /// <summary>
    /// Storage utilization report model
    /// </summary>
    public class StorageUtilizationReportModel : BaseReportModel
    {
        public decimal TotalCapacity { get; set; } // GB
        public decimal UsedSpace { get; set; } // GB
        public decimal AvailableSpace { get; set; } // GB
        public decimal UtilizationPercentage { get; set; }

        public List<ProjectStorageModel> ProjectStorageUsage { get; set; } = new List<ProjectStorageModel>();
        public List<StoragePredictionModel> StorageGrowthPrediction { get; set; } = new List<StoragePredictionModel>();

        // UI Helper Properties
        public string CapacityText => $"{UsedSpace:F1}/{TotalCapacity:F1} GB ({UtilizationPercentage:F1}%)";
        public string AvailableText => $"{AvailableSpace:F1} GB available";
        
        public Brush UtilizationColor
        {
            get
            {
                if (UtilizationPercentage < 70) return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                if (UtilizationPercentage < 85) return new SolidColorBrush(Color.FromRgb(243, 156, 18));
                return new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
        }
    }

    /// <summary>
    /// Project storage model
    /// </summary>
    public class ProjectStorageModel
    {
        public string ProjectName { get; set; } = "";
        public decimal UsedSpace { get; set; } // GB
        public int FileCount { get; set; }
        public decimal Percentage { get; set; }

        // UI Helper Properties
        public string StorageText => $"{UsedSpace:F1} GB ({FileCount} files)";
        public string PercentageText => $"{Percentage:F1}%";
    }

    /// <summary>
    /// Storage prediction model
    /// </summary>
    public class StoragePredictionModel
    {
        public string Month { get; set; } = "";
        public decimal PredictedUsage { get; set; } // GB

        // UI Helper Properties
        public string UsageText => $"{PredictedUsage:F1} GB";
    }

    #endregion

    #region Time Tracking Report Models

    /// <summary>
    /// Daily time log model
    /// </summary>
    public class DailyTimeLogModel
    {
        public DateTime Date { get; set; }
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }

        // UI Helper Properties
        public string DateText => Date.ToString("MMM dd");
        public string HoursText => $"{TotalHours:F1}h total, {BillableHours:F1}h billable";
    }

    /// <summary>
    /// Time tracking report model
    /// </summary>
    public class TimeTrackingReportModel : BaseReportModel
    {
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }
        public decimal BillablePercentage { get; set; }

        public List<ReportProjectTimeModel> ProjectTimeBreakdown { get; set; } = new List<ReportProjectTimeModel>();
        public List<UserTimeContributionModel> UserTimeContribution { get; set; } = new List<UserTimeContributionModel>();
        public List<DailyTimeLogModel> DailyTimeLog { get; set; } = new List<DailyTimeLogModel>();

        // UI Helper Properties
        public string TotalHoursText => $"{TotalHours:F1} hours";
        public string BillableHoursText => $"{BillableHours:F1}h ({BillablePercentage:F1}%)";
        public string NonBillableHoursText => $"{NonBillableHours:F1}h";
    }

    /// <summary>
    /// User time contribution model
    /// </summary>
    public class UserTimeContributionModel
    {
        public string UserName { get; set; } = "";
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal BillableRate { get; set; } // percentage

        // UI Helper Properties
        public string TimeContributionText => $"{TotalHours:F1}h total, {BillableHours:F1}h billable ({BillableRate:F1}%)";
    }

    /// <summary>
    /// Project time model
    /// </summary>
    public class ReportProjectTimeModel
    {
        public string ProjectName { get; set; } = "";
        public decimal Hours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal Percentage { get; set; }

        // UI Helper Properties
        public string TimeText => $"{Hours:F1}h ({BillableHours:F1}h billable)";
        public string PercentageText => $"{Percentage:F1}%";
    }

    #endregion

    #region Billable Hours Report Models

    /// <summary>
    /// Billable hours report model
    /// </summary>
    public class BillableHoursReportModel : BaseReportModel
    {
        public decimal TotalBillableHours { get; set; }
        public decimal TotalNonBillableHours { get; set; }
        public decimal BillableRate { get; set; } // percentage
        public decimal EstimatedRevenue { get; set; }

        public List<ClientBillableModel> ClientBillableHours { get; set; } = new List<ClientBillableModel>();
        public List<MonthlyBillableModel> MonthlyBillableComparison { get; set; } = new List<MonthlyBillableModel>();

        // UI Helper Properties
        public string BillableHoursText => $"{TotalBillableHours:F1}h ({BillableRate:F1}%)";
        public string RevenueText => $"${EstimatedRevenue:N0}";
        public decimal TotalHours => TotalBillableHours + TotalNonBillableHours;
        public string TotalHoursText => $"{TotalHours:F1}h total";
    }

    /// <summary>
    /// Client billable model
    /// </summary>
    public class ClientBillableModel
    {
        public string ClientName { get; set; } = "";
        public decimal BillableHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Revenue { get; set; }

        // UI Helper Properties
        public string BillableText => $"{BillableHours:F1}h @ ${HourlyRate:F0}/h = ${Revenue:N0}";
    }

    /// <summary>
    /// Monthly billable model
    /// </summary>
    public class MonthlyBillableModel
    {
        public string Month { get; set; } = "";
        public decimal BillableHours { get; set; }
        public decimal Revenue { get; set; }

        // UI Helper Properties
        public string MonthlyText => $"{Month}: {BillableHours:F1}h (${Revenue:N0})";
    }

    #endregion

    #region System Usage Analytics Models

    /// <summary>
    /// System usage analytics model
    /// </summary>
    public class SystemUsageAnalyticsModel : BaseReportModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalSessions { get; set; }
        public decimal AverageSessionDuration { get; set; } // minutes
        public int TotalPageViews { get; set; }

        public List<FeatureUsageModel> FeatureUsageStats { get; set; } = new List<FeatureUsageModel>();
        public List<DataPointModel> UserActivityTrend { get; set; } = new List<DataPointModel>();
        public List<HourlyUsageModel> PeakUsageHours { get; set; } = new List<HourlyUsageModel>();

        // UI Helper Properties
        public string UsersSummary => $"{ActiveUsers}/{TotalUsers} active users";
        public string SessionsSummary => $"{TotalSessions} sessions, {AverageSessionDuration:F1} min avg";
        public string PageViewsSummary => $"{TotalPageViews} page views";
    }

    /// <summary>
    /// Feature usage model
    /// </summary>
    public class FeatureUsageModel
    {
        public string FeatureName { get; set; } = "";
        public int UsageCount { get; set; }
        public decimal UsagePercentage { get; set; }

        // UI Helper Properties
        public string UsageText => $"{UsageCount} uses ({UsagePercentage:F1}%)";
    }

    /// <summary>
    /// Hourly usage model
    /// </summary>
    public class HourlyUsageModel
    {
        public int Hour { get; set; }
        public int UserCount { get; set; }

        // UI Helper Properties
        public string HourText => $"{Hour:D2}:00";
        public string UsageText => $"{UserCount} users";
    }

    #endregion

    #region Performance Metrics Report Models

    /// <summary>
    /// Performance metrics report model
    /// </summary>
    public class PerformanceMetricsReportModel : BaseReportModel
    {
        public decimal AverageResponseTime { get; set; } // milliseconds
        public decimal SystemUptime { get; set; } // percentage
        public decimal ErrorRate { get; set; } // percentage
        public int ThroughputRequests { get; set; }

        public List<ResponseTimeModel> ResponseTimeBreakdown { get; set; } = new List<ResponseTimeModel>();
        public List<ErrorAnalysisModel> ErrorAnalysis { get; set; } = new List<ErrorAnalysisModel>();
        public ResourceUtilizationModel ResourceUtilization { get; set; } = new ResourceUtilizationModel();

        // UI Helper Properties
        public string ResponseTimeText => $"{AverageResponseTime:F1} ms avg";
        public string UptimeText => $"{SystemUptime:F2}% uptime";
        public string ErrorRateText => $"{ErrorRate:F2}% error rate";
        public string ThroughputText => $"{ThroughputRequests:N0} requests";
        
        public Brush PerformanceColor
        {
            get
            {
                if (AverageResponseTime < 200 && ErrorRate < 1) return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                if (AverageResponseTime < 500 && ErrorRate < 3) return new SolidColorBrush(Color.FromRgb(243, 156, 18));
                return new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
        }
    }

    /// <summary>
    /// Response time model
    /// </summary>
    public class ResponseTimeModel
    {
        public string Endpoint { get; set; } = "";
        public decimal AverageResponseTime { get; set; } // milliseconds
        public int RequestCount { get; set; }

        // UI Helper Properties
        public string ResponseText => $"{AverageResponseTime:F1} ms ({RequestCount:N0} requests)";
    }

    /// <summary>
    /// Error analysis model
    /// </summary>
    public class ErrorAnalysisModel
    {
        public string ErrorType { get; set; } = "";
        public int Count { get; set; }
        public decimal Percentage { get; set; }

        // UI Helper Properties
        public string ErrorText => $"{ErrorType}: {Count} ({Percentage:F1}%)";
    }

    /// <summary>
    /// Resource utilization model
    /// </summary>
    public class ResourceUtilizationModel
    {
        public decimal CpuUsage { get; set; } // percentage
        public decimal MemoryUsage { get; set; } // percentage
        public decimal DiskUsage { get; set; } // percentage
        public decimal NetworkUsage { get; set; } // percentage

        // UI Helper Properties
        public string CpuText => $"CPU: {CpuUsage:F1}%";
        public string MemoryText => $"Memory: {MemoryUsage:F1}%";
        public string DiskText => $"Disk: {DiskUsage:F1}%";
        public string NetworkText => $"Network: {NetworkUsage:F1}%";
    }

    #endregion
}