using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để complete task
    /// </summary>
    public class CompleteTaskRequest : IValidatableObject
    {
        public DateTime? CompletedAt { get; set; }

        [Range(0, 100)]
        public decimal FinalProgress { get; set; } = 100;

        [Range(0, int.MaxValue)]
        public int? FinalActualHours { get; set; }

        [StringLength(1000)]
        public string CompletionNotes { get; set; } = "";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (CompletedAt.HasValue && CompletedAt.Value > DateTime.UtcNow.AddMinutes(5))
            {
                results.Add(new ValidationResult(
                    "CompletedAt không được trong tương lai",
                    new[] { nameof(CompletedAt) }));
            }

            if (FinalProgress != 100)
            {
                results.Add(new ValidationResult(
                    "FinalProgress phải = 100% khi complete task",
                    new[] { nameof(FinalProgress) }));
            }

            if (FinalActualHours.HasValue && FinalActualHours.Value < 0)
            {
                results.Add(new ValidationResult(
                    "FinalActualHours phải >= 0",
                    new[] { nameof(FinalActualHours) }));
            }

            return results;
        }
    }
}
