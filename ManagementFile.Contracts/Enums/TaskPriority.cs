using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// TaskPriority - Độ ưu tiên nhiệm vụ
    /// Định nghĩa mức độ quan trọng và tính khẩn cấp của task trong hệ thống quản lý công việc
    /// Áp dụng ma trận Eisenhower (Important vs Urgent) và priority scheduling algorithms
    /// Hỗ trợ resource allocation và deadline management
    /// </summary>
    public enum TaskPriority
    {

        /// <summary>Low - Độ ưu tiên thấp</summary>
        /// <remarks>
        /// Nhiệm vụ có thể trích hoãn:
        /// - Không ảnh hưởng đến business critical operations
        /// - Có thể làm trong thời gian rảnh rỗi (nice-to-have)
        /// - Thường là improvements, optimizations, documentation
        /// - SLA: 2-4 tuần hoặc không có deadline cứng
        /// - Có thể bị push back nếu có priority cao hơn
        /// - Thích hợp cho junior developers hoặc learning tasks
        /// </remarks>
        [Description("Thấp - có thể hoãn lại")]
        Low = 0,

        /// <summary>Normal - Độ ưu tiên bình thường</summary>
        /// <remarks>
        /// Nhiệm vụ thường ngày, workflow chuẩn:
        /// - Công việc theo kế hoạch định sẵn
        /// - Phần của sprint backlog hoặc planned deliverables
        /// - Cần hoàn thành đúng timeline nhưng không gấp
        /// - SLA: 1-2 tuần tùy thuộc complexity
        /// - Balance giữa quality và timeline
        /// - Majority của development tasks
        /// </remarks>
        [Description("Bình thường - theo kế hoạch")]
        Normal = 1,

        /// <summary>Medium - Độ ưu tiên trung bình</summary>
        /// <remarks>
        /// Nhiệm vụ quan trọng hơn normal:
        /// - Features có impact đến user experience
        /// - Bug fixes ảnh hưởng workflow
        /// - Performance improvements cần thiết
        /// - SLA: 5-10 ngày
        /// - Cần attention nhưng không urgent
        /// </remarks>
        [Description("Trung bình - cần chú ý")]
        Medium = 2,

        /// <summary>High - Độ ưu tiên cao</summary>
        /// <remarks>
        /// Nhiệm vụ quan trọng cần ưu tiên:
        /// - Liên quan đến major features hoặc business objectives
        /// - Blocking other tasks hoặc dependencies
        /// - Customer-facing issues ảnh hưởng user experience
        /// - SLA: 3-7 ngày, cần fast-track
        /// - Require experienced developers
        /// - May need overtime hoặc additional resources
        /// </remarks>
        [Description("Cao - cần ưu tiên xử lý")]
        High = 3,

        /// <summary>Critical - Cấp thiết</summary>
        /// <remarks>
        /// Nhiệm vụ mission-critical:
        /// - Security vulnerabilities cần patch ngay
        /// - Production bugs ảnh hưởng core functionality
        /// - Compliance issues có thể dẫn đến legal problems
        /// - Major system outages hoặc data corruption
        /// - SLA: 24-72 giờ, all-hands-on-deck
        /// - Override other tasks, reallocate resources
        /// - Require senior developers và immediate attention
        /// </remarks>
        [Description("Cấp thiết - phải xử lý ngay")]
        Critical = 4,

        /// <summary>Emergency - Khẩn cấp tối đa</summary>
        /// <remarks>
        /// Tình huống khẩn cấp, nguy hiểm tức thì:
        /// - Complete system failure, service down
        /// - Data breach hoặc security incident đang diễn ra
        /// - Legal injunction hoặc regulatory compliance violations
        /// - Financial losses đang accumulating real-time
        /// - SLA: Immediate response, 24/7 availability required
        /// - Drop everything else, war room mode
        /// - All senior staff mobilized, executive involvement
        /// - May require vendor escalation hoặc external expertise
        /// </remarks>
        [Description("Khẩn cấp - dừng mọi việc để xử lý")]
        Emergency = 5,


        [Description("Tất cả")]
        All = 99,
    }

    /// <summary>
    /// Extension methods cho TaskPriority enum
    /// </summary>
    public static class TaskPriorityExtensions
    {
        /// <summary>
        /// Lấy mô tả của độ ưu tiên
        /// </summary>
        public static string GetDescription(this TaskPriority priority)
        {
            return TaskPriorityHelper.GetDescription(priority);
        }

        /// <summary>
        /// Lấy display name ngắn gọn
        /// </summary>
        public static string GetDisplayName(this TaskPriority priority)
        {
            switch (priority)
            {
                case TaskPriority.Low: return "Thấp";
                case TaskPriority.Normal: return "Bình thường";
                case TaskPriority.Medium: return "Trung bình";
                case TaskPriority.High: return "Cao";
                case TaskPriority.Critical: return "Cấp thiết";
                case TaskPriority.Emergency: return "Khẩn cấp";
                default: return "Tất cả";
            }
        }

        /// <summary>
        /// Lấy icon cho priority
        /// </summary>
        public static string GetIcon(this TaskPriority priority)
        {
            return TaskPriorityHelper.GetDisplayIcon(priority);
        }

        /// <summary>
        /// Lấy màu hex cho priority
        /// </summary>
        public static string GetHexColor(this TaskPriority priority)
        {
            switch (priority)
            {
                case TaskPriority.Low: return "#28A745";        // Green
                case TaskPriority.Normal: return "#6C757D";     // Gray
                case TaskPriority.Medium: return "#FFC107";     // Yellow
                case TaskPriority.High: return "#FD7E14";       // Orange
                case TaskPriority.Critical: return "#DC3545";   // Red
                case TaskPriority.Emergency: return "#6F42C1";  // Purple
                default: return "#6C757D";
            }
        }

        public static (int, int, int) GetRgbColor(this TaskPriority priority)
        {
            var hex = priority.GetHexColor();
            // Convert hex to RGB
            int r = Convert.ToInt32(hex.Substring(1, 2), 16);
            int g = Convert.ToInt32(hex.Substring(3, 2), 16);
            int b = Convert.ToInt32(hex.Substring(5, 2), 16);

            
            return (r, g, b);

        }

        /// <summary>
        /// Lấy CSS class cho priority
        /// </summary>
        public static string GetCssClass(this TaskPriority priority)
        {
            switch (priority)
            {
                case TaskPriority.Low: return "priority-low";
                case TaskPriority.Normal: return "priority-normal";
                case TaskPriority.Medium: return "priority-medium";
                case TaskPriority.High: return "priority-high";
                case TaskPriority.Critical: return "priority-critical";
                case TaskPriority.Emergency: return "priority-emergency";
                default: return "priority-unknown";
            }
        }

        /// <summary>
        /// Lấy weight/số thứ tự cho sorting
        /// </summary>
        public static int GetWeight(this TaskPriority priority)
        {
            return (int)priority;
        }

        /// <summary>
        /// Kiểm tra có phải priority cao không (High, Critical, Emergency)
        /// </summary>
        public static bool IsHighPriority(this TaskPriority priority)
        {
            return priority >= TaskPriority.High;
        }

        /// <summary>
        /// Kiểm tra có phải priority thấp không (Low, Normal)
        /// </summary>
        public static bool IsLowPriority(this TaskPriority priority)
        {
            return priority <= TaskPriority.Normal;
        }

        /// <summary>
        /// Kiểm tra có phải urgent priority không (Critical, Emergency)
        /// </summary>
        public static bool IsUrgent(this TaskPriority priority)
        {
            return priority >= TaskPriority.Critical;
        }

        /// <summary>
        /// Lấy SLA deadline theo ngày
        /// </summary>
        public static int GetSlaInDays(this TaskPriority priority)
        {
            switch (priority)
            {
                case TaskPriority.Emergency: return 1;      // 24 hours
                case TaskPriority.Critical: return 3;       // 72 hours
                case TaskPriority.High: return 7;           // 1 week
                case TaskPriority.Medium: return 10;        // 10 days
                case TaskPriority.Normal: return 14;        // 2 weeks
                case TaskPriority.Low: return 30;           // 1 month
                default: return 14;
            }
        }

        /// <summary>
        /// Lấy escalation level
        /// </summary>
        public static string GetEscalationLevel(this TaskPriority priority)
        {
            switch (priority)
            {
                case TaskPriority.Emergency: return "Executive/C-Level";
                case TaskPriority.Critical: return "Senior Management";
                case TaskPriority.High: return "Team Lead/Manager";
                case TaskPriority.Medium: return "Project Manager";
                case TaskPriority.Normal: return "Team Lead";
                case TaskPriority.Low: return "Self-managed";
                default: return "Unknown";
            }
        }
    }

    /// <summary>
    /// Helper class cho TaskPriority
    /// </summary>
    public static class TaskPriorityHelper
    {
        private static readonly Dictionary<TaskPriority, string> PriorityIcons = new Dictionary<TaskPriority, string>
        {
            { TaskPriority.Low, "🔵" },          // Blue circle for low
            { TaskPriority.Normal, "🟢" },       // Green circle for normal
            { TaskPriority.Medium, "🟡" },       // Yellow circle for medium
            { TaskPriority.High, "🟠" },         // Orange circle for high
            { TaskPriority.Critical, "🔴" },     // Red circle for critical
            { TaskPriority.Emergency, "🚨" },    // Siren for emergency
        };

        private static readonly Dictionary<TaskPriority, string> PriorityColors = new Dictionary<TaskPriority, string>
        {
            { TaskPriority.Low, "#28A745" },
            { TaskPriority.Normal, "#6C757D" },
            { TaskPriority.Medium, "#FFC107" },
            { TaskPriority.High, "#FD7E14" },
            { TaskPriority.Critical, "#DC3545" },
            { TaskPriority.Emergency, "#6F42C1" },
        };

        /// <summary>
        /// Lấy mô tả của độ ưu tiên nhiệm vụ
        /// </summary>
        public static string GetDescription(TaskPriority priority)
        {
            var type = typeof(TaskPriority);
            var memInfo = type.GetMember(priority.ToString());
            if (memInfo.Length > 0)
            {
                var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            return priority.ToString();
        }

        /// <summary>
        /// Parse TaskPriority từ description
        /// </summary>
        public static TaskPriority FromDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return TaskPriority.All;

            foreach (var field in typeof(TaskPriority).GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                    {
                        return (TaskPriority)field.GetValue(null);
                    }
                }
                else
                {
                    if (field.Name.Equals(description, StringComparison.OrdinalIgnoreCase))
                    {
                        return (TaskPriority)field.GetValue(null);
                    }
                }
            }
            return TaskPriority.All;
        }

        /// <summary>
        /// Parse TaskPriority từ string
        /// </summary>
        public static TaskPriority GetEnumTaskPriority(string taskPriority)
        {
            if (string.IsNullOrWhiteSpace(taskPriority))
                return TaskPriority.All;

            if (Enum.TryParse<TaskPriority>(taskPriority, true, out var priority))
            {
                return priority;
            }

            // Try parse from description
            return FromDescription(taskPriority);
        }

        /// <summary>
        /// Lấy icon cho priority
        /// </summary>
        public static string GetDisplayIcon(TaskPriority priority)
        {
            return PriorityIcons.ContainsKey(priority) ? PriorityIcons[priority] : "❓";
        }

        /// <summary>
        /// Lấy màu cho priority
        /// </summary>
        public static string GetColor(TaskPriority priority)
        {
            return PriorityColors.ContainsKey(priority) ? PriorityColors[priority] : "#6C757D";
        }

        /// <summary>
        /// Lấy danh sách các độ ưu tiên để hiển thị trong ComboBox (loại bỏ Unspecified)
        /// </summary>
        public static ObservableCollection<TaskPriorityItem> GetTaskPriorityItems()
        {
            var items = new ObservableCollection<TaskPriorityItem>();

            foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)).Cast<TaskPriority>())
            {
                if (priority == TaskPriority.All) continue;

                items.Add(CreatePriorityItem(priority));
            }

            return items;
        }

        /// <summary>
        /// Lấy danh sách tất cả độ ưu tiên bao gồm "Unspecified" để filter
        /// </summary>
        public static ObservableCollection<TaskPriorityItem> GetAllTaskPriorityItems()
        {
            var items = new ObservableCollection<TaskPriorityItem>();

            foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)).Cast<TaskPriority>())
            {
                items.Add(CreatePriorityItem(priority));
            }

            return items;
        }

        /// <summary>
        /// Lấy danh sách priority items được sắp xếp theo weight
        /// </summary>
        public static ObservableCollection<TaskPriorityItem> GetSortedTaskPriorityItems()
        {
            var items = GetTaskPriorityItems();
            var sortedItems = new ObservableCollection<TaskPriorityItem>(
                items.OrderBy(x => x.Value.GetWeight()));

            return sortedItems;
        }

        /// <summary>
        /// Tạo TaskPriorityItem từ enum value
        /// </summary>
        private static TaskPriorityItem CreatePriorityItem(TaskPriority priority)
        {
            return new TaskPriorityItem
            {
                Value = priority,
                Description = GetDescription(priority),
                Icon = GetDisplayIcon(priority),
                Color = GetColor(priority),
                Weight = priority.GetWeight(),
                IsHighPriority = priority.IsHighPriority(),
                IsUrgent = priority.IsUrgent(),
                SlaInDays = priority.GetSlaInDays()
            };
        }

        /// <summary>
        /// Tính toán priority score cho task scheduling
        /// </summary>
        public static double CalculatePriorityScore(TaskPriority priority, DateTime? dueDate = null)
        {
            var baseScore = priority.GetWeight() * 10;

            if (dueDate.HasValue)
            {
                var daysUntilDue = (dueDate.Value - DateTime.Now).TotalDays;
                if (daysUntilDue < 0) // Overdue
                {
                    baseScore += (int)(Math.Abs(daysUntilDue) * 2); // Penalty for overdue
                }
                else if (daysUntilDue < 3) // Due soon
                {
                    baseScore += (int)((3 - daysUntilDue) * 1.5);
                }
            }

            return baseScore;
        }

        /// <summary>
        /// Gợi ý escalation dựa trên priority và thời gian
        /// </summary>
        public static bool ShouldEscalate(TaskPriority priority, DateTime createdAt, DateTime? dueDate = null)
        {
            var ageInDays = (DateTime.Now - createdAt).TotalDays;
            var slaInDays = priority.GetSlaInDays();

            if (slaInDays > 0 && ageInDays > slaInDays * 0.8) // 80% of SLA reached
            {
                return true;
            }

            if (dueDate.HasValue && DateTime.Now > dueDate.Value)
            {
                return true; // Overdue
            }

            return false;
        }
    }

    /// <summary>
    /// Model cho Task Priority trong ComboBox - Enhanced version
    /// </summary>
    public class TaskPriorityItem
    {
        public TaskPriority Value { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public int Weight { get; set; }
        public bool IsHighPriority { get; set; }
        public bool IsUrgent { get; set; }
        public int SlaInDays { get; set; }

        /// <summary>
        /// Text hiển thị trong ComboBox
        /// </summary>
        public string DisplayText => $"{Icon} {Value.GetDisplayName()}";

        /// <summary>
        /// Text hiển thị đầy đủ với description
        /// </summary>
        public string FullDisplayText => $"{Icon} {Description}";

        /// <summary>
        /// Tooltip text
        /// </summary>
        public string ToolTip => $"{Description}\nSLA: {SlaInDays} ngày\nEscalation: {Value.GetEscalationLevel()}";

        /// <summary>
        /// Override ToString for debugging
        /// </summary>
        public override string ToString()
        {
            return DisplayText;
        }

        /// <summary>
        /// Override Equals for comparison
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is TaskPriorityItem other)
            {
                return Value == other.Value;
            }
            return false;
        }

        /// <summary>
        /// Override GetHashCode
        /// </summary>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}