using ManagementFile.Contracts.Enums;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ManagementFile.AdminManagement.Converters
{
    /// <summary>
    /// Converter to convert UserRole enum to display string
    /// </summary>
    public class RoleDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserRole role)
            {
                switch (role)
                {
                    case UserRole.Guest:
                        return "👁️ Guest";
                    case UserRole.Intern:
                        return "🎓 Intern";
                    case UserRole.Staff:
                        return "👤 Staff";
                    case UserRole.Senior:
                        return "⭐ Senior";
                    case UserRole.TeamLead:
                        return "👑 Team Lead";
                    case UserRole.Manager:
                        return "📊 Manager";
                    case UserRole.Director:
                        return "🏢 Director";
                    case UserRole.Admin:
                        return "🚀 Admin";
                    default:
                        return role.ToString();
                }
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to convert Department enum to display string
    /// </summary>
    public class DepartmentDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Department dept)
            {
                switch (dept)
                {
                    case Department.PM:
                        return "📋 PM";
                    case Department.SRA:
                        return "📊 SRA";
                    case Department.SD:
                        return "🏗️ SD";
                    case Department.UDC:
                        return "💻 UDC";
                    case Department.UT:
                        return "🧪 UT";
                    case Department.ITST:
                        return "🔧 ITST";
                    case Department.STST:
                        return "⚡ STST";
                    case Department.IM:
                        return "🚀 IM";
                    case Department.OTHER:
                        return "📁 OTHER";
                    default:
                        return dept.ToString();
                }
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to get initials from full name
    /// </summary>
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fullName && !string.IsNullOrWhiteSpace(fullName))
            {
                var parts = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
                }
                else if (parts.Length == 1)
                {
                    return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
                }
            }
            return "??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to convert null object to boolean (for button enabling)
    /// </summary>
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to convert string to visibility
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value?.ToString()) 
                ? System.Windows.Visibility.Collapsed 
                : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}