using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    /// <summary>
    /// Project Task DTO
    /// ✅ Enhanced with Hierarchy support for parent-child task relationships
    /// Compatible with .NET Standard 2.0 (C# 7.3)
    /// </summary>
    public class ProjectTaskDto
    {
        #region Basic Properties

        public int Id { get; set; } 
        public string TaskCode { get; set; } = "";
        public string TaskName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; }
        public TaskStatuss Status { get; set; }
        public TaskPriority Priority { get; set; }
        public decimal Progress { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysRemaining { get; set; }

        #endregion

        #region References

        public int ProjectId { get; set; } 
        public string ProjectName { get; set; } = "";
        public int? AssignedToId { get; set; }
        public string AssignedToName { get; set; }
        public int? ParentTaskId { get; set; }
        public string ParentTaskName { get; set; }
        public int? ReporterId { get; set; }
        public string ReporterName { get; set; }
        
        // Multiple assignees support
        public List<int> AssignedToIds { get; set; } = new List<int>();
        public List<string> AssignedToNames { get; set; } = new List<string>();

        #endregion

        #region Metadata

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CreatedBy { get; set; } 
        public string CreatedByName { get; set; } = "";
        public int UpdatedBy { get; set; }
        public string UpdatedByName { get; set; }
        public int? CompletedBy { get; set; }

        #endregion

        #region Status Flags

        public bool IsBlocked { get; set; }
        public string BlockReason { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsSubTask { get; set; }

        public long Version { get; set; }

        #endregion

        #region Collections

        public List<string> Dependencies { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<ProjectTaskDto> SubTasks { get; set; } = new List<ProjectTaskDto>();
        public List<TaskCommentDto> Comments { get; set; } = new List<TaskCommentDto>();
        
        public int CommentCount { get; set; }
        public int CompletedCommentCount { get; set; }

        public decimal TotalTimeCommentActualHours { get; set; }

        public Department TaskType { get; set; }

        #endregion

        #region ✅ Hierarchy Support Properties

        /// <summary>
        /// Hierarchy level in task tree (0 = root task, 1 = first level subtask, etc.)
        /// </summary>
        public int HierarchyLevel { get; set; }

        /// <summary>
        /// Total number of direct subtasks (not recursive)
        /// Similar to ProjectDto.TotalChildCount
        /// </summary>
        public int TotalChildCount { get; set; }

        /// <summary>
        /// Whether this task has any subtasks
        /// Computed: TotalChildCount > 0 || SubTasks.Count > 0
        /// </summary>
        public bool HasSubTasks { get; set; }

        /// <summary>
        /// Whether this task is a root task (no parent)
        /// Computed: !ParentTaskId.HasValue || ParentTaskId <= 0
        /// </summary>
        public bool IsRootTask { get; set; }

        /// <summary>
        /// Whether this task is currently expanded to show subtasks (UI state)
        /// Used for hierarchical display in DataGrid/TreeView
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Whether subtasks are currently being loaded (UI loading state)
        /// Shows loading spinner/indicator in UI
        /// </summary>
        public bool IsLoadingSubTasks { get; set; }

        /// <summary>
        /// Progress percentage of subtasks completion
        /// Calculated as: (CompletedSubTasks / TotalSubTasks) * 100
        /// Rollup metric for parent tasks
        /// </summary>
        public decimal SubTasksProgressPercentage { get; set; }

        /// <summary>
        /// Full path of parent task IDs from root to current task
        /// Example: "1>5>12" means root(1) -> parent(5) -> current(12)
        /// Used for hierarchy navigation and breadcrumb
        /// </summary>
        public string HierarchyPath { get; set; } = "";

        /// <summary>
        /// Display text showing task hierarchy position
        /// Example: "Project Management > UI Development > Login Form"
        /// Human-readable breadcrumb for UI display
        /// </summary>
        public string HierarchyBreadcrumb { get; set; } = "";

        /// <summary>
        /// Maximum allowed depth for nested subtasks
        /// Default: 5 levels (0-root, 1-4 subtask levels)
        /// Prevents infinite nesting and performance issues
        /// </summary>
        public int MaxHierarchyDepth { get; set; } = 5;

        /// <summary>
        /// Whether this task can have more subtasks added based on current depth
        /// Computed: HierarchyLevel < MaxHierarchyDepth
        /// </summary>
        public bool CanAddSubTask { get; set; } = true;

        /// <summary>
        /// Rollup completion status - true if all subtasks (recursive) are completed
        /// Used to validate parent task completion
        /// </summary>
        public int SubTasksCompleted { get; set; }

        /// <summary>
        /// Earliest start date among all subtasks (recursive)
        /// Rollup metric for project planning
        /// </summary>
        public DateTime? SubTasksEarliestStart { get; set; }

        /// <summary>
        /// Latest due date among all subtasks (recursive)
        /// Critical path calculation for parent tasks
        /// </summary>
        public DateTime? SubTasksLatestDue { get; set; }

        /// <summary>
        /// Total estimated hours for all subtasks (recursive sum)
        /// Rollup for resource planning
        /// </summary>
        public decimal SubTasksTotalEstimatedHours { get; set; }

        /// <summary>
        /// Total actual hours spent on all subtasks (recursive sum)
        /// Rollup for time tracking and billing
        /// </summary>
        public decimal SubTasksTotalActualHours { get; set; }

        #endregion

    }
}
