using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks
{
    /// <summary>
    /// Request để cập nhật task
    /// </summary>
    public class UpdateTaskRequest : BaseTaskRequest
    {
        /// <summary>
        /// Status - Trạng thái nhiệm vụ
        /// </summary>
        public TaskStatuss Status { get; set; }

        /// <summary>
        /// Progress - Tiến độ hoàn thành (0-100%)
        /// </summary>
        [Range(0, 100, ErrorMessage = "Progress phải nằm trong khoảng 0-100")]
        public decimal Progress { get; set; } = 0;

        /// <summary>
        /// CompletedAt - Thời gian hoàn thành thực tế
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// CompletedBy - ID người đánh dấu hoàn thành
        /// ✅ Changed to nullable for consistency
        /// </summary>
        public int? CompletedBy { get; set; }

        /// <summary>
        /// IsBlocked - Task có bị block không
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// BlockReason - Lý do bị block
        /// </summary>
        [StringLength(500, ErrorMessage = "BlockReason không được vượt quá 500 ký tự")]
        public string BlockReason { get; set; } = "";

        public long Version { get; set; }

        /// <summary>
        /// Custom validation cho business rules
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Gọi base validation trước
            var results = base.Validate(validationContext).ToList();

            // ==================== PROGRESS & STATUS CONSISTENCY ====================
            if (Progress == 100 && Status != TaskStatuss.Completed)
            {
                results.Add(new ValidationResult(
                    "Khi Progress = 100%, Status phải là Completed",
                    new[] { nameof(Status), nameof(Progress) }));
            }

            if (Status == TaskStatuss.Completed && Progress < 100)
            {
                results.Add(new ValidationResult(
                    "Khi Status = Completed, Progress phải = 100%",
                    new[] { nameof(Status), nameof(Progress) }));
            }

            // ==================== COMPLETED FIELDS CONSISTENCY ====================
            if (Status == TaskStatuss.Completed)
            {
                if (!CompletedAt.HasValue)
                {
                    results.Add(new ValidationResult(
                        "Khi Status = Completed, CompletedAt không được để trống",
                        new[] { nameof(CompletedAt) }));
                }

                if (!CompletedBy.HasValue || CompletedBy.Value <= 0)
                {
                    results.Add(new ValidationResult(
                        "Khi Status = Completed, CompletedBy phải được set",
                        new[] { nameof(CompletedBy) }));
                }
            }
            else
            {
                // Status != Completed
                if (CompletedAt.HasValue)
                {
                    results.Add(new ValidationResult(
                        "CompletedAt chỉ được set khi Status = Completed",
                        new[] { nameof(CompletedAt) }));
                }

                if (CompletedBy.HasValue && CompletedBy.Value > 0)
                {
                    results.Add(new ValidationResult(
                        "CompletedBy chỉ được set khi Status = Completed",
                        new[] { nameof(CompletedBy) }));
                }
            }

            // ==================== BLOCKED STATUS CONSISTENCY ====================
            if (IsBlocked && string.IsNullOrWhiteSpace(BlockReason))
            {
                results.Add(new ValidationResult(
                    "Khi IsBlocked = true, BlockReason không được để trống",
                    new[] { nameof(BlockReason) }));
            }

            if (!IsBlocked && !string.IsNullOrWhiteSpace(BlockReason))
            {
                results.Add(new ValidationResult(
                    "Khi IsBlocked = false, BlockReason phải để trống",
                    new[] { nameof(BlockReason) }));
            }

            // ==================== DATE VALIDATIONS ====================
            if (CompletedAt.HasValue && CompletedAt.Value > DateTime.UtcNow.AddMinutes(5))
            {
                results.Add(new ValidationResult(
                    "CompletedAt không được trong tương lai (cho phép sai lệch 5 phút)",
                    new[] { nameof(CompletedAt) }));
            }

            // Validate CompletedAt >= StartDate nếu cả hai được set
            if (CompletedAt.HasValue && StartDate.HasValue && CompletedAt.Value < StartDate.Value)
            {
                results.Add(new ValidationResult(
                    "CompletedAt phải sau hoặc bằng StartDate",
                    new[] { nameof(CompletedAt), nameof(StartDate) }));
            }

            // ==================== BUSINESS LOGIC VALIDATIONS ====================
            // Không thể hoàn thành task đang bị block
            if (Status == TaskStatuss.Completed && IsBlocked)
            {
                results.Add(new ValidationResult(
                    "Không thể hoàn thành task đang bị block",
                    new[] { nameof(Status), nameof(IsBlocked) }));
            }

            return results;
        }
    }
}
