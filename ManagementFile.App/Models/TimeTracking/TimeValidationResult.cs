using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Models.TimeTracking
{
    /// <summary>
    /// Kết quả validation thời gian
    /// </summary>
    public class TimeValidationResult
    {
        public bool IsValid { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; }
        public List<string> Details { get; set; } = new List<string>();
        public DateTime? ConflictDate { get; set; }
        public TimeSpan? ConflictTime { get; set; }
    }

    public enum ValidationSeverity
    {
        Info,       // Thông tin
        Warning,    // Cảnh báo (có thể bỏ qua)
        Error,      // Lỗi (không thể lưu)
        Critical    // Nghiêm trọng (vi phạm quy định)
    }

    /// <summary>
    /// Cấu hình validation rules
    /// </summary>
    public class TimeValidationConfig
    {
        // Giới hạn giờ
        public decimal MaxHoursPerDay { get; set; } = 12m;
        public decimal StandardHoursPerDay { get; set; } = 8m;
        public decimal MaxHoursPerWeek { get; set; } = 60m;
        public decimal StandardHoursPerWeek { get; set; } = 40m;

        // Cảnh báo
        public bool WarnOnWeekend { get; set; } = true;
        public bool WarnOnOvertime { get; set; } = true;
        public bool WarnOnUndertime { get; set; } = true;
        public decimal UndertimeThreshold { get; set; } = 6m; // < 6h/ngày

        // Kiểm tra conflict
        public bool CheckTimeOverlap { get; set; } = true;
        public bool CheckFutureDate { get; set; } = true;
        public int MaxDaysInPast { get; set; } = 30; // Không cho nhập quá 30 ngày trước

        // Business rules
        public bool RequireTaskForEntry { get; set; } = true;
        public bool RequireDescriptionForLongEntries { get; set; } = true;
        public decimal LongEntryThreshold { get; set; } = 4m; // > 4h cần mô tả

        // Auto-fill
        public bool EnableAutoFill { get; set; } = true;
        public bool SuggestFromHistory { get; set; } = true;
    }
}
