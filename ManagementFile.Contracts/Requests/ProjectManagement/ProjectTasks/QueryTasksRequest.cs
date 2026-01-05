using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để query/filter tasks
    /// </summary>
    public class QueryTasksRequest : IValidatableObject
    {
        public int? ProjectId { get; set; }
        public int? AssignedToId { get; set; }
        public int? ReporterId { get; set; }
        public int? ParentTaskId { get; set; }

        public List<TaskStatuss> Statuses { get; set; } = new List<TaskStatuss>();
        public List<TaskPriority> Priorities { get; set; } = new List<TaskPriority>();
        public List<string> TaskTypes { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();

        public bool? IsBlocked { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? IsCompleted { get; set; }
        public bool? HasSubTasks { get; set; }
        public bool? IsActive { get; set; }

        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }

        [Range(0, 100)]
        public decimal? ProgressMin { get; set; }

        [Range(0, 100)]
        public decimal? ProgressMax { get; set; }

        [StringLength(200)]
        public string SearchKeyword { get; set; } = "";

        // Pagination
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 1000)]
        public int PageSize { get; set; } = 20;

        // Sorting
        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (StartDateFrom.HasValue && StartDateTo.HasValue && StartDateTo.Value < StartDateFrom.Value)
            {
                results.Add(new ValidationResult(
                    "StartDateTo phải sau hoặc bằng StartDateFrom",
                    new[] { nameof(StartDateTo) }));
            }

            if (DueDateFrom.HasValue && DueDateTo.HasValue && DueDateTo.Value < DueDateFrom.Value)
            {
                results.Add(new ValidationResult(
                    "DueDateTo phải sau hoặc bằng DueDateFrom",
                    new[] { nameof(DueDateTo) }));
            }

            if (ProgressMin.HasValue && ProgressMax.HasValue && ProgressMax.Value < ProgressMin.Value)
            {
                results.Add(new ValidationResult(
                    "ProgressMax phải >= ProgressMin",
                    new[] { nameof(ProgressMax) }));
            }

            if (ProjectId.HasValue && ProjectId.Value <= 0)
            {
                results.Add(new ValidationResult(
                    "ProjectId phải > 0",
                    new[] { nameof(ProjectId) }));
            }

            var validSortFields = new[] { "CreatedAt", "UpdatedAt", "Title", "Priority",
                "DueDate", "Progress", "Status", "EstimatedHours", "ActualHours" };

            if (!string.IsNullOrEmpty(SortBy) && !validSortFields.Contains(SortBy))
            {
                results.Add(new ValidationResult(
                    $"SortBy phải là một trong: {string.Join(", ", validSortFields)}",
                    new[] { nameof(SortBy) }));
            }

            return results;
        }
    }
}
