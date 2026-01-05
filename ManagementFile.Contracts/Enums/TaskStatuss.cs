using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// TaskStatus - Trạng thái nhiệm vụ
    /// Workflow states cho task management với state machine pattern
    /// </summary>
    public enum TaskStatuss
    {
        /// <summary>All - Tất cả trạng thái (dùng cho filter)</summary>
        [Description("Tất cả trạng thái")]
        All = -1,

        /// <summary>Todo - Chưa bắt đầu, đang chờ thực hiện</summary>
        [Description("Chưa bắt đầu, đang chờ thực hiện")]
        Todo = 0,

        /// <summary>InProgress - Đang thực hiện, có người đang làm</summary>
        [Description("Đang thực hiện, có người đang làm")]
        InProgress = 1,

        /// <summary>InReview - Đang review, chờ phê duyệt</summary>
        [Description("Đang review, chờ phê duyệt")]
        InReview = 2,

        /// <summary>Testing - Đang testing, QA đang kiểm tra</summary>
        [Description("Đang testing, QA đang kiểm tra")]
        Testing = 3,

        /// <summary>Completed - Hoàn thành, đã được approve</summary>
        [Description("Hoàn thành, đã được approve")]
        Completed = 4,

        /// <summary>Blocked - Bị block, không thể tiến hành</summary>
        [Description("Bị block, không thể tiến hành")]
        Blocked = 5,

        /// <summary>OnHold - Tạm dừng, chờ điều kiện khác</summary>
        [Description("Tạm dừng, chờ điều kiện khác")]
        OnHold = 6,

        /// <summary>Cancelled - Hủy bỏ, không thực hiện nữa</summary>
        [Description("Hủy bỏ, không thực hiện nữa")]
        Cancelled = 7,

        /// <summary>Reopened - Mở lại sau khi đã completed</summary>
        [Description("Mở lại sau khi đã completed")]
        Reopened = 8
    }

    /// <summary>
    /// Extension methods cho TaskStatuss enum
    /// </summary>
    public static class TaskStatussExtensions
    {
        /// <summary>
        /// Lấy mô tả của trạng thái
        /// </summary>
        public static string GetDescription(this TaskStatuss status)
        {
            return TaskStatussHelper.GetDescription(status);
        }

        /// <summary>
        /// Lấy display name ngắn gọn
        /// </summary>
        public static string GetDisplayName(this TaskStatuss status)
        {
            switch (status)
            {
                case TaskStatuss.All: return "Tất cả";
                case TaskStatuss.Todo: return "Chờ làm";
                case TaskStatuss.InProgress: return "Đang làm";
                case TaskStatuss.InReview: return "Đang review";
                case TaskStatuss.Testing: return "Đang test";
                case TaskStatuss.Completed: return "Hoàn thành";
                case TaskStatuss.Blocked: return "Bị chặn";
                case TaskStatuss.OnHold: return "Tạm dừng";
                case TaskStatuss.Cancelled: return "Đã hủy";
                case TaskStatuss.Reopened: return "Mở lại";
                default: return "Không xác định";
            }
        }

        /// <summary>
        /// Lấy icon cho status
        /// </summary>
        public static string GetIcon(this TaskStatuss status)
        {
            return TaskStatussHelper.GetDisplayIcon(status);
        }

        /// <summary>
        /// Lấy màu hex cho status
        /// </summary>
        public static string GetHexColor(this TaskStatuss status)
        {
            switch (status)
            {
                case TaskStatuss.Todo: return "#6C757D";        // Gray
                case TaskStatuss.InProgress: return "#007BFF";  // Blue
                case TaskStatuss.InReview: return "#FFC107";    // Yellow
                case TaskStatuss.Testing: return "#20C997";     // Teal
                case TaskStatuss.Completed: return "#28A745";   // Green
                case TaskStatuss.Blocked: return "#DC3545";     // Red
                case TaskStatuss.OnHold: return "#FD7E14";      // Orange
                case TaskStatuss.Cancelled: return "#6C757D";   // Gray
                case TaskStatuss.Reopened: return "#6F42C1";    // Purple
                case TaskStatuss.All: return "#495057";         // Dark Gray
                default: return "#ADB5BD";
            }
        }

        public static (int, int, int) GetRgbColor(this TaskStatuss statuss)
        {
            var hex = statuss.GetHexColor();
            // Convert hex to RGB
            int r = Convert.ToInt32(hex.Substring(1, 2), 16);
            int g = Convert.ToInt32(hex.Substring(3, 2), 16);
            int b = Convert.ToInt32(hex.Substring(5, 2), 16);


            return (r, g, b);

        }

        /// <summary>
        /// Lấy CSS class cho status
        /// </summary>
        public static string GetCssClass(this TaskStatuss status)
        {
            switch (status)
            {
                case TaskStatuss.Todo: return "status-todo";
                case TaskStatuss.InProgress: return "status-inprogress";
                case TaskStatuss.InReview: return "status-inreview";
                case TaskStatuss.Testing: return "status-testing";
                case TaskStatuss.Completed: return "status-completed";
                case TaskStatuss.Blocked: return "status-blocked";
                case TaskStatuss.OnHold: return "status-onhold";
                case TaskStatuss.Cancelled: return "status-cancelled";
                case TaskStatuss.Reopened: return "status-reopened";
                case TaskStatuss.All: return "status-all";
                default: return "status-unknown";
            }
        }

        /// <summary>
        /// Kiểm tra có phải trạng thái active không
        /// </summary>
        public static bool IsActive(this TaskStatuss status)
        {
            return status == TaskStatuss.Todo ||
                   status == TaskStatuss.InProgress ||
                   status == TaskStatuss.InReview ||
                   status == TaskStatuss.Testing ||
                   status == TaskStatuss.Reopened;
        }

        /// <summary>
        /// Kiểm tra có phải trạng thái final không
        /// </summary>
        public static bool IsFinal(this TaskStatuss status)
        {
            return status == TaskStatuss.Completed ||
                   status == TaskStatuss.Cancelled;
        }

        /// <summary>
        /// Kiểm tra có phải trạng thái blocked không
        /// </summary>
        public static bool IsBlocked(this TaskStatuss status)
        {
            return status == TaskStatuss.Blocked ||
                   status == TaskStatuss.OnHold;
        }

        /// <summary>
        /// Kiểm tra có phải trạng thái in progress không
        /// </summary>
        public static bool IsInProgress(this TaskStatuss status)
        {
            return status == TaskStatuss.InProgress ||
                   status == TaskStatuss.InReview ||
                   status == TaskStatuss.Testing;
        }

        /// <summary>
        /// Lấy progress percentage cho status
        /// </summary>
        public static int GetProgressPercentage(this TaskStatuss status)
        {
            switch (status)
            {
                case TaskStatuss.Todo: return 0;
                case TaskStatuss.InProgress: return 25;
                case TaskStatuss.InReview: return 75;
                case TaskStatuss.Testing: return 90;
                case TaskStatuss.Completed: return 100;
                case TaskStatuss.Cancelled: return 0;
                case TaskStatuss.Blocked: return 0;
                case TaskStatuss.OnHold: return 0;
                case TaskStatuss.Reopened: return 10;
                default: return 0;
            }
        }

        /// <summary>
        /// Lấy danh sách trạng thái có thể chuyển đến
        /// </summary>
        public static List<TaskStatuss> GetValidTransitions(this TaskStatuss currentStatus)
        {
            switch (currentStatus)
            {
                case TaskStatuss.Todo:
                    return new List<TaskStatuss> { TaskStatuss.InProgress, TaskStatuss.Blocked, TaskStatuss.OnHold, TaskStatuss.Cancelled };

                case TaskStatuss.InProgress:
                    return new List<TaskStatuss> { TaskStatuss.InReview, TaskStatuss.Completed, TaskStatuss.Blocked, TaskStatuss.OnHold, TaskStatuss.Cancelled };

                case TaskStatuss.InReview:
                    return new List<TaskStatuss> { TaskStatuss.Testing, TaskStatuss.Completed, TaskStatuss.InProgress, TaskStatuss.Blocked };

                case TaskStatuss.Testing:
                    return new List<TaskStatuss> { TaskStatuss.Completed, TaskStatuss.InProgress, TaskStatuss.InReview, TaskStatuss.Blocked };

                case TaskStatuss.Completed:
                    return new List<TaskStatuss> { TaskStatuss.Reopened };

                case TaskStatuss.Blocked:
                    return new List<TaskStatuss> { TaskStatuss.Todo, TaskStatuss.InProgress, TaskStatuss.Cancelled };

                case TaskStatuss.OnHold:
                    return new List<TaskStatuss> { TaskStatuss.Todo, TaskStatuss.InProgress, TaskStatuss.Cancelled };

                case TaskStatuss.Cancelled:
                    return new List<TaskStatuss> { TaskStatuss.Todo, TaskStatuss.Reopened };

                case TaskStatuss.Reopened:
                    return new List<TaskStatuss> { TaskStatuss.InProgress, TaskStatuss.InReview, TaskStatuss.Testing };

                default:
                    return new List<TaskStatuss>();
            }
        }

        /// <summary>
        /// Kiểm tra có thể chuyển sang trạng thái mới không
        /// </summary>
        public static bool CanTransitionTo(this TaskStatuss currentStatus, TaskStatuss newStatus)
        {
            return currentStatus.GetValidTransitions().Contains(newStatus);
        }

        /// <summary>
        /// Lấy workflow phase
        /// </summary>
        public static string GetWorkflowPhase(this TaskStatuss status)
        {
            switch (status)
            {
                case TaskStatuss.Todo:
                case TaskStatuss.Reopened:
                    return "Planning";

                case TaskStatuss.InProgress:
                    return "Development";

                case TaskStatuss.InReview:
                    return "Review";

                case TaskStatuss.Testing:
                    return "Quality Assurance";

                case TaskStatuss.Completed:
                    return "Completed";

                case TaskStatuss.Blocked:
                case TaskStatuss.OnHold:
                    return "Blocked";

                case TaskStatuss.Cancelled:
                    return "Cancelled";

                default:
                    return "Unknown";
            }
        }
    }

    /// <summary>
    /// Helper class cho TaskStatuss
    /// </summary>
    public static class TaskStatussHelper
    {
        private static readonly Dictionary<TaskStatuss, string> StatusIcons = new Dictionary<TaskStatuss, string>
        {
            { TaskStatuss.Todo, "📋" },          // Todo
            { TaskStatuss.InProgress, "⏳" },    // In Progress
            { TaskStatuss.InReview, "🔍" },      // In Review
            { TaskStatuss.Testing, "🧪" },       // Testing
            { TaskStatuss.Completed, "✅" },     // Completed
            { TaskStatuss.Blocked, "⛔" },       // Blocked
            { TaskStatuss.OnHold, "⏸️" },        // On Hold
            { TaskStatuss.Cancelled, "❌" },     // Cancelled
            { TaskStatuss.Reopened, "🔄" },      // Reopened
            { TaskStatuss.All, "🎯" }            // All
        };

        private static readonly Dictionary<TaskStatuss, string> StatusColors = new Dictionary<TaskStatuss, string>
        {
            { TaskStatuss.Todo, "#6C757D" },
            { TaskStatuss.InProgress, "#007BFF" },
            { TaskStatuss.InReview, "#FFC107" },
            { TaskStatuss.Testing, "#20C997" },
            { TaskStatuss.Completed, "#28A745" },
            { TaskStatuss.Blocked, "#DC3545" },
            { TaskStatuss.OnHold, "#FD7E14" },
            { TaskStatuss.Cancelled, "#6C757D" },
            { TaskStatuss.Reopened, "#6F42C1" },
            { TaskStatuss.All, "#495057" }
        };

        /// <summary>
        /// Lấy mô tả của trạng thái nhiệm vụ
        /// </summary>
        public static string GetDescription(TaskStatuss status)
        {
            var type = typeof(TaskStatuss);
            var memInfo = type.GetMember(status.ToString());
            if (memInfo.Length > 0)
            {
                var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            return status.ToString();
        }

        /// <summary>
        /// Parse TaskStatuss từ string
        /// </summary>
        public static TaskStatuss GetTaskStatusFromString(string statusString)
        {
            if (string.IsNullOrWhiteSpace(statusString))
                return TaskStatuss.All;

            if (Enum.TryParse<TaskStatuss>(statusString, true, out var status))
            {
                return status;
            }

            // Try parse from display name
            foreach (TaskStatuss stat in Enum.GetValues(typeof(TaskStatuss)))
            {
                if (stat.GetDisplayName().Equals(statusString, StringComparison.OrdinalIgnoreCase))
                {
                    return stat;
                }
            }

            return TaskStatuss.All;
        }

        /// <summary>
        /// Lấy icon cho status
        /// </summary>
        public static string GetDisplayIcon(TaskStatuss status)
        {
            return StatusIcons.ContainsKey(status) ? StatusIcons[status] : "❓";
        }

        /// <summary>
        /// Lấy màu cho status
        /// </summary>
        public static string GetColor(TaskStatuss status)
        {
            return StatusColors.ContainsKey(status) ? StatusColors[status] : "#6C757D";
        }

        /// <summary>
        /// Lấy danh sách trạng thái để hiển thị trong ComboBox (loại bỏ All)
        /// </summary>
        public static ObservableCollection<TaskStatussItem> GetTaskStatusItems()
        {
            var items = new ObservableCollection<TaskStatussItem>();

            foreach (TaskStatuss status in Enum.GetValues(typeof(TaskStatuss)))
            {
                if (status == TaskStatuss.All) continue;

                items.Add(CreateStatusItem(status));
            }

            return items;
        }

        /// <summary>
        /// Lấy danh sách tất cả trạng thái bao gồm "All" để filter
        /// </summary>
        public static ObservableCollection<TaskStatussItem> GetAllTaskStatusItems()
        {
            var items = new ObservableCollection<TaskStatussItem>();

            foreach (TaskStatuss status in Enum.GetValues(typeof(TaskStatuss)))
            {
                items.Add(CreateStatusItem(status));
            }

            return items;
        }

        /// <summary>
        /// Lấy danh sách trạng thái active (đang làm việc)
        /// </summary>
        public static ObservableCollection<TaskStatussItem> GetActiveStatusItems()
        {
            var items = new ObservableCollection<TaskStatussItem>();

            foreach (TaskStatuss status in Enum.GetValues(typeof(TaskStatuss)))
            {
                if (status.IsActive())
                {
                    items.Add(CreateStatusItem(status));
                }
            }

            return items;
        }

        /// <summary>
        /// Lấy danh sách trạng thái có thể chuyển đến từ trạng thái hiện tại
        /// </summary>
        public static ObservableCollection<TaskStatussItem> GetTransitionStatusItems(TaskStatuss currentStatus)
        {
            var items = new ObservableCollection<TaskStatussItem>();
            var validTransitions = currentStatus.GetValidTransitions();

            foreach (var status in validTransitions)
            {
                items.Add(CreateStatusItem(status));
            }

            return items;
        }

        /// <summary>
        /// Tạo TaskStatussItem từ enum value
        /// </summary>
        private static TaskStatussItem CreateStatusItem(TaskStatuss status)
        {
            return new TaskStatussItem
            {
                Value = status,
                Description = GetDescription(status),
                Icon = GetDisplayIcon(status),
                Color = GetColor(status),
                IsActive = status.IsActive(),
                IsFinal = status.IsFinal(),
                IsBlocked = status.IsBlocked(),
                ProgressPercentage = status.GetProgressPercentage(),
                WorkflowPhase = status.GetWorkflowPhase()
            };
        }

        /// <summary>
        /// Thống kê trạng thái cho dashboard
        /// </summary>
        public static Dictionary<TaskStatuss, int> GetStatusStatistics(IEnumerable<TaskStatuss> statuses)
        {
            var stats = new Dictionary<TaskStatuss, int>();

            foreach (TaskStatuss status in Enum.GetValues(typeof(TaskStatuss)))
            {
                if (status != TaskStatuss.All)
                {
                    stats[status] = statuses.Count(s => s == status);
                }
            }

            return stats;
        }

        /// <summary>
        /// Tính toán completion rate
        /// </summary>
        public static double CalculateCompletionRate(IEnumerable<TaskStatuss> statuses)
        {
            var statusList = statuses.ToList();
            if (!statusList.Any()) return 0;

            var completedCount = statusList.Count(s => s == TaskStatuss.Completed);
            return (double)completedCount / statusList.Count * 100;
        }

        /// <summary>
        /// Tính toán blocked rate
        /// </summary>
        public static double CalculateBlockedRate(IEnumerable<TaskStatuss> statuses)
        {
            var statusList = statuses.ToList();
            if (!statusList.Any()) return 0;

            var blockedCount = statusList.Count(s => s.IsBlocked());
            return (double)blockedCount / statusList.Count * 100;
        }
    }

    /// <summary>
    /// Model cho Task Status trong ComboBox - Enhanced version
    /// </summary>
    public class TaskStatussItem
    {
        public TaskStatuss Value { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsFinal { get; set; }
        public bool IsBlocked { get; set; }
        public int ProgressPercentage { get; set; }
        public string WorkflowPhase { get; set; } = "";

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
        public string ToolTip => $"{Description}\nPhase: {WorkflowPhase}\nProgress: {ProgressPercentage}%";

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
            if (obj is TaskStatussItem other)
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