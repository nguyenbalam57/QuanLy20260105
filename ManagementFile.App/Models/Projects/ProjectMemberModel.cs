using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ManagementFile.App.Models.Projects
{
    public class ProjectMemberModel : INotifyPropertyChanged
    {
        private bool _isActive;

        public int Id { get; set; }
        public string IdText => Id > 0 ? Id.ToString("D6") : "N/A";
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public UserRole ProjectRole { get; set; }
        public UserRole Role { get; set; } // Compatibility property
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public decimal AllocationPercentage { get; set; }
        public decimal? HourlyRate { get; set; }
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Additional properties for UI
        public int AssignedTasks { get; set; }
        public int CompletedTasks { get; set; }
        public decimal TotalHours { get; set; }
        public DateTime? LastActivity { get; set; }

        // UI Helper Properties
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

        public string RoleDisplayName
        {
            get
            {
                return UserRoleExtensions.GetDisplayName(ProjectRole);
            }
        }

        public string RoleIcon
        {
            get
            {
                return UserRoleExtensions.GetRoleIcon(ProjectRole);

            }
        }

        public Brush RoleColor
        {
            get
            {
                string colorHex = UserRoleExtensions.GetRoleColor(ProjectRole);
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

        public string DepartmentDisplayName => ""; // Can be expanded later

        public string LastActivityDisplayText
        {
            get
            {
                if (LastActivity == null)
                    return "Chưa có hoạt động";

                var diff = DateTime.Now - LastActivity.Value;
                if (diff.TotalDays > 7)
                    return LastActivity.Value.ToString("dd/MM/yyyy");
                if (diff.TotalDays > 1)
                    return $"{(int)diff.TotalDays} ngày trước";
                if (diff.TotalHours > 1)
                    return $"{(int)diff.TotalHours} giờ trước";

                return "Gần đây";
            }
        }

        public static ProjectMemberModel MapToProjectMemberModel(ProjectMemberDto dto)
        {
            return new ProjectMemberModel
            {
                Id = dto.Id,
                ProjectId = dto.ProjectId,
                UserId = dto.UserId,
                UserName = dto.UserName,
                FullName = dto.FullName,
                Email = dto.Email,
                ProjectRole = dto.ProjectRole,
                Role = dto.ProjectRole, // Set both for compatibility
                JoinedAt = dto.JoinedAt,
                IsActive = dto.IsActive,
                LeftAt = dto.LeftAt,

            };
        }



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
