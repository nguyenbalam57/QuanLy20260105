using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Base request chung cho các thao tác với task
    /// </summary>
    public abstract class BaseTaskRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Title là bắt buộc")]
        [StringLength(200, ErrorMessage = "Title không được vượt quá 200 ký tự")]
        public string Title { get; set; } = "";

        [StringLength(5000, ErrorMessage = "Description không được vượt quá 5000 ký tự")]
        public string Description { get; set; } = "";

        public TaskPriority Priority { get; set; } = TaskPriority.Low;

        public Department TaskType { get; set; } = Department.OTHER;

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "EstimatedHours phải >= 0")]
        public decimal EstimatedHours { get; set; } = 0;

        /// <summary>
        /// AssignedToId - Nullable để phân biệt: null (unassigned) vs có giá trị (assigned)
        /// </summary>
        public int? AssignedToId { get; set; }

        /// <summary>
        /// ReporterId - Người report/tạo task
        /// </summary>
        public int? ReporterId { get; set; }

        /// <summary>
        /// ParentTaskId - ID của parent task nếu đây là subtask
        /// </summary>
        public int? ParentTaskId { get; set; }


        /// <summary>
        /// Lấy thêm những nội dung id user được liên quan đến
        /// </summary>
        public List<int> AssignedToIds { get; set; } = new List<int>();

        /// <summary>
        /// Tags - Danh sách tags của task
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Metadata bổ sung dưới dạng key-value pairs
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Base validation cho tất cả request
        /// </summary>
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate Title không chỉ chứa khoảng trắng
            if (string.IsNullOrWhiteSpace(Title))
            {
                results.Add(new ValidationResult(
                    "Title không được để trống hoặc chỉ chứa khoảng trắng",
                    new[] { nameof(Title) }));
            }

            // Validate DueDate >= StartDate nếu cả hai được set
            if (StartDate.HasValue && DueDate.HasValue && DueDate.Value < StartDate.Value)
            {
                results.Add(new ValidationResult(
                    "DueDate phải sau hoặc bằng StartDate",
                    new[] { nameof(DueDate), nameof(StartDate) }));
            }

            // Validate dates không ở quá xa trong tương lai (optional business rule)
            var maxFutureDate = DateTime.UtcNow.AddYears(5);
            if (StartDate.HasValue && StartDate.Value > maxFutureDate)
            {
                results.Add(new ValidationResult(
                    "StartDate không được quá 5 năm trong tương lai",
                    new[] { nameof(StartDate) }));
            }

            if (DueDate.HasValue && DueDate.Value > maxFutureDate)
            {
                results.Add(new ValidationResult(
                    "DueDate không được quá 5 năm trong tương lai",
                    new[] { nameof(DueDate) }));
            }

            // Validate Tags không chứa giá trị rỗng
            if (Tags != null && Tags.Any(tag => string.IsNullOrWhiteSpace(tag)))
            {
                results.Add(new ValidationResult(
                    "Tags không được chứa giá trị rỗng",
                    new[] { nameof(Tags) }));
            }

            // Validate Tags length
            if (Tags != null && Tags.Any(tag => tag.Length > 50))
            {
                results.Add(new ValidationResult(
                    "Mỗi tag không được vượt quá 50 ký tự",
                    new[] { nameof(Tags) }));
            }

            // ✅ NEW: Validate ParentTaskId không âm (nếu có giá trị)
            if (ParentTaskId.HasValue && ParentTaskId.Value <= 0)
            {
                results.Add(new ValidationResult(
                    "ParentTaskId phải là số dương hoặc null",
                    new[] { nameof(ParentTaskId) }));
            }

            // ✅ NEW: Validate AssignedToId không âm (nếu có giá trị)  
            if (AssignedToId.HasValue && AssignedToId.Value <= 0)
            {
                results.Add(new ValidationResult(
                    "AssignedToId phải là số dương hoặc null",
                    new[] { nameof(AssignedToId) }));
            }

            // ✅ NEW: Validate ReporterId không âm (nếu có giá trị)
            if (ReporterId.HasValue && ReporterId.Value <= 0)
            {
                results.Add(new ValidationResult(
                    "ReporterId phải là số dương hoặc null",
                    new[] { nameof(ReporterId) }));
            }

            // ✅ Validate EstimatedHours có giới hạn hợp lý
            if (EstimatedHours > 10000)
            {
                results.Add(new ValidationResult(
                    "EstimatedHours không được vượt quá 10,000 giờ (khoảng 1.14 năm)",
                    new[] { nameof(EstimatedHours) }));
            }

            return results;
        }
    }
}
