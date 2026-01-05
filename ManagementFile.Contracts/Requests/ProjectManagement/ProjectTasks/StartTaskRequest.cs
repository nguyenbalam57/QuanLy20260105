using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để start task
    /// </summary>
    public class StartTaskRequest
    {
        public DateTime? StartDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (StartDate.HasValue && StartDate.Value > DateTime.UtcNow.AddDays(7))
            {
                results.Add(new ValidationResult(
                    "StartDate không được quá 7 ngày trong tương lai",
                    new[] { nameof(StartDate) }));
            }

            return results;
        }
    }
}
