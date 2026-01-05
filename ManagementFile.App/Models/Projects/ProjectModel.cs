using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace ManagementFile.App.Models.Projects
{
    public class ProjectModel : INotifyPropertyChanged
    {
        private int _id;
        private string _projectCode = "";
        private string _projectName = "";
        private string _description = "";
        private ProjectStatus _status = ProjectStatus.Planning;
        private TaskPriority _priority = TaskPriority.Low;
        private decimal _completionPercentage;
        private bool _isOverdue;

        //Parent/Child support
        private int? _projectParentId;
        private int _totalChildCount;
        private int _hierarchyLevel = 0;
        private bool _isLoadingChildren = false;
        private ObservableCollection<ProjectModel> _children = new ObservableCollection<ProjectModel>();

        // UI State
        private bool _isSelected;
        private bool _isExpanded;
        private bool _isHighlighted;
        private bool _showActions = true;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string IdText => Id > 0 ? ProjectCode : "";

        public string ProjectCode
        {
            get => _projectCode;
            set => SetProperty(ref _projectCode, value);
        }

        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ProjectStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public TaskPriority Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                OnPropertyChanged();
            }
        }

        public int? ProjectManagerId { get; set; }
        public string ProjectManagerName { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string ClientName { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EstimatedEndDate { get; set; }
        public decimal Budget { get; set; }
        public decimal EstimatedBudget { get; set; }
        public decimal ActualBudget { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }

        public decimal CompletionPercentage
        {
            get => _completionPercentage;
            set => SetProperty(ref _completionPercentage, value);
        }

        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }

        public bool IsOverdue
        {
            get => _isOverdue;
            set => SetProperty(ref _isOverdue, value);
        }

        public bool IsCompleted { get; set; }
        public decimal BudgetVariance { get; set; }
        public int HourVariance { get; set; }

        // Additional properties for Phase 2 compatibility
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalMembers { get; set; }

        // UI Helper Properties
        public string DisplayName => $"{ProjectCode} - {ProjectName}";
        public string StatusDisplay => ProjectStatusHelper.GetName(Status);
        public string StatusIcon => ProjectStatusHelper.GetDisplayIcon(Status);
        public string PriorityDisplay => Priority.GetDisplayName();
        public string ProgressDisplay => $"{CompletionPercentage:F1}%";
        public string BudgetDisplay => $"${EstimatedBudget:N0} / ${ActualBudget:N0}";

        /// <summary>
        /// Status color
        /// </summary>
        public Brush StatusColor
        {
            get
            {
                var color = ProjectStatusHelper.GetRgbColor(Status);
                return new SolidColorBrush(Color.FromRgb(
                    byte.Parse(color.R.ToString()),
                    byte.Parse(color.G.ToString()),
                    byte.Parse(color.B.ToString())));
            }
        }

        /// <summary>
        /// Status badge color
        /// </summary>
        public Brush StatusBadgeColor => StatusColor;

        /// <summary>
        /// Progress text
        /// </summary>
        public string ProgressText => $"{CompletionPercentage:F0}%";

        /// <summary>
        /// Tasks summary text
        /// </summary>
        public string TasksSummary => $"{CompletedTasks}/{TotalTasks}";

        /// <summary>
        /// Due date display text
        /// </summary>
        public string DueDateDisplayText
        {
            get
            {
                if (EstimatedEndDate == null)
                    return "Chưa định";

                var diff = EstimatedEndDate.Value - DateTime.Now;
                if (diff.TotalDays < 0)
                    return "Quá hạn";
                if (diff.TotalDays <= 7)
                    return $"{(int)diff.TotalDays} ngày";

                return EstimatedEndDate.Value.ToString("dd/MM/yyyy");
            }
        }

        /// <summary>
        /// Created date display text
        /// </summary>
        public string CreatedAtDisplayText => CreatedAt.ToString("dd/MM/yyyy");

        #region UI State Properties

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

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

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        public bool ShowActions
        {
            get => _showActions;
            set => SetProperty(ref _showActions, value);
        }

        #endregion

        #region Hierarchy and Lazy Loading Properties

        /// <summary>
        /// Cấp độ phân cấp (0 = root, 1 = child level 1, etc.)
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
        /// Đang load children
        /// </summary>
        public bool IsLoadingChildren
        {
            get => _isLoadingChildren;
            set
            {
                if (SetProperty(ref _isLoadingChildren, value))
                {
                    OnPropertyChanged(nameof(ExpandCollapseIcon));
                    OnPropertyChanged(nameof(ExpandCollapseColor));
                    OnPropertyChanged(nameof(ExpandCollapseTooltip));
                    OnPropertyChanged(nameof(ShowLoadMoreChildren));
                }
            }
        }

        /// <summary>
        /// Tổng số children từ server (có thể > số children đã load)
        /// </summary>
        public int TotalChildCount
        {
            get => _totalChildCount;
            set
            {
                if (SetProperty(ref _totalChildCount, value))
                {
                    OnPropertyChanged(nameof(ChildCount));
                    OnPropertyChanged(nameof(HasChildren));
                    OnPropertyChanged(nameof(CanExpand));
                    OnPropertyChanged(nameof(ShowLoadMoreChildren));
                    OnPropertyChanged(nameof(LoadMoreChildrenButtonText));
                    OnPropertyChanged(nameof(LoadMoreChildrenTooltip));
                    OnPropertyChanged(nameof(ChildCountTooltip));
                }
            }
        }

        /// <summary>
        /// Indent width based on hierarchy level (20px per level)
        /// </summary>
        public double IndentWidth => HierarchyLevel * 10;

        /// <summary>
        /// Indent margin cho hierarchical display
        /// </summary>
        public Thickness IndentMargin => new Thickness(HierarchyLevel * 20, 0, 0, 0);

        /// <summary>
        /// Can this project be expanded
        /// </summary>
        public bool CanExpand => HasChildren || TotalChildCount > 0;

        /// <summary>
        /// Số lượng children đã load
        /// </summary>
        public int LoadedChildCount => Children?.Count ?? 0;

        /// <summary>
        /// Expand/Collapse icon
        /// </summary>
        public string ExpandCollapseIcon
        {
            get
            {
                if (!CanExpand) return "";
                if (IsLoadingChildren) return "⏳";
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
                if (IsLoadingChildren) return "#2196F3";
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
                if (IsLoadingChildren) return "Đang tải dự án con...";
                if (!CanExpand) return "";

                var totalChildren = TotalChildCount > 0 ? TotalChildCount : ChildCount;

                if (IsExpanded)
                    return $"Click để thu gọn {totalChildren} dự án con";
                else
                    return $"Click để xem {totalChildren} dự án con";
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
                    case 0:
                        return "#007BFF"; // Root - Blue
                    case 1:
                        return "#28A745"; // Level 1 - Green
                    case 2:
                        return "#FFC107"; // Level 2 - Yellow
                    case 3:
                        return "#FF9800"; // Level 3 - Orange
                    default:
                        return "#6C757D";  // Level 4+ - Gray
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
                    case 0:
                        return "📁"; // Root project
                    case 1:
                        return "📂"; // First level child
                    case 2:
                        return "📄"; // Second level child
                    case 3:
                        return "📃";  // Third level child
                    default:
                        return "📋";   // Deeper levels
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
                    case 0:
                        return "Dự án gốc";
                    case 1:
                        return "Dự án con cấp 1";
                    case 2:
                        return "Dự án con cấp 2";
                    case 3:
                        return "Dự án con cấp 3";
                    default:
                        return $"Dự án con cấp {HierarchyLevel}";
                }
            }
        }

        /// <summary>
        /// Child count tooltip
        /// </summary>
        public string ChildCountTooltip
        {
            get
            {
                var totalChildren = TotalChildCount > 0 ? TotalChildCount : ChildCount;
                if (totalChildren == 0) return "";

                var loadedCount = LoadedChildCount;

                if (loadedCount >= totalChildren)
                    return $"Tất cả {totalChildren} dự án con đã được tải";
                else if (loadedCount > 0)
                    return $"Đã tải {loadedCount}/{totalChildren} dự án con. Click ▶ để tải thêm";
                else
                    return $"Có {totalChildren} dự án con. Click ▶ để xem";
            }
        }

        /// <summary>
        /// Show "Load More Children" button
        /// </summary>
        public bool ShowLoadMoreChildren
        {
            get
            {
                var totalChildren = TotalChildCount > 0 ? TotalChildCount : ChildCount;
                var loadedCount = LoadedChildCount;
                return IsExpanded && totalChildren > loadedCount && !IsLoadingChildren;
            }
        }

        /// <summary>
        /// Load More Children button text
        /// </summary>
        public string LoadMoreChildrenButtonText
        {
            get
            {
                var totalChildren = TotalChildCount > 0 ? TotalChildCount : ChildCount;
                var loadedCount = LoadedChildCount;
                var remaining = totalChildren - loadedCount;
                return $"📥 Tải thêm {Math.Min(remaining, 20)} dự án ({remaining} còn lại)";
            }
        }

        /// <summary>
        /// Load More Children tooltip
        /// </summary>
        public string LoadMoreChildrenTooltip
        {
            get
            {
                var totalChildren = TotalChildCount > 0 ? TotalChildCount : ChildCount;
                var loadedCount = LoadedChildCount;
                var remaining = totalChildren - loadedCount;
                return $"Đã tải {loadedCount}/{totalChildren} dự án con. Click để tải thêm {Math.Min(remaining, 20)} dự án.";
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
                    case 0:
                        return new SolidColorBrush(Colors.White);
                    case 1:
                        return new SolidColorBrush(Color.FromRgb(0xF0, 0xF8, 0xFF));
                    case 2:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xFA, 0xE6));
                    case 3:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0xEE));
                    default:
                        return new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
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
                if (!IsChild)
                    return new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));

                switch (HierarchyLevel)
                {
                    case 1:
                        return new SolidColorBrush(Color.FromRgb(0x00, 0x7B, 0xFF));
                    case 2:
                        return new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
                    case 3:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                    default:
                        return new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
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
                if (IsChild)
                    return new Thickness(2, 0, 0, 0); // Left border for children
                return new Thickness(0, 0, 0, 1); // Bottom border for root
            }
        }

        /// <summary>
        /// Visibility cho expand button
        /// </summary>
        public Visibility ExpandButtonVisibility => CanExpand ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Hierarchy indicator text
        /// </summary>
        public string HierarchyIndicator
        {
            get
            {
                if (HierarchyLevel == 0) return "";
                if (HierarchyLevel == 1) return "├─";
                if (HierarchyLevel == 2) return "│ ├─";
                return "│ │ ├─";
            }
        }

        #endregion

        #region Parent/Child Properties

        public bool IsChild => ProjectParentId.HasValue && ProjectParentId > 0;
        public bool HasChildren => TotalChildCount > 0 || (Children?.Count ?? 0) > 0;
        public int ChildCount => TotalChildCount > 0 ? TotalChildCount : (Children?.Count ?? 0);
        public bool IsRootProject => !ProjectParentId.HasValue;

        public int? ProjectParentId
        {
            get => _projectParentId;
            set
            {
                if (SetProperty(ref _projectParentId, value))
                {
                    OnPropertyChanged(nameof(IsChild));
                    OnPropertyChanged(nameof(IsRootProject));
                }
            }
        }

        public ObservableCollection<ProjectModel> Children
        {
            get => _children;
            set
            {
                if (SetProperty(ref _children, value))
                {
                    OnPropertyChanged(nameof(HasChildren));
                    OnPropertyChanged(nameof(ChildCount));
                    OnPropertyChanged(nameof(LoadedChildCount));
                    OnPropertyChanged(nameof(ShowLoadMoreChildren));
                }
            }
        }

        #endregion

        #region Load More SubTasks Support

        /// <summary>
        /// Show "Load More SubTasks" button khi còn subtasks chưa load
        /// </summary>
        public bool ShowLoadMoreSubTasksButton =>
            IsExpanded && HasChildren && (Children.Count < TotalChildCount);

        /// <summary>
        /// Text cho Load More button
        /// </summary>
        public string LoadMoreSubTasksButtonText
        {
            get
            {
                var remaining = TotalChildCount - Children.Count;
                return remaining > 0 ? $"Xem thêm ({remaining} dự án)" : "";
            }
        }

        /// <summary>
        /// Số lượng subtasks còn lại chưa load
        /// </summary>
        public int RemainingSubTasksCount =>
            Math.Max(0, TotalChildCount - Children.Count);

        /// <summary>
        /// Tooltip cho Load More button
        /// </summary>
        public string LoadMoreSubTasksTooltip =>
            $"Click để load thêm {Math.Min(20, RemainingSubTasksCount)} dự án con.";

        /// <summary>
        /// Check if this is a "Load More" placeholder row
        /// </summary>
        public bool IsLoadMorePlaceholderRow =>
            Id < 0 && ProjectCode?.StartsWith("__LOADMORE_") == true;

        /// <summary>
        /// Display text for Load More row
        /// </summary>
        public string LoadMoreRowDisplayText
        {
            get
            {
                if (!IsLoadMorePlaceholderRow) return "";
                return $"Xem thêm";
            }
        }

        /// <summary>
        /// Style for Load More row
        /// </summary>
        public Brush LoadMoreRowBackground => new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)); // Light Blue

        public Brush LoadMoreRowForeground => new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2)); // Blue



        #endregion

        #region Events

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

        /// <summary>
        /// Clone project model
        /// </summary>
        public ProjectModel Clone()
        {
            var clone = new ProjectModel
            {
                Id = Id,
                ProjectCode = ProjectCode,
                ProjectName = ProjectName,
                Description = Description,
                Status = Status,
                Priority = Priority,
                CompletionPercentage = CompletionPercentage,
                ProjectParentId = ProjectParentId,
                HierarchyLevel = HierarchyLevel,
                TotalChildCount = TotalChildCount,
                ProjectManagerId = ProjectManagerId,
                ProjectManagerName = ProjectManagerName,
                ClientId = ClientId,
                ClientName = ClientName,
                StartDate = StartDate,
                PlannedEndDate = PlannedEndDate,
                ActualEndDate = ActualEndDate,
                EndDate = EndDate,
                EstimatedEndDate = EstimatedEndDate,
                Budget = Budget,
                EstimatedBudget = EstimatedBudget,
                ActualBudget = ActualBudget,
                EstimatedHours = EstimatedHours,
                ActualHours = ActualHours,
                IsActive = IsActive,
                IsPublic = IsPublic,
                CreatedAt = CreatedAt,
                CreatedBy = CreatedBy,
                UpdatedAt = UpdatedAt,
                UpdatedBy = UpdatedBy,
                IsOverdue = IsOverdue,
                IsCompleted = IsCompleted,
                BudgetVariance = BudgetVariance,
                HourVariance = HourVariance,
                TotalTasks = TotalTasks,
                CompletedTasks = CompletedTasks,
                TotalMembers = TotalMembers
            };

            if (Tags != null)
                clone.Tags = new List<string>(Tags);

            return clone;
        }




        /// <summary>
        /// Highlight project với animation effect
        /// </summary>
        public void Highlight()
        {
            IsHighlighted = true;
            System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(() => IsHighlighted = false);
            });
        }

        #endregion

        #region Static Hierarchy Building Methods

        /// <summary>
        /// Build hierarchy từ flat list của projects
        /// </summary>
        public static List<ProjectModel> BuildHierarchy(IEnumerable<ProjectModel> projects)
        {
            var result = new List<ProjectModel>();
            var projectDict = projects.ToDictionary(p => p.Id, p => p);

            var rootProjects = projects.Where(p => !p.ProjectParentId.HasValue).OrderBy(p => p.CreatedAt);

            foreach (var rootProject in rootProjects)
            {
                AddProjectAndChildren(rootProject, result, projectDict, 0);
            }

            return result;
        }

        private static void AddProjectAndChildren(ProjectModel project, List<ProjectModel> result,
            Dictionary<int, ProjectModel> projectDict, int level)
        {
            project.HierarchyLevel = level;
            result.Add(project);

            if (project.IsExpanded)
            {
                var children = projectDict.Values
                    .Where(p => p.ProjectParentId == project.Id)
                    .OrderBy(p => p.CreatedAt);

                foreach (var child in children)
                {
                    AddProjectAndChildren(child, result, projectDict, level + 1);
                }
            }
        }

        #endregion

        #region Static Conversion Methods

        /// <summary>
        /// Convert từ ProjectDto sang ProjectModel
        /// Hỗ trợ hierarchy với children recursively
        /// </summary>
        /// <param name="dto">ProjectDto from API</param>
        /// <returns>ProjectModel for UI binding</returns>
        public static ProjectModel FromDto(ProjectDto dto)
        {
            if (dto == null) return null;

            var model = new ProjectModel
            {
                Id = dto.Id,
                ProjectCode = dto.ProjectCode ?? "",
                ProjectName = dto.ProjectName ?? "",
                Description = dto.Description ?? "",
                Status = dto.Status,
                Priority = dto.Priority,
                CompletionPercentage = dto.CompletionPercentage,

                // Manager and Client info
                ProjectManagerId = dto.ProjectManagerId,
                ProjectManagerName = dto.ProjectManagerName ?? "",
                ClientId = dto.ClientId?.ToString() ?? "",
                ClientName = dto.ClientName ?? "",

                // Dates
                StartDate = dto.StartDate,
                PlannedEndDate = dto.PlannedEndDate,
                ActualEndDate = dto.ActualEndDate,
                EndDate = dto.ActualEndDate ?? dto.PlannedEndDate,
                EstimatedEndDate = dto.PlannedEndDate,

                // Budget and Hours
                EstimatedBudget = dto.EstimatedBudget,
                ActualBudget = dto.ActualBudget,
                Budget = dto.EstimatedBudget,
                EstimatedHours = dto.EstimatedHours,
                ActualHours = dto.ActualHours,

                // Status flags
                IsActive = dto.IsActive,
                IsPublic = dto.IsPublic,
                IsOverdue = dto.IsOverdue,
                IsCompleted = dto.IsCompleted,

                // Variance
                BudgetVariance = dto.BudgetVariance,
                HourVariance = dto.HourVariance,

                // Tags
                Tags = dto.Tags ?? new List<string>(),

                // Audit info
                CreatedAt = dto.CreatedAt,
                CreatedBy = dto.CreatedBy,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,

                // Summary info
                TotalTasks = dto.TotalTasks,
                CompletedTasks = dto.CompletedTasks,
                TotalMembers = dto.TotalMembers,

                // Hierarchy properties
                ProjectParentId = dto.ProjectParentId,
                TotalChildCount = dto.TotalChildCount,
                HierarchyLevel = 0 // Will be set by hierarchy builder
            };

            // Convert children recursively if present
            if (dto.Children != null && dto.Children.Count > 0)
            {
                model.Children = new ObservableCollection<ProjectModel>(
                    dto.Children.Select(childDto => FromDto(childDto))
                );
            }

            return model;
        }

        /// <summary>
        /// Convert từ ProjectModel sang ProjectDto
        /// Để gửi lên API hoặc serialize
        /// </summary>
        /// <param name="model">ProjectModel from UI</param>
        /// <returns>ProjectDto for API</returns>
        public static ProjectDto ToDto(ProjectModel model)
        {
            if (model == null) return null;

            var dto = new ProjectDto
            {
                Id = model.Id,
                ProjectCode = model.ProjectCode,
                ProjectName = model.ProjectName,
                Description = model.Description,
                Status = model.Status,
                Priority = model.Priority,
                CompletionPercentage = model.CompletionPercentage,

                // Manager and Client info
                ProjectManagerId = model.ProjectManagerId,
                ProjectManagerName = model.ProjectManagerName,
                ClientId = !string.IsNullOrEmpty(model.ClientId) && int.TryParse(model.ClientId, out int clientId)
                    ? (int?)clientId
                    : null,
                ClientName = model.ClientName,

                // Dates
                StartDate = model.StartDate,
                PlannedEndDate = model.PlannedEndDate,
                ActualEndDate = model.ActualEndDate,

                // Budget and Hours
                EstimatedBudget = model.EstimatedBudget,
                ActualBudget = model.ActualBudget,
                EstimatedHours = model.EstimatedHours,
                ActualHours = model.ActualHours,

                // Status flags
                IsActive = model.IsActive,
                IsPublic = model.IsPublic,
                IsOverdue = model.IsOverdue,
                IsCompleted = model.IsCompleted,

                // Variance
                BudgetVariance = model.BudgetVariance,
                HourVariance = model.HourVariance,

                // Tags
                Tags = model.Tags,

                // Audit info
                CreatedAt = model.CreatedAt,
                CreatedBy = model.CreatedBy,
                UpdatedAt = model.UpdatedAt,
                UpdatedBy = model.UpdatedBy,

                // Summary info
                TotalTasks = model.TotalTasks,
                CompletedTasks = model.CompletedTasks,
                TotalMembers = model.TotalMembers,

                // Hierarchy properties
                ProjectParentId = model.ProjectParentId,
                TotalChildCount = model.TotalChildCount
            };

            // Convert children recursively if present
            if (model.Children != null && model.Children.Count > 0)
            {
                dto.Children = model.Children.Select(childModel => ToDto(childModel)).ToList();
            }

            return dto;
        }

        /// <summary>
        /// Convert list of ProjectDto sang list of ProjectModel
        /// </summary>
        public static List<ProjectModel> FromDtoList(IEnumerable<ProjectDto> dtos)
        {
            if (dtos == null) return new List<ProjectModel>();

            return dtos.Select(dto => FromDto(dto)).Where(m => m != null).ToList();
        }

        /// <summary>
        /// Convert list of ProjectModel sang list of ProjectDto
        /// </summary>
        public static List<ProjectDto> ToDtoList(IEnumerable<ProjectModel> models)
        {
            if (models == null) return new List<ProjectDto>();

            return models.Select(model => ToDto(model)).Where(d => d != null).ToList();
        }

        /// <summary>
        /// Convert từ ProjectDto sang ProjectModel và build hierarchy tree
        /// </summary>
        public static List<ProjectModel> FromDtoListWithHierarchy(IEnumerable<ProjectDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return new List<ProjectModel>();

            // Convert all DTOs to models first
            var models = FromDtoList(dtos);

            // Build hierarchy
            return BuildHierarchy(models);
        }

        /// <summary>
        /// Update existing ProjectModel from ProjectDto
        /// Useful for refresh scenarios
        /// </summary>
        public static void UpdateFromDto(ProjectModel model, ProjectDto dto)
        {
            if (model == null || dto == null) return;

            model.Id = dto.Id;
            model.ProjectCode = dto.ProjectCode ?? "";
            model.ProjectName = dto.ProjectName ?? "";
            model.Description = dto.Description ?? "";
            model.Status = dto.Status;
            model.Priority = dto.Priority;
            model.CompletionPercentage = dto.CompletionPercentage;

            model.ProjectManagerId = dto.ProjectManagerId;
            model.ProjectManagerName = dto.ProjectManagerName ?? "";
            model.ClientId = dto.ClientId?.ToString() ?? "";
            model.ClientName = dto.ClientName ?? "";

            model.StartDate = dto.StartDate;
            model.PlannedEndDate = dto.PlannedEndDate;
            model.ActualEndDate = dto.ActualEndDate;
            model.EndDate = dto.ActualEndDate ?? dto.PlannedEndDate;
            model.EstimatedEndDate = dto.PlannedEndDate;

            model.EstimatedBudget = dto.EstimatedBudget;
            model.ActualBudget = dto.ActualBudget;
            model.Budget = dto.EstimatedBudget;
            model.EstimatedHours = dto.EstimatedHours;
            model.ActualHours = dto.ActualHours;

            model.IsActive = dto.IsActive;
            model.IsPublic = dto.IsPublic;
            model.IsOverdue = dto.IsOverdue;
            model.IsCompleted = dto.IsCompleted;

            model.BudgetVariance = dto.BudgetVariance;
            model.HourVariance = dto.HourVariance;

            model.Tags = dto.Tags ?? new List<string>();

            model.CreatedAt = dto.CreatedAt;
            model.CreatedBy = dto.CreatedBy;
            model.UpdatedAt = dto.UpdatedAt;
            model.UpdatedBy = dto.UpdatedBy;

            model.TotalTasks = dto.TotalTasks;
            model.CompletedTasks = dto.CompletedTasks;
            model.TotalMembers = dto.TotalMembers;

            model.ProjectParentId = dto.ProjectParentId;
            model.TotalChildCount = dto.TotalChildCount;

            // Update children if present
            if (dto.Children != null && dto.Children.Count > 0)
            {
                model.Children.Clear();
                foreach (var childDto in dto.Children)
                {
                    model.Children.Add(FromDto(childDto));
                }
            }
        }

        #endregion
    }
}
