using ManagementFile.Contracts.Enums.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// ProjectStatus - Trạng thái dự án trong vòng đời quản lý dự án
    /// Định nghĩa các giai đoạn từ khởi tạo đến kết thúc của một dự án
    /// Hỗ trợ workflow management và project lifecycle tracking
    /// </summary>
    public enum ProjectStatus
    {
        All = -1,
        /// <summary>Planning - Giai đoạn lập kế hoạch</summary>
        [Description("Đang lập kế hoạch và chuẩn bị")]
        Planning = 0,

        /// <summary>Active - Giai đoạn thực hiện</summary>
        [Description("Đang thực hiện tích cực")]
        Active = 1,

        /// <summary>OnHold - Tạm dừng tạm thời</summary>
        [Description("Tạm dừng, chờ điều kiện phù hợp")]
        OnHold = 2,

        /// <summary>Completed - Hoàn thành thành công</summary>
        [Description("Hoàn thành thành công")]
        Completed = 3,

        /// <summary>Cancelled - Hủy bỏ trước khi hoàn thành</summary>
        [Description("Đã hủy bỏ")]
        Cancelled = 4,

        /// <summary>Archived - Lưu trữ dài hạn</summary>
        [Description("Đã lưu trữ")]
        Archived = 5
    }

    /// <summary>
    /// Project transition rules và business logic
    /// </summary>
    public static class ProjectStatusHelper
    {
        /// <summary>
        /// Định nghĩa các chuyển đổi hợp lệ giữa các trạng thái
        /// </summary>
        private static readonly Dictionary<ProjectStatus, ProjectStatus[]> _validTransitions =
            new Dictionary<ProjectStatus, ProjectStatus[]>
            {
                [ProjectStatus.Planning] = new[]
                {
                    ProjectStatus.Active,
                    ProjectStatus.OnHold,
                    ProjectStatus.Cancelled
                },

                [ProjectStatus.Active] = new[]
                {
                    ProjectStatus.OnHold,
                    ProjectStatus.Completed,
                    ProjectStatus.Cancelled
                },

                [ProjectStatus.OnHold] = new[]
                {
                    ProjectStatus.Active,
                    ProjectStatus.Cancelled
                },

                [ProjectStatus.Completed] = new[]
                {
                    ProjectStatus.Archived
                },

                [ProjectStatus.Cancelled] = new[]
                {
                    ProjectStatus.Archived
                },

                [ProjectStatus.Archived] = new ProjectStatus[] { }
            };

        /// <summary>
        /// Các trạng thái được coi là "active" (đang diễn ra)
        /// </summary>
        public static readonly ProjectStatus[] ActiveStatuses =
        {
            ProjectStatus.Planning,
            ProjectStatus.Active,
            ProjectStatus.OnHold
        };

        /// <summary>
        /// Các trạng thái được coi là "closed" (đã kết thúc)
        /// </summary>
        public static readonly ProjectStatus[] ClosedStatuses =
        {
            ProjectStatus.Completed,
            ProjectStatus.Cancelled,
            ProjectStatus.Archived
        };

        /// <summary>
        /// Kiểm tra có thể chuyển từ trạng thái này sang trạng thái khác không
        /// </summary>
        public static bool CanTransitionTo(ProjectStatus fromStatus, ProjectStatus toStatus)
        {
            if (!_validTransitions.TryGetValue(fromStatus, out ProjectStatus[] allowedTransitions))
                return false;

            return allowedTransitions.Contains(toStatus);
        }

        /// <summary>
        /// Lấy danh sách các trạng thái có thể chuyển đến
        /// </summary>
        public static ProjectStatus[] GetValidTransitions(ProjectStatus currentStatus)
        {
            return _validTransitions.TryGetValue(currentStatus, out ProjectStatus[] transitions)
                ? transitions : new ProjectStatus[0];
        }

        /// <summary>
        /// Kiểm tra project có đang active không
        /// </summary>
        public static bool IsActive(ProjectStatus status)
        {
            return ActiveStatuses.Contains(status);
        }

        /// <summary>
        /// Kiểm tra project có đã closed không
        /// </summary>
        public static bool IsClosed(ProjectStatus status)
        {
            return ClosedStatuses.Contains(status);
        }

        /// <summary>
        /// Kiểm tra có thể edit project không (chỉ khi đang active)
        /// </summary>
        public static bool CanEdit(ProjectStatus status)
        {
            return status == ProjectStatus.Planning || status == ProjectStatus.Active;
        }

        /// <summary>
        /// Kiểm tra có thể assign tasks không
        /// </summary>
        public static bool CanAssignTasks(ProjectStatus status)
        {
            return status == ProjectStatus.Active;
        }

        /// <summary>
        /// Lấy tên hiển thị từ enum value
        /// </summary>
        public static string GetName(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All: return "Tất cả";
                case ProjectStatus.Planning: return "Lập kế hoạch";
                case ProjectStatus.Active: return "Đang thực hiện";
                case ProjectStatus.OnHold: return "Tạm dừng";
                case ProjectStatus.Completed: return "Hoàn thành";
                case ProjectStatus.Cancelled: return "Đã hủy";
                case ProjectStatus.Archived: return "Lưu trữ";
                default: return status.ToString();
            }
        }

        /// <summary>
        /// Lấy short name (viết tắt)
        /// </summary>
        public static string GetShortName(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All: return "ALL";
                case ProjectStatus.Planning: return "PLAN";
                case ProjectStatus.Active: return "ACTV";
                case ProjectStatus.OnHold: return "HOLD";
                case ProjectStatus.Completed: return "DONE";
                case ProjectStatus.Cancelled: return "CNCL";
                case ProjectStatus.Archived: return "ARCH";
                default: return status.ToString();
            }
        }

        /// <summary>
        /// Lấy description đầy đủ
        /// </summary>
        public static string GetDescription(ProjectStatus status)
        {
            var field = status.GetType().GetField(status.ToString());
            var description = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                .Cast<DescriptionAttribute>()
                                .FirstOrDefault()?.Description
                            ?? status.ToString();
            return description;
        }

        /// <summary>
        /// Lấy display text (icon + description)
        /// </summary>
        public static string GetDisplayText(ProjectStatus status)
        {
            var icon = GetDisplayIcon(status);
            var description = GetDescription(status);
            return string.Format("{0} {1}", icon, description);
        }

        /// <summary>
        /// Lấy full display text (icon + name + description)
        /// </summary>
        public static string GetFullDisplayText(ProjectStatus status)
        {
            var icon = GetDisplayIcon(status);
            var name = GetName(status);
            var description = GetDescription(status);
            return string.Format("{0} {1} - {2}", icon, name, description);
        }

        /// <summary>
        /// Lấy màu sắc cho hiển thị UI (Hex)
        /// </summary>
        public static string GetDisplayColor(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All: return "#6C757D";
                case ProjectStatus.Planning: return "#FFA500";
                case ProjectStatus.Active: return "#28A745";
                case ProjectStatus.OnHold: return "#FFC107";
                case ProjectStatus.Completed: return "#007BFF";
                case ProjectStatus.Cancelled: return "#DC3545";
                case ProjectStatus.Archived: return "#6C757D";
                default: return "#000000";
            }
        }

        /// <summary>
        /// Lấy màu sắc RGB
        /// </summary>
        public static RgbColor GetRgbColor(this ProjectStatus status)
        {
            var hex = GetDisplayColor(status);
            int r = Convert.ToInt32(hex.Substring(1, 2), 16);
            int g = Convert.ToInt32(hex.Substring(3, 2), 16);
            int b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return new RgbColor(r, g, b);
        }

        /// <summary>
        /// Lấy màu background (lighter)
        /// </summary>
        public static string GetBackgroundColor(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All: return "#E9ECEF";
                case ProjectStatus.Planning: return "#FFE5B4";
                case ProjectStatus.Active: return "#D4EDDA";
                case ProjectStatus.OnHold: return "#FFF3CD";
                case ProjectStatus.Completed: return "#CCE5FF";
                case ProjectStatus.Cancelled: return "#F8D7DA";
                case ProjectStatus.Archived: return "#E9ECEF";
                default: return "#FFFFFF";
            }
        }

        /// <summary>
        /// Lấy màu border
        /// </summary>
        public static string GetBorderColor(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All: return "#ADB5BD";
                case ProjectStatus.Planning: return "#FFD700";
                case ProjectStatus.Active: return "#20C997";
                case ProjectStatus.OnHold: return "#FFC107";
                case ProjectStatus.Completed: return "#0056B3";
                case ProjectStatus.Cancelled: return "#BD2130";
                case ProjectStatus.Archived: return "#495057";
                default: return "#000000";
            }
        }

        /// <summary>
        /// Lấy icon cho hiển thị UI
        /// </summary>
        public static string GetDisplayIcon(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All: return "📊";
                case ProjectStatus.Planning: return "📋";
                case ProjectStatus.Active: return "⚡";
                case ProjectStatus.OnHold: return "⏸️";
                case ProjectStatus.Completed: return "✅";
                case ProjectStatus.Cancelled: return "❌";
                case ProjectStatus.Archived: return "📦";
                default: return "❓";
            }
        }

        /// <summary>
        /// Lấy alternative icon (cho variation)
        /// </summary>
        public static string GetAlternativeIcon(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All: return "🗂️";
                case ProjectStatus.Planning: return "🗓️";
                case ProjectStatus.Active: return "🚀";
                case ProjectStatus.OnHold: return "⏰";
                case ProjectStatus.Completed: return "🎉";
                case ProjectStatus.Cancelled: return "🚫";
                case ProjectStatus.Archived: return "🗄️";
                default: return "❓";
            }
        }

        /// <summary>
        /// Lấy emoji status
        /// </summary>
        public static string GetStatusEmoji(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All: return "📂";
                case ProjectStatus.Planning: return "🎯";
                case ProjectStatus.Active: return "💪";
                case ProjectStatus.OnHold: return "⏳";
                case ProjectStatus.Completed: return "🏆";
                case ProjectStatus.Cancelled: return "💔";
                case ProjectStatus.Archived: return "💾";
                default: return "❓";
            }
        }

        /// <summary>
        /// Lấy tooltip text
        /// </summary>
        public static string GetTooltip(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.All:
                    return "Hiển thị tất cả dự án";
                case ProjectStatus.Planning:
                    return "📋 Giai đoạn lập kế hoạch\n" +
                           "• Xác định scope và requirements\n" +
                           "• Phân bổ nguồn lực\n" +
                           "• Lập kế hoạch chi tiết";
                case ProjectStatus.Active:
                    return "⚡ Đang thực hiện tích cực\n" +
                           "• Teams đang làm việc\n" +
                           "• Tasks được assign và tracking\n" +
                           "• Progress được monitor";
                case ProjectStatus.OnHold:
                    return "⏸️ Tạm dừng tạm thời\n" +
                           "• Chờ phê duyệt hoặc điều kiện\n" +
                           "• Có thể resume sau\n" +
                           "• Resources tạm ngừng";
                case ProjectStatus.Completed:
                    return "✅ Hoàn thành thành công\n" +
                           "• Đạt được objectives\n" +
                           "• Deliverables được accept\n" +
                           "• Documentation hoàn tất";
                case ProjectStatus.Cancelled:
                    return "❌ Đã hủy bỏ\n" +
                           "• Không còn business value\n" +
                           "• Resources được reallocate\n" +
                           "• Cần close-out activities";
                case ProjectStatus.Archived:
                    return "📦 Đã lưu trữ\n" +
                           "• Chỉ để tham khảo\n" +
                           "• Historical data\n" +
                           "• Không có activities";
                default:
                    return status.ToString();
            }
        }

        /// <summary>
        /// Lấy sort order (thứ tự sắp xếp)
        /// </summary>
        public static int GetSortOrder(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.Active: return 1;
                case ProjectStatus.Planning: return 2;
                case ProjectStatus.OnHold: return 3;
                case ProjectStatus.Completed: return 4;
                case ProjectStatus.Cancelled: return 5;
                case ProjectStatus.Archived: return 6;
                case ProjectStatus.All: return 0;
                default: return 999;
            }
        }

        /// <summary>
        /// Lấy badge style
        /// </summary>
        public static string GetBadgeStyle(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.Planning: return "badge-warning";
                case ProjectStatus.Active: return "badge-success";
                case ProjectStatus.OnHold: return "badge-info";
                case ProjectStatus.Completed: return "badge-primary";
                case ProjectStatus.Cancelled: return "badge-danger";
                case ProjectStatus.Archived: return "badge-secondary";
                default: return "badge-dark";
            }
        }

        public static ProjectStatus GetProjectStatusFromString(string status)
        {
            if (Enum.TryParse<ProjectStatus>(status, true, out var result))
            {
                return result;
            }
            return ProjectStatus.Planning;
        }

        /// <summary>
        /// Validate business rules cho status transition
        /// </summary>
        public static StatusTransitionValidation ValidateTransition(
            ProjectStatus fromStatus,
            ProjectStatus toStatus,
            DateTime? projectStartDate = null,
            decimal? completionPercentage = null)
        {
            // Kiểm tra transition hợp lệ
            if (!CanTransitionTo(fromStatus, toStatus))
            {
                return new StatusTransitionValidation
                {
                    IsValid = false,
                    ErrorMessage = string.Format("Không thể chuyển từ {0} sang {1}",
                        GetDescription(fromStatus), GetDescription(toStatus))
                };
            }

            // Business rules cụ thể
            switch (toStatus)
            {
                case ProjectStatus.Active:
                    if (projectStartDate.HasValue && projectStartDate > DateTime.Now)
                    {
                        return new StatusTransitionValidation
                        {
                            IsValid = false,
                            ErrorMessage = "Không thể chuyển sang Active khi project start date chưa đến"
                        };
                    }
                    break;

                case ProjectStatus.Completed:
                    if (completionPercentage.HasValue && completionPercentage < 100)
                    {
                        return new StatusTransitionValidation
                        {
                            IsValid = false,
                            ErrorMessage = "Không thể chuyển sang Completed khi completion < 100%"
                        };
                    }
                    break;

                case ProjectStatus.Archived:
                    if (fromStatus != ProjectStatus.Completed && fromStatus != ProjectStatus.Cancelled)
                    {
                        return new StatusTransitionValidation
                        {
                            IsValid = false,
                            ErrorMessage = "Chỉ có thể Archive project đã Completed hoặc Cancelled"
                        };
                    }
                    break;
            }

            return new StatusTransitionValidation
            {
                IsValid = true,
                ErrorMessage = string.Empty
            };
        }

        /// <summary>
        /// Lấy danh sách ProjectStatusItem với đầy đủ thông tin
        /// </summary>
        public static ObservableCollection<ProjectStatusItem> GetProjectStatusItems()
        {
            var items = new ObservableCollection<ProjectStatusItem>();
            foreach (ProjectStatus status in Enum.GetValues(typeof(ProjectStatus)).Cast<ProjectStatus>())
            {
                items.Add(new ProjectStatusItem(status));
            }
            return items;
        }

        /// <summary>
        /// Lấy danh sách ProjectStatusItem không bao gồm "All"
        /// </summary>
        public static ObservableCollection<ProjectStatusItem> GetProjectStatusItemsWithoutAll()
        {
            var items = new ObservableCollection<ProjectStatusItem>();
            foreach (ProjectStatus status in Enum.GetValues(typeof(ProjectStatus))
                .Cast<ProjectStatus>()
                .Where(s => s != ProjectStatus.All))
            {
                items.Add(new ProjectStatusItem(status));
            }
            return items;
        }

        /// <summary>
        /// Lấy danh sách ProjectStatusItem đã sắp xếp theo sort order
        /// </summary>
        public static ObservableCollection<ProjectStatusItem> GetProjectStatusItemsSorted()
        {
            var items = new ObservableCollection<ProjectStatusItem>();
            foreach (ProjectStatus status in Enum.GetValues(typeof(ProjectStatus))
                .Cast<ProjectStatus>()
                .OrderBy(s => GetSortOrder(s)))
            {
                items.Add(new ProjectStatusItem(status));
            }
            return items;
        }
    }

    /// <summary>
    /// Struct để lưu RGB color (thay cho tuple)
    /// </summary>
    public struct RgbColor
    {
        public int R { get; }
        public int G { get; }
        public int B { get; }

        public RgbColor(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        public override string ToString()
        {
            return string.Format("RGB({0}, {1}, {2})", R, G, B);
        }
    }

    /// <summary>
    /// Struct để lưu validation result (thay cho tuple)
    /// </summary>
    public struct StatusTransitionValidation
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Model item cho ProjectStatus với đầy đủ thông tin display
    /// </summary>
    public class ProjectStatusItem
    {
        public ProjectStatusItem()
        {
        }

        public ProjectStatusItem(ProjectStatus status)
        {
            Value = status;
            Name = ProjectStatusHelper.GetName(status);
            ShortName = ProjectStatusHelper.GetShortName(status);
            Description = ProjectStatusHelper.GetDescription(status);
            Icon = ProjectStatusHelper.GetDisplayIcon(status);
            AlternativeIcon = ProjectStatusHelper.GetAlternativeIcon(status);
            Emoji = ProjectStatusHelper.GetStatusEmoji(status);
            Color = ProjectStatusHelper.GetDisplayColor(status);
            BackgroundColor = ProjectStatusHelper.GetBackgroundColor(status);
            BorderColor = ProjectStatusHelper.GetBorderColor(status);
            Tooltip = ProjectStatusHelper.GetTooltip(status);
            SortOrder = ProjectStatusHelper.GetSortOrder(status);
            BadgeStyle = ProjectStatusHelper.GetBadgeStyle(status);
            IsActive = ProjectStatusHelper.IsActive(status);
            IsClosed = ProjectStatusHelper.IsClosed(status);
            CanEdit = ProjectStatusHelper.CanEdit(status);
            CanAssignTasks = ProjectStatusHelper.CanAssignTasks(status);
        }

        /// <summary>Enum value</summary>
        public ProjectStatus Value { get; set; }

        /// <summary>Tên hiển thị ngắn gọn</summary>
        public string Name { get; set; }

        /// <summary>Tên viết tắt</summary>
        public string ShortName { get; set; }

        /// <summary>Mô tả chi tiết</summary>
        public string Description { get; set; }

        /// <summary>Icon chính</summary>
        public string Icon { get; set; }

        /// <summary>Icon thay thế</summary>
        public string AlternativeIcon { get; set; }

        /// <summary>Emoji status</summary>
        public string Emoji { get; set; }

        /// <summary>Màu chính (Hex)</summary>
        public string Color { get; set; }

        /// <summary>Màu background (Hex)</summary>
        public string BackgroundColor { get; set; }

        /// <summary>Màu border (Hex)</summary>
        public string BorderColor { get; set; }

        /// <summary>Tooltip text</summary>
        public string Tooltip { get; set; }

        /// <summary>Thứ tự sắp xếp</summary>
        public int SortOrder { get; set; }

        /// <summary>Badge style class</summary>
        public string BadgeStyle { get; set; }

        /// <summary>Có đang active không</summary>
        public bool IsActive { get; set; }

        /// <summary>Có đã closed không</summary>
        public bool IsClosed { get; set; }

        /// <summary>Có thể edit không</summary>
        public bool CanEdit { get; set; }

        /// <summary>Có thể assign tasks không</summary>
        public bool CanAssignTasks { get; set; }

        #region Display Properties

        /// <summary>Text hiển thị: Icon + Description</summary>
        public string DisplayText
        {
            get { return string.Format("{0} {1}", Icon, Description); }
        }

        /// <summary>Text hiển thị đầy đủ: Icon + Name + Description</summary>
        public string FullDisplayText
        {
            get { return string.Format("{0} {1} - {2}", Icon, Name, Description); }
        }

        /// <summary>Text hiển thị ngắn: Icon + Name</summary>
        public string ShortDisplayText
        {
            get { return string.Format("{0} {1}", Icon, Name); }
        }

        /// <summary>Text hiển thị với emoji: Emoji + Name</summary>
        public string EmojiDisplayText
        {
            get { return string.Format("{0} {1}", Emoji, Name); }
        }

        /// <summary>Text cho badge: ShortName</summary>
        public string BadgeText
        {
            get { return ShortName; }
        }

        /// <summary>RGB Color</summary>
        public RgbColor RgbColor
        {
            get { return Value.GetRgbColor(); }
        }

        
        #endregion

        #region Helper Methods

        /// <summary>
        /// Kiểm tra có thể transition sang status khác không
        /// </summary>
        public bool CanTransitionTo(ProjectStatus toStatus)
        {
            return ProjectStatusHelper.CanTransitionTo(Value, toStatus);
        }

        /// <summary>
        /// Lấy danh sách valid transitions
        /// </summary>
        public ProjectStatus[] GetValidTransitions()
        {
            return ProjectStatusHelper.GetValidTransitions(Value);
        }

        /// <summary>
        /// Override ToString
        /// </summary>
        public override string ToString()
        {
            return DisplayText;
        }

        #endregion
    }
}