using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ManagementFile.App.Models.Projects
{
    /// <summary>
    /// ProjectTaskModel - WPF Model for ProjectTask with INotifyPropertyChanged
    /// ✅ Enhanced with Hierarchy support for parent-child task relationships
    /// </summary>
    public class ProjectTaskModel : INotifyPropertyChanged
    {
        private int _id;
        private string _taskName = "";
        private string _title = "";
        private TaskStatuss _status;
        private TaskPriority _priority;
        private decimal _progress;
        private bool _isOverdue;

        // Hierarchy fields
        private int _hierarchyLevel = 0;
        private bool _isLoadingSubTasks = false;
        private ObservableCollection<ProjectTaskModel> _subTasks = new ObservableCollection<ProjectTaskModel>();
        private bool _isExpanded = false;
        private bool _isSelected = false;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string IdText => Id > 0 ? TaskCode : "";

        public int ProjectId { get; set; }
        public int? ParentTaskId { get; set; }
        public string TaskCode { get; set; } = "";

        public string TaskName
        {
            get => _taskName;
            set => SetProperty(ref _taskName, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description { get; set; } = "";

        public TaskStatuss Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string StatusDisplayName => TaskStatussExtensions.GetDisplayName(Status);

        public string StatusBadgeColor => TaskStatussExtensions.GetHexColor(Status);

        public TaskPriority Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public string PriorityIcon => TaskPriorityExtensions.GetIcon(Priority);

        public int AssignedToId { get; set; }
        public string AssignedToName { get; set; } = "";
        public int ReporterId { get; set; }
        public string ReporterName { get; set; } = "";
        public List<int> AssignedToIds { get; set; }
        public List<string> AssignedToNames { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }

        public decimal Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public int ProgressPercentage => (int)Progress;

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int CompletedBy { get; set; }
        public bool IsBlocked { get; set; }
        public string BlockReason { get; set; } = "";
        public bool IsActive { get; set; }
        public Department TaskType { get; set; } = Department.OTHER;
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = "";

        public bool IsOverdue
        {
            get => _isOverdue;
            set => SetProperty(ref _isOverdue, value);
        }

        public bool IsCompleted { get; set; }
        public bool IsSubTask { get; set; }
        public int CommentCount { get; set; }
        public int CompletedCommentCount { get; set; }
        public long Version { get; set; }

        // UI Helper Properties
        public string DisplayName => !string.IsNullOrEmpty(TaskName) ? TaskName : Title;

        public string CommentSummaryText => $"{CompletedCommentCount}/{CommentCount}";

        public decimal TotalTimeCommentActualHours { get; set; }

        /// <summary>
        /// Progress color based on value
        /// </summary>
        public Brush ProgressColor
        {
            get
            {
                if (Progress >= 90) return new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Green
                if (Progress >= 70) return new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Blue
                if (Progress >= 40) return new SolidColorBrush(Color.FromRgb(243, 156, 18)); // Orange
                return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
            }
        }

        /// <summary>
        /// Due date display text
        /// </summary>
        public string DueDateDisplayText
        {
            get
            {
                if (DueDate == null)
                    return "Chưa định";

                var diff = DueDate.Value - DateTime.Now;
                if (diff.TotalDays < 0)
                    return "Quá hạn";
                if (diff.TotalDays <= 1)
                    return "Hôm nay";
                if (diff.TotalDays <= 7)
                    return string.Format("{0} ngày", (int)diff.TotalDays);

                return DueDate.Value.ToString("dd/MM/yyyy");
            }
        }

        /// <summary>
        /// Due date color based on urgency
        /// </summary>
        public Brush DueDateColor
        {
            get
            {
                if (DueDate == null)
                    return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Gray

                var diff = DueDate.Value - DateTime.Now;
                if (diff.TotalDays < 0)
                    return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red - Overdue
                if (diff.TotalDays <= 1)
                    return new SolidColorBrush(Color.FromRgb(243, 156, 18)); // Orange - Today
                if (diff.TotalDays <= 3)
                    return new SolidColorBrush(Color.FromRgb(241, 196, 15)); // Yellow - Soon

                return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Gray - Normal
            }
        }

        #region Hierarchy Support Properties

        /// <summary>
        /// Hierarchy level in task tree (0 = root task, 1 = first level subtask, etc.)
        /// </summary>
        public int HierarchyLevel
        {
            get => _hierarchyLevel;
            set
            {
                if (SetProperty(ref _hierarchyLevel, value))
                {
                    OnPropertyChanged(nameof(IndentWidth));
                    OnPropertyChanged(nameof(IndentMargin));
                    OnPropertyChanged(nameof(LevelBadgeColor));
                    OnPropertyChanged(nameof(LevelIcon));
                    OnPropertyChanged(nameof(LevelTooltip));
                    OnPropertyChanged(nameof(LevelBackground));
                    OnPropertyChanged(nameof(HierarchyBorderBrush));
                    OnPropertyChanged(nameof(HierarchyBorderThickness));
                }
            }
        }

        /// <summary>
        /// Whether subtasks are currently being loaded
        /// </summary>
        public bool IsLoadingSubTasks
        {
            get => _isLoadingSubTasks;
            set
            {
                if (SetProperty(ref _isLoadingSubTasks, value))
                {
                    OnPropertyChanged(nameof(ExpandCollapseIcon));
                    OnPropertyChanged(nameof(ExpandCollapseColor));
                    OnPropertyChanged(nameof(ExpandCollapseTooltip));
                }
            }
        }

        /// <summary>
        /// Collection of subtasks
        /// </summary>
        public ObservableCollection<ProjectTaskModel> SubTasks
        {
            get => _subTasks;
            set
            {
                if (SetProperty(ref _subTasks, value))
                {
                    OnPropertyChanged(nameof(HasSubTasks));
                    OnPropertyChanged(nameof(LoadedSubTaskCount));
                    OnPropertyChanged(nameof(RemainingSubTasksCount));
                    OnPropertyChanged(nameof(LoadMoreRowDisplayText));
                }
            }
        }

        /// <summary>
        /// Whether this task is currently expanded to show subtasks
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value))
                {
                    OnPropertyChanged(nameof(ExpandCollapseIcon));
                    OnPropertyChanged(nameof(ExpandCollapseColor));
                    OnPropertyChanged(nameof(ExpandCollapseTooltip));
                }
            }
        }

        /// <summary>
        /// Whether this task is selected in UI
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Total number of direct subtasks (from server)
        /// </summary>
        public int TotalSubTaskCount { get; set; }

        public int SubTasksCompleted { get; set; }

        /// <summary>
        /// Whether this task has any subtasks
        /// </summary>
        public bool HasSubTasks => TotalSubTaskCount > 0 || (SubTasks?.Count ?? 0) > 0;

        /// <summary>
        /// Whether this task is a root task (no parent)
        /// </summary>
        public bool IsRootTask => ParentTaskId <= 0;

        /// <summary>
        /// Number of subtasks loaded in memory
        /// </summary>
        public int LoadedSubTaskCount => SubTasks?.Count ?? 0;

        /// <summary>
        /// Whether can expand this task
        /// </summary>
        public bool CanExpand => HasSubTasks && !IsExpanded;

        /// <summary>
        /// Whether can collapse this task
        /// </summary>
        public bool CanCollapse => HasSubTasks && IsExpanded;

        /// <summary>
        /// Indent width for UI (pixels)
        /// </summary>
        public double IndentWidth => HierarchyLevel * 10;

        /// <summary>
        /// Indent margin for hierarchical display
        /// </summary>
        public Thickness IndentMargin => new Thickness(HierarchyLevel * 20, 0, 0, 0);

        /// <summary>
        /// Expand/Collapse icon
        /// </summary>
        public string ExpandCollapseIcon
        {
            get
            {
                if (!HasSubTasks) return "";
                if (IsLoadingSubTasks) return "⏳";
                return IsExpanded ? "▼" : "▶";
            }
        }

        /// <summary>
        /// Expand/Collapse button color
        /// </summary>
        public string ExpandCollapseColor
        {
            get
            {
                if (IsLoadingSubTasks) return "#2196F3";
                return IsExpanded ? "#28A745" : "#6C757D";
            }
        }

        /// <summary>
        /// Expand/Collapse tooltip
        /// </summary>
        public string ExpandCollapseTooltip
        {
            get
            {
                if (IsLoadingSubTasks) return "Đang tải subtasks...";
                if (!HasSubTasks) return "";

                var totalSubTasks = TotalSubTaskCount > 0 ? TotalSubTaskCount : SubTasks.Count;

                if (IsExpanded)
                    return string.Format("Click để thu gọn {0} subtasks", totalSubTasks);
                else
                    return string.Format("Click để xem {0} subtasks", totalSubTasks);
            }
        }

        /// <summary>
        /// Level badge color based on hierarchy level
        /// </summary>
        public string LevelBadgeColor
        {
            get
            {
                switch (HierarchyLevel)
                {
                    case 0: return "#007BFF"; // Root - Blue
                    case 1: return "#28A745"; // Level 1 - Green
                    case 2: return "#FFC107"; // Level 2 - Yellow
                    case 3: return "#FF9800"; // Level 3 - Orange
                    default: return "#6C757D"; // Level 4+ - Gray
                }
            }
        }

        /// <summary>
        /// Level icon based on hierarchy level
        /// </summary>
        public string LevelIcon
        {
            get
            {
                switch (HierarchyLevel)
                {
                    case 0: return "📋"; // Root task
                    case 1: return "📄"; // First level subtask
                    case 2: return "📝"; // Second level subtask
                    default: return "•";  // Deeper levels
                }
            }
        }

        /// <summary>
        /// Level tooltip
        /// </summary>
        public string LevelTooltip
        {
            get
            {
                switch (HierarchyLevel)
                {
                    case 0: return "Task gốc";
                    case 1: return "Subtask cấp 1";
                    case 2: return "Subtask cấp 2";
                    case 3: return "Subtask cấp 3";
                    default: return string.Format("Subtask cấp {0}", HierarchyLevel);
                }
            }
        }

        /// <summary>
        /// Background color based on level
        /// </summary>
        public Brush LevelBackground
        {
            get
            {
                switch (HierarchyLevel)
                {
                    case 0: return new SolidColorBrush(Colors.White);
                    case 1: return new SolidColorBrush(Color.FromRgb(0xF0, 0xF8, 0xFF));
                    case 2: return new SolidColorBrush(Color.FromRgb(0xFF, 0xFA, 0xE6));
                    case 3: return new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0xEE));
                    default: return new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
                }
            }
        }

        /// <summary>
        /// Border brush for hierarchy visual
        /// </summary>
        public Brush HierarchyBorderBrush
        {
            get
            {
                if (IsRootTask)
                    return new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));

                switch (HierarchyLevel)
                {
                    case 1: return new SolidColorBrush(Color.FromRgb(0x00, 0x7B, 0xFF));
                    case 2: return new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
                    case 3: return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                    default: return new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
                }
            }
        }

        /// <summary>
        /// Border thickness for hierarchy visual
        /// </summary>
        public Thickness HierarchyBorderThickness
        {
            get
            {
                if (!IsRootTask)
                    return new Thickness(2, 0, 0, 0); // Left border for subtasks
                return new Thickness(0, 0, 0, 1); // Bottom border for root
            }
        }

        /// <summary>
        /// Visibility for expand button
        /// </summary>
        public Visibility ExpandButtonVisibility => HasSubTasks ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Subtask count display text
        /// </summary>
        public string SubTaskCountDisplay
        {
            get
            {
                if (!HasSubTasks) return "";
                return string.Format("{0}/{1}", SubTasksCompleted, TotalSubTaskCount);
            }
        }

        /// <summary>
        /// Tooltip for subtask count badge
        /// </summary>
        public string SubTaskCountTooltip
        {
            get
            {
                if (!HasSubTasks) return "";
                var remaining = TotalSubTaskCount - SubTasksCompleted;
                return string.Format("{0} subtasks ({1} hoàn thành, {2} còn lại)", 
                    TotalSubTaskCount, SubTasksCompleted, remaining);
            }
        }

        /// <summary>
        /// Task type icon for hierarchy
        /// </summary>
        public string TaskTypeIcon => HasSubTasks ? "📁" : "📄";

        /// <summary>
        /// Progress text with subtasks consideration
        /// </summary>
        public string ProgressText
        {
            get
            {
                if (HasSubTasks)
                {
                    var subTaskProgress = TotalSubTaskCount > 0 
                        ? (SubTasksCompleted * 100.0m / TotalSubTaskCount) 
                        : 0;
                    return string.Format("{0:F1}% (Subtasks: {1:F1}%)", Progress, subTaskProgress);
                }
                return string.Format("{0:F1}%", Progress);
            }
        }

        #endregion

        #region Load More SubTasks Support

        /// <summary>
        /// Show "Load More SubTasks" button khi còn subtasks chưa load
        /// </summary>
        public bool ShowLoadMoreSubTasksButton =>
            IsExpanded && HasSubTasks && (SubTasks.Count < TotalSubTaskCount);

        /// <summary>
        /// Text cho Load More button
        /// </summary>
        public string LoadMoreSubTasksButtonText
        {
            get
            {
                var remaining = TotalSubTaskCount - SubTasks.Count;
                return remaining > 0 ? $"Xem thêm ({remaining} công việc)" : "";
            }
        }

        /// <summary>
        /// Số lượng subtasks còn lại chưa load
        /// </summary>
        public int RemainingSubTasksCount =>
            Math.Max(0, TotalSubTaskCount - SubTasks.Count);

        /// <summary>
        /// Tooltip cho Load More button
        /// </summary>
        public string LoadMoreSubTasksTooltip =>
            $"Click để load thêm {Math.Min(20, RemainingSubTasksCount)} công việc.";

        /// <summary>
        /// Check if this is a "Load More" placeholder row
        /// </summary>
        public bool IsLoadMorePlaceholderRow =>
            Id < 0 && TaskCode?.StartsWith("__LOADMORE_") == true;

        /// <summary>
        /// Display text for Load More row
        /// </summary>
        public string LoadMoreRowDisplayText
        {
            get
            {
                if (!IsLoadMorePlaceholderRow) return "";
                var remaining = RemainingSubTasksCount;
                var lastLoaded = TotalSubTaskCount - remaining;
                return $"Xem thêm";
            }
        }

        /// <summary>
        /// Style for Load More row
        /// </summary>
        public Brush LoadMoreRowBackground => new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)); // Light Blue

        public Brush LoadMoreRowForeground => new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2)); // Blue



        #endregion

        #region Time Tracking Properties

        /// <summary>
        /// Hiển thị nội dung text lên Combobox chọn task khi ghi thời gian
        /// </summary>
        public string DisplayTextForTimeTracking
        {
            get
            {
                string space = "   ";
                string text = "";
                for (int i = 0; i < HierarchyLevel; i++)
                {
                    text += space;
                }
                
                text += $"{TaskCode} - {Title}";

                return text;
            }
        }

        #endregion

        #region Mapping Methods

        public static ProjectTaskModel MapToProjectTaskModel(ProjectTaskDto dto)
        {
            if (dto == null) return null;

            var model = new ProjectTaskModel
            {
                Id = dto.Id,
                ProjectId = dto.ProjectId,
                ParentTaskId = dto.ParentTaskId ?? 0,
                TaskCode = dto.TaskCode,
                TaskName = dto.Title,
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                Priority = dto.Priority,
                AssignedToId = dto.AssignedToId ?? 0,
                AssignedToName = dto.AssignedToName,
                ReporterId = dto.ReporterId ?? 0,
                ReporterName = dto.ReporterName,
                AssignedToIds = dto.AssignedToIds,
                AssignedToNames = dto.AssignedToNames,
                TaskType = dto.TaskType,
                StartDate = dto.StartDate,
                DueDate = dto.DueDate,
                CompletedAt = dto.CompletedAt,
                EstimatedHours = dto.EstimatedHours,
                ActualHours = dto.ActualHours,
                Progress = dto.Progress,
                CreatedAt = dto.CreatedAt,
                IsOverdue = dto.IsOverdue,
                Version = dto.Version,
                CommentCount = dto.CommentCount,
                CompletedCommentCount = dto.CompletedCommentCount,
                IsCompleted = dto.IsCompleted,
                IsSubTask = dto.IsSubTask,
                IsBlocked = dto.IsBlocked,
                BlockReason = dto.BlockReason,
                IsActive = dto.IsActive,
                
                // Hierarchy properties
                HierarchyLevel = dto.HierarchyLevel,
                TotalSubTaskCount = dto.TotalChildCount,
                SubTasksCompleted = dto.SubTasksCompleted,
                TotalTimeCommentActualHours = dto.TotalTimeCommentActualHours,
                IsExpanded = dto.IsExpanded
            };

            // Convert subtasks recursively if present
            if (dto.SubTasks != null && dto.SubTasks.Count > 0)
            {
                foreach (var subTaskDto in dto.SubTasks)
                {
                    var subTaskModel = MapToProjectTaskModel(subTaskDto);
                    if (subTaskModel != null)
                    {
                        model.SubTasks.Add(subTaskModel);
                    }
                }
            }

            return model;
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Helper Methods

        public List<string> GetTags()
        {
            return Tags;
        }

        /// <summary>
        /// Highlight task with animation effect
        /// </summary>
        public void Highlight()
        {
            IsSelected = true;
            System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(() => IsSelected = false);
            });
        }

        #endregion
    }
}
