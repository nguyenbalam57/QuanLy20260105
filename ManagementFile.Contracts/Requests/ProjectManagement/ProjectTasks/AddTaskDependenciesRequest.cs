using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để thêm dependencies
    /// </summary>
    public class AddTaskDependenciesRequest : IValidatableObject
    {
        [Required]
        [MinLength(1)]
        public List<int> DependentTaskIds { get; set; } = new List<int>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (DependentTaskIds == null || DependentTaskIds.Count == 0)
            {
                results.Add(new ValidationResult(
                    "DependentTaskIds không được để trống",
                    new[] { nameof(DependentTaskIds) }));
            }

            if (DependentTaskIds != null && DependentTaskIds.Any(id => id <= 0))
            {
                results.Add(new ValidationResult(
                    "Tất cả DependentTaskIds phải > 0",
                    new[] { nameof(DependentTaskIds) }));
            }

            if (DependentTaskIds != null && DependentTaskIds.Distinct().Count() != DependentTaskIds.Count)
            {
                results.Add(new ValidationResult(
                    "DependentTaskIds không được chứa giá trị trùng lặp",
                    new[] { nameof(DependentTaskIds) }));
            }

            return results;
        }
    }
}
