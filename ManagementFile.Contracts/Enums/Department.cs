using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// Department - Phòng ban trong tổ chức
    /// Các bộ phận chuyên môn
    /// </summary>
    public enum Department
    {
        [Description("Tất cả")]
        All = -1,

        /// <summary>PM - Project Management (Quản lý dự án)</summary>
        [Description("Quản lý dự án")]
        PM = 0,
        /// <summary>SRA - System Requirements Analysis (Phân tích yêu cầu hệ thống)</summary>
        [Description("Phân tích yêu cầu hệ thống")]
        SRA = 1,
        /// <summary>SD - System Design (Thiết kế hệ thống)</summary>
        [Description("Thiết kế hệ thống")]
        SD = 2,
        /// <summary>UDC - Unit Development & Construction (Phát triển và xây dựng đơn vị)</summary>
        [Description("Phát triển và xây dựng đơn vị")]
        UDC = 3,
        /// <summary>UT - Unit Test (Kiểm thử đơn vị)</summary>
        [Description("Kiểm thử đơn vị")]
        UT = 4,
        /// <summary>ITST - Integration Test & System Test (Kiểm thử tích hợp và hệ thống)</summary>
        [Description("Kiểm thử tích hợp và hệ thống")]
        ITST = 5,
        /// <summary>STST - System Test & Stress Test (Kiểm thử hệ thống và tải)</summary>
        [Description("Kiểm thử hệ thống và tải")]
        STST = 6,
        /// <summary>IM - Implementation (Triển khai)</summary>
        [Description("Triển khai")]
        IM = 7,
        /// <summary>OTHER </summary>
        [Description("Khác")]
        OTHER = 8
    }

    /// <summary>
    /// Extension methods cho Department enum
    /// </summary>
    public static class DepartmentExtensions
    {
        /// <summary>
        /// Lấy mô tả từ Description attribute
        /// </summary>
        /// <param name="department">Department enum value</param>
        /// <returns>Mô tả của department</returns>
        public static string GetDescription(this Department department)
        {
            var fieldInfo = department.GetType().GetField(department.ToString());
            var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
            return descriptionAttribute?.Description ?? department.ToString();
        }

        /// <summary>
        /// Lấy icon tương ứng với department
        /// </summary>
        /// <param name="department">Department enum value</param>
        /// <returns>Icon class hoặc Unicode icon</returns>
        public static string GetIcon(this Department department)
        {
            switch (department)
            {
                case Department.All:
                    return "📂"; // hoặc "fas fa-folder"
                case Department.PM:
                    return "👨‍💼"; // hoặc "fas fa-project-diagram"
                case Department.SRA:
                    return "📋"; // hoặc "fas fa-clipboard-list"
                case Department.SD:
                    return "🎨"; // hoặc "fas fa-drafting-compass"
                case Department.UDC:
                    return "⚙️"; // hoặc "fas fa-cogs"
                case Department.UT:
                    return "🧪"; // hoặc "fas fa-flask"
                case Department.ITST:
                    return "🔗"; // hoặc "fas fa-link"
                case Department.STST:
                    return "⚡"; // hoặc "fas fa-bolt"
                case Department.IM:
                    return "🚀"; // hoặc "fas fa-rocket"
                case Department.OTHER:
                    return "📦"; // hoặc "fas fa-box"
                default:
                    return "❓"; // hoặc "fas fa-question"
            }

        }

        /// <summary>
        /// Lấy màu chủ đề của department (Hex color)
        /// </summary>
        /// <param name="department">Department enum value</param>
        /// <returns>Mã màu Hex</returns>
        public static string GetColor(this Department department)
        {
            switch (department)
            {
                case Department.All:
                    return "#607D8B"; // Xám xanh
                case Department.PM:
                    return "#FF6B35"; // Cam đậm
                case Department.SRA:
                    return "#4285F4"; // Xanh dương Google
                case Department.SD:
                    return "#9C27B0"; // Tím
                case Department.UDC:
                    return "#FF9800"; // Cam
                case Department.UT:
                    return "#4CAF50"; // Xanh lá
                case Department.ITST:
                    return "#00BCD4"; // Cyan
                case Department.STST:
                    return "#F44336"; // Đỏ
                case Department.IM:
                    return "#8BC34A"; // Xanh lá nhạt
                case Department.OTHER:
                    return "#757575"; // Xám đậm
                default:
                    return "#9E9E9E"; // Xám
            }


        }

        /// <summary>
        /// Lấy màu nền nhạt cho department
        /// </summary>
        /// <param name="department">Department enum value</param>
        /// <returns>Mã màu Hex cho background</returns>
        public static string GetBackgroundColor(this Department department)
        {
            switch (department)
            {
                case Department.All:
                    return "#ECEFF1"; // Xám xanh nhạt
                case Department.PM:
                    return "#FFF3E0"; // Cam nhạt
                case Department.SRA:
                    return "#E3F2FD"; // Xanh dương nhạt
                case Department.SD:
                    return "#F3E5F5"; // Tím nhạt
                case Department.UDC:
                    return "#FFF8E1"; // Vàng nhạt
                case Department.UT:
                    return "#E8F5E8"; // Xanh lá nhạt
                case Department.ITST:
                    return "#E0F2F1"; // Cyan nhạt
                case Department.STST:
                    return "#FFEBEE"; // Đỏ nhạt
                case Department.IM:
                    return "#F1F8E9"; // Xanh lá nhạt hơn
                case Department.OTHER:
                    return "#F5F5F5"; // Xám nhạt
                default:
                    return "#FFFFFF"; // Trắng
            }


        }

        /// <summary>
        /// Lấy viết tắt của department
        /// </summary>
        /// <param name="department">Department enum value</param>
        /// <returns>Chuỗi viết tắt</returns>
        public static string GetAbbreviation(this Department department)
        {
            return department.ToString();
        }

        public static ObservableCollection<DepartmentItem> GetDepartmentItems()
        {
            var items = new ObservableCollection<DepartmentItem>();
            foreach (Department status in Enum.GetValues(typeof(Department)).Cast<Department>())
            {

                if (status == Department.All) continue; // Loại bỏ All khỏi dialog

                var field = status.GetType().GetField(status.ToString());
                var description = GetDescription(status);
                var icon = GetIcon(status);
                var color = GetColor(status);
                var backgroundColor = GetBackgroundColor(status);
                items.Add(new DepartmentItem
                {
                    Value = status,
                    Description = description,
                    Icon = icon,
                    Color = color,
                    BackgroundColor = backgroundColor
                });
            }
            return items;
        }

        /// <summary>
        /// Lấy danh sách tất cả trạng thái bao gồm "All" để filter
        /// </summary>
        public static ObservableCollection<DepartmentItem> GetAllDepartmentItems()
        {
            var items = new ObservableCollection<DepartmentItem>();
            foreach (Department status in Enum.GetValues(typeof(Department)))
            {
                var field = status.GetType().GetField(status.ToString());
                var description = GetDescription(status);

                var icon = GetIcon(status);
                var color = GetColor(status);
                var backgroundColor = GetBackgroundColor(status);
                items.Add(new DepartmentItem
                {
                    Value = status,
                    Description = description,
                    Icon = icon,
                    Color = color,
                    BackgroundColor = backgroundColor
                });
            }
            return items;
        }

    }

    /// <summary>
    /// Class chứa thông tin chi tiết về department
    /// </summary>
    public class DepartmentItem
    {
        public Department Value { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public string DisplayText => $"{Icon} {Description}";
    }
}
