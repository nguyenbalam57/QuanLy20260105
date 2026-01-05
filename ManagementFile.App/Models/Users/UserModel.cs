using ManagementFile.App.Models.Projects;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ManagementFile.App.Models.Users
{
    public class UserModel : INotifyPropertyChanged
    {
        private int _id = 0;
        private string _userName = "";
        private string _fullName = "";
        private string _email = "";
        private UserRole _role = UserRole.All;
        private bool _isActive = true;
        private bool _isSelected;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public UserRole Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// Thuộc tính IsSelected cho ISelectableUserModel, để hiển thị trạng thái được chọn
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        // Permission flags
        public bool IsAdmin { get; set; }
        public bool IsProjectManager { get; set; }
        public bool IsViewer { get; set; }

        public Department Department { get; set; } = Department.OTHER;
        public string PhoneNumber { get; set; } = "";
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // UI Helper Properties
        public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : UserName;

        public string RoleDisplayName
        {
            get
            {
                return UserRoleExtensions.GetDisplayName(Role);
            }
        }

        public string Avatar
        {
            get
            {
                if (string.IsNullOrEmpty(FullName))
                    return "?";

                var names = FullName.Split(' ');
                if (names.Length >= 2)
                    return $"{names[0][0]}{names[names.Length - 1][0]}";

                return names[0].Length > 0 ? names[0][0].ToString() : "?";
            }
        }

        public Brush RoleColor
        {
            get
            {
                string colorHex = UserRoleExtensions.GetRoleColor(Role);
                colorHex = colorHex.Replace("#", "");
                if (colorHex.Length == 6 &&
                    byte.TryParse(colorHex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r) &&
                    byte.TryParse(colorHex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g) &&
                    byte.TryParse(colorHex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
                {
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }

                return new SolidColorBrush(Color.FromRgb(149, 165, 166));

            }
        }

        public string LastLoginText
        {
            get
            {
                if (!LastLogin.HasValue)
                    return "Chưa đăng nhập";

                var diff = DateTime.Now - LastLogin.Value;
                if (diff.TotalDays > 7)
                    return LastLogin.Value.ToString("dd/MM/yyyy");
                if (diff.TotalDays > 1)
                    return $"{(int)diff.TotalDays} ngày trước";
                if (diff.TotalHours > 1)
                    return $"{(int)diff.TotalHours} giờ trước";

                return "Gần đây";
            }
        }

        #region Static Factory Methods

        /// <summary>
        /// Create UserModel từ UserDto
        /// </summary>
        public static UserModel FromDto(UserDto dto)
        {
            if (dto == null) return null;

            return new UserModel
            {
                Id = dto.Id,
                UserName = dto.Username,
                FullName = dto.FullName,
                Email = dto.Email ?? "",
                Role = dto.Role,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,

                // Map permissions based on role
                IsAdmin = IsAdminRole(dto),
                IsProjectManager = IsManagerRole(dto),
                IsViewer = true,
            };
        }

        /// <summary>
        /// Convert UserModel sang UserDto
        /// </summary>
        public UserDto ToDto()
        {
            return new UserDto
            {
                Id = Id,
                Username = UserName,
                FullName = FullName,
                Email = Email,
                Role = Role,
                IsActive = IsActive,
                CreatedAt = CreatedAt,
            };
        }


        #endregion

        #region Private Helper Methods

        private static bool IsAdminRole(UserDto dto)
        {
            if (dto == null) return false;

            return dto.Role == UserRole.Admin;
        }

        private static bool IsManagerRole(UserDto dto)
        {
            if (dto == null) return false;

            return dto.Role == UserRole.Manager ||
                   dto.Role == UserRole.TeamLead;
        }

        public bool CanEditProject( ProjectModel project)
        {
            // Implement your permission logic here
            return Id == project?.ProjectManagerId || Role == UserRole.Admin;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }


    }
}
