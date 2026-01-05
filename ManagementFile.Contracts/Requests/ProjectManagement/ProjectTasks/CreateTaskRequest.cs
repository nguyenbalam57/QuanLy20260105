using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để tạo task mới
    /// </summary>
    public class CreateTaskRequest : BaseTaskRequest
    {
        /// <summary>
        /// ProjectId - ID dự án chứa task này (bắt buộc khi tạo)
        /// </summary>
        [Required(ErrorMessage = "ProjectId là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ProjectId phải > 0")]
        public int ProjectId { get; set; }

        /// <summary>
        /// TaskCode - Mã task (optional, có thể auto-generate)
        /// </summary>
        [StringLength(50)]
        public string TaskCode { get; set; } = "";

        /// <summary>
        /// Tự động set progress = 0 khi tạo task mới
        /// </summary>
        public decimal Progress { get; private set; } = 0;

        /// <summary>
        /// Tự động set status = Todo khi tạo task mới
        /// </summary>
        public TaskStatuss Status { get; private set; } = TaskStatuss.Todo;

        /// <summary>
        /// Tự động set ActualHours = 0 khi tạo task mới
        /// </summary>
        public int ActualHours { get; private set; } = 0;

        /// <summary>
        /// Custom validation cho create
        /// </summary>
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Gọi base validation trước
            var results = base.Validate(validationContext).ToList();

            // Validate Progress phải = 0 khi tạo mới
            if (Progress != 0)
            {
                results.Add(new ValidationResult(
                    "Progress phải = 0 khi tạo task mới",
                    new[] { nameof(Progress) }));
            }

            // Validate Status phải = Todo khi tạo mới
            if (Status != TaskStatuss.Todo)
            {
                results.Add(new ValidationResult(
                    "Status phải = Todo khi tạo task mới",
                    new[] { nameof(Status) }));
            }

            // Validate ActualHours phải = 0 khi tạo mới
            if (ActualHours != 0)
            {
                results.Add(new ValidationResult(
                    "ActualHours phải = 0 khi tạo task mới",
                    new[] { nameof(ActualHours) }));
            }

            return results;
        }
    }
}
