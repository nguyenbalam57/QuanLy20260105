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
    /// UserRole - Vai trò người dùng trong hệ thống
    /// Phân cấp quyền hạn từ cao xuống thấp (theo thứ tự số)
    /// Số càng cao thì quyền hạn càng lớn
    /// </summary>
    public enum UserRole
    {
        [Description("All - Hiển thị tất cả")]
        All = -1,

        /// <summary>Guest - Khách, chỉ xem</summary>
        [Description("Khách - Chỉ có quyền xem thông tin cơ bản")]
        Guest = 0,

        /// <summary>Intern - Thực tập sinh, quyền hạn thấp nhất</summary>
        [Description("Thực tập sinh - Quyền hạn hạn chế, cần sự giám sát")]
        Intern = 1,

        /// <summary>Staff - Nhân viên thường</summary>
        [Description("Nhân viên - Có thể thực hiện các công việc được phân công")]
        Staff = 2,

        /// <summary>Senior - Nhân viên senior</summary>
        [Description("Nhân viên cao cấp - Có kinh nghiệm và quyền hạn cao hơn")]
        Senior = 3,

        /// <summary>TeamLead - Trưởng nhóm</summary>
        [Description("Trưởng nhóm - Quản lý nhóm nhỏ và phân công công việc")]
        TeamLead = 4,

        /// <summary>Manager - Quản lý cấp trung</summary>
        [Description("Quản lý - Quản lý nhiều nhóm và ra quyết định quan trọng")]
        Manager = 5,

        /// <summary>Director - Giám đốc</summary>
        [Description("Giám đốc - Quản lý cấp cao, định hướng chiến lược")]
        Director = 6,

        /// <summary>Admin - Quản trị viên cao nhất</summary>
        [Description("Quản trị viên - Có toàn quyền trong hệ thống")]
        Admin = 7
    }

    /// <summary>
    /// Extension methods for UserRole enum
    /// Cung cấp các phương thức tiện ích cho UserRole
    /// </summary>
    public static class UserRoleExtensions
    {
        /// <summary>
        /// Lấy mô tả (Description) của UserRole
        /// </summary>
        /// <param name="role">UserRole cần lấy mô tả</param>
        /// <returns>Chuỗi mô tả hoặc tên enum nếu không có Description</returns>
        public static string GetDescription(this UserRole role)
        {
            var field = role.GetType().GetField(role.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute?.Description ?? role.ToString();
        }

        /// <summary>
        /// Lấy tên hiển thị ngắn gọn của vai trò
        /// </summary>
        /// <param name="role">UserRole cần lấy tên hiển thị</param>
        /// <returns>Tên hiển thị tiếng Việt</returns>
        public static string GetDisplayName(this UserRole role)
        {

            switch(role)
            {
                case UserRole.Guest:
                    return "Khách";
                case UserRole.Intern:
                    return "Thực tập sinh";
                case UserRole.Staff:
                    return "Nhân viên";
                case UserRole.Senior:
                    return "Nhân viên cao cấp";
                case UserRole.TeamLead:
                    return "Trưởng nhóm";
                case UserRole.Manager:
                    return "Quản lý";
                case UserRole.Director:
                    return "Giám đốc";
                case UserRole.Admin:
                    return "Quản trị viên";
                default:
                    return role.ToString();
            }
        }

        /// <summary>
        /// Đọc chuỗi và chuyển thành UserRole
        /// </summary>
        /// <param name="roleString"></param>
        /// <returns></returns>
        public static UserRole GetUserRoleFromString(string roleString)
        {
            if (string.IsNullOrEmpty(roleString)) return UserRole.Guest;

            if (Enum.TryParse<UserRole>(roleString, out var result))
            {
                return result;
            }

            // Fallback: tìm theo DisplayName
            foreach (UserRole role in Enum.GetValues(typeof(UserRole)).Cast<UserRole>())
            {
                if (GetDisplayName(role).Equals(roleString, StringComparison.OrdinalIgnoreCase))
                {
                    return role;
                }
            }

            return UserRole.Guest; // Default value là khách
        }

        /// <summary>
        /// Kiểm tra xem vai trò có phải là cấp quản lý không
        /// (TeamLead trở lên)
        /// </summary>
        /// <param name="role">UserRole cần kiểm tra</param>
        /// <returns>true nếu là cấp quản lý</returns>
        public static bool IsManagement(this UserRole role)
        {
            return (int)role >= (int)UserRole.TeamLead;
        }

        /// <summary>
        /// Kiểm tra xem vai trò có phải là cấp điều hành không
        /// (Manager trở lên)
        /// </summary>
        /// <param name="role">UserRole cần kiểm tra</param>
        /// <returns>true nếu là cấp điều hành</returns>
        public static bool IsExecutive(this UserRole role)
        {
            return (int)role >= (int)UserRole.Manager;
        }

        /// <summary>
        /// Kiểm tra xem vai trò có phải là nhân viên thực thi không
        /// (Intern đến Senior)
        /// </summary>
        /// <param name="role">UserRole cần kiểm tra</param>
        /// <returns>true nếu là nhân viên thực thi</returns>
        public static bool IsEmployee(this UserRole role)
        {
            return role >= UserRole.Intern && role <= UserRole.Senior;
        }

        /// <summary>
        /// Kiểm tra xem có quyền cao hơn hoặc bằng vai trò được chỉ định không
        /// </summary>
        /// <param name="currentRole">Vai trò hiện tại</param>
        /// <param name="requiredRole">Vai trò yêu cầu</param>
        /// <returns>true nếu có đủ quyền</returns>
        public static bool HasPermission(this UserRole currentRole, UserRole requiredRole)
        {
            return (int)currentRole >= (int)requiredRole;
        }

        /// <summary>
        /// Lấy cấp độ quyền hạn (số nguyên)
        /// </summary>
        /// <param name="role">UserRole</param>
        /// <returns>Cấp độ quyền hạn từ 0-7</returns>
        public static int GetAuthorityLevel(this UserRole role)
        {
            return (int)role;
        }

        /// <summary>
        /// Kiểm tra xem có thể thăng chức lên vai trò mới không
        /// </summary>
        /// <param name="currentRole">Vai trò hiện tại</param>
        /// <param name="newRole">Vai trò mới</param>
        /// <returns>true nếu có thể thăng chức</returns>
        public static bool CanPromoteTo(this UserRole currentRole, UserRole newRole)
        {
            // Chỉ có thể thăng chức lên 1-2 cấp, không thể nhảy vọt quá nhiều
            int currentLevel = (int)currentRole;
            int newLevel = (int)newRole;

            return newLevel > currentLevel && (newLevel - currentLevel) <= 2;
        }

        /// <summary>
        /// Lấy màu sắc đại diện cho vai trò (để hiển thị UI)
        /// 
        /// Cách sử dụng string colorHex = UserRoleExtensions.GetRoleColor(Role);
        ///colorHex = colorHex.Replace("#", "");
        ///        if (colorHex.Length == 6 &&
        ///            byte.TryParse(colorHex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r) &&
        ///            byte.TryParse(colorHex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g) &&
        ///            byte.TryParse(colorHex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
        ///        {
        ///            return new SolidColorBrush(Color.FromRgb(r, g, b));
        ///        }
        ///        return new SolidColorBrush(Color.FromRgb(149, 165, 166));
        /// </summary>
        /// <param name="role">UserRole</param>
        /// <returns>Mã màu hex</returns>
        public static string GetRoleColor(this UserRole role)
        {

            switch(role)
            {
                case UserRole.Guest:
                    return "#9E9E9E"; // Gray
                case UserRole.Intern:
                    return "#4CAF50"; // Light Green
                case UserRole.Staff:
                    return "#2196F3"; // Blue
                case UserRole.Senior:
                    return "#FF9800"; // Orange
                case UserRole.TeamLead:
                    return "#9C27B0"; // Purple
                case UserRole.Manager:
                    return "#F44336"; // Red
                case UserRole.Director:
                    return "#795548"; // Brown
                case UserRole.Admin:
                    return "#000000"; // Black
                default:
                    return "#607D8B"; // Default Blue Gray

            }
        }

        /// <summary>
        /// Lấy icon đại diện cho vai trò
        /// </summary>
        /// <param name="role">UserRole</param>
        /// <returns>Tên icon (dành cho icon font)</returns>
        public static string GetRoleIcon(this UserRole role)
        {
            switch (role)
            {
                case UserRole.Guest:
                    return "👀"; // Eye icon
                case UserRole.Intern:
                    return "🎓"; // Graduation cap
                case UserRole.Staff:
                    return "👤"; // ID badge (hoặc dùng 👤)
                case UserRole.Senior:
                    return "🏅"; // Medal
                case UserRole.TeamLead:
                    return "🧑‍🤝‍🧑"; // Group
                case UserRole.Manager:
                    return "📋"; // Clipboard
                case UserRole.Director:
                    return "🏢"; // Office building
                case UserRole.Admin:
                    return "🛡️"; // Shield
                default:
                    return "👤"; // Default person icon
            }
        }


        private static readonly Dictionary<UserRole, string> UserRoleIcons = new Dictionary<UserRole, string>
        {
            { UserRole.All, GetRoleIcon(UserRole.All) },
            { UserRole.Guest, GetRoleIcon(UserRole.Guest) },
            { UserRole.Intern, GetRoleIcon(UserRole.Intern) },
            { UserRole.Staff, GetRoleIcon(UserRole.Staff) },
            { UserRole.Senior, GetRoleIcon(UserRole.Senior) },
            { UserRole.TeamLead, GetRoleIcon(UserRole.TeamLead) },
            { UserRole.Manager, GetRoleIcon(UserRole.Manager) },
            { UserRole.Director, GetRoleIcon(UserRole.Director) },
            { UserRole.Admin, GetRoleIcon(UserRole.Admin) },

        };
        public static ObservableCollection<UserRoleItem> GetUserRoleItems()
        {
            var items = new ObservableCollection<UserRoleItem>();
            foreach (UserRole status in Enum.GetValues(typeof(UserRole)).Cast<UserRole>())
            {

                if (status == UserRole.All) continue; // Loại bỏ All khỏi dialog

                var field = status.GetType().GetField(status.ToString());
                var description = GetDisplayName(status);
                var icon = UserRoleIcons.ContainsKey(status) ? UserRoleIcons[status] : "❓";
                items.Add(new UserRoleItem
                {
                    Value = status,
                    Description = description,
                    Icon = icon
                });
            }
            return items;
        }

        public static ObservableCollection<UserRoleItem> GetUserRoleMemberItems()
        {
            var items = new ObservableCollection<UserRoleItem>();
            foreach (UserRole status in Enum.GetValues(typeof(UserRole)).Cast<UserRole>())
            {

                if (status == UserRole.All || status == UserRole.Admin) continue; // Loại bỏ All khỏi dialog

                var field = status.GetType().GetField(status.ToString());
                var description = GetDisplayName(status);
                var icon = UserRoleIcons.ContainsKey(status) ? UserRoleIcons[status] : "❓";
                items.Add(new UserRoleItem
                {
                    Value = status,
                    Description = description,
                    Icon = icon
                });
            }
            return items;
        }

        /// <summary>
        /// Lấy danh sách tất cả trạng thái bao gồm "All" để filter
        /// </summary>
        public static ObservableCollection<UserRoleItem> GetAllUserRoleItems()
        {
            var items = new ObservableCollection<UserRoleItem>();
            foreach (UserRole status in Enum.GetValues(typeof(UserRole)))
            {
                var field = status.GetType().GetField(status.ToString());
                var description = GetDisplayName(status);

                var icon = UserRoleIcons.ContainsKey(status) ? UserRoleIcons[status] : "❓";

                items.Add(new UserRoleItem
                {
                    Value = status,
                    Description = description,
                    Icon = icon
                });
            }
            return items;
        }
    }

    public class UserRoleItem
    {
        public UserRole Value { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string DisplayText => $"{Icon} {Description}";
    }
}