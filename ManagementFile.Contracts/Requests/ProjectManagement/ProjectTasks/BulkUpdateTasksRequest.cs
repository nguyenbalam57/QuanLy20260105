using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để bulk update tasks
    /// </summary>
    public class BulkUpdateTasksRequest : IValidatableObject
    {
        [Required]
        [MinLength(1, ErrorMessage = "TaskIds phải có ít nhất 1 phần tử")]
        public List<int> TaskIds { get; set; } = new List<int>();

        public TaskStatuss? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public int? AssignedToId { get; set; }
        public bool? IsBlocked { get; set; }
        public string BlockReason { get; set; } = "";
        public List<string> TagsToAdd { get; set; } = new List<string>();
        public List<string> TagsToRemove { get; set; } = new List<string>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (TaskIds == null || TaskIds.Count == 0)
            {
                results.Add(new ValidationResult(
                    "TaskIds không được để trống",
                    new[] { nameof(TaskIds) }));
            }

            if (TaskIds != null && TaskIds.Any(id => id <= 0))
            {
                results.Add(new ValidationResult(
                    "Tất cả TaskIds phải > 0",
                    new[] { nameof(TaskIds) }));
            }

            if (TaskIds != null && TaskIds.Count > 100)
            {
                results.Add(new ValidationResult(
                    "Không được cập nhật quá 100 tasks cùng lúc",
                    new[] { nameof(TaskIds) }));
            }

            if (IsBlocked == true && string.IsNullOrWhiteSpace(BlockReason))
            {
                results.Add(new ValidationResult(
                    "Khi IsBlocked = true, BlockReason không được để trống",
                    new[] { nameof(BlockReason) }));
            }

            if (AssignedToId.HasValue && AssignedToId.Value <= 0)
            {
                results.Add(new ValidationResult(
                    "AssignedToId phải > 0",
                    new[] { nameof(AssignedToId) }));
            }

            // Validate không có update nào được chỉ định
            if (!Status.HasValue && !Priority.HasValue && !AssignedToId.HasValue &&
                !IsBlocked.HasValue && TagsToAdd.Count == 0 && TagsToRemove.Count == 0)
            {
                results.Add(new ValidationResult(
                    "Phải chỉ định ít nhất một trường để cập nhật",
                    new[] { nameof(Status), nameof(Priority), nameof(AssignedToId) }));
            }

            return results;
        }
    }
}
