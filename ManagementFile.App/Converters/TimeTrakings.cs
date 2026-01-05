using ManagementFile.App.Models.TimeTracking;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ManagementFile.App.Converters
{
    /// <summary>
    /// Converter để hiển thị màu dựa trên số giờ
    /// Sử dụng heatmap: càng nhiều giờ càng đậm màu
    /// </summary>
    public class HoursToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal hours)
            {
                // 0h = Trắng
                if (hours == 0)
                    return new SolidColorBrush(Colors.White);

                // 1-4h = Xanh nhạt
                if (hours <= 4)
                    return new SolidColorBrush(Color.FromRgb(200, 230, 255));

                // 4-8h = Xanh
                if (hours <= 8)
                    return new SolidColorBrush(Color.FromRgb(100, 180, 255));

                // 8-10h = Xanh đậm (OT nhẹ)
                if (hours <= 10)
                    return new SolidColorBrush(Color.FromRgb(255, 200, 100));

                // >10h = Đỏ (OT nặng)
                return new SolidColorBrush(Color.FromRgb(255, 100, 100));
            }

            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter cho text color
    /// </summary>
    public class HoursToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal hours)
            {
                // >10h = Text đỏ
                if (hours > 10)
                    return new SolidColorBrush(Color.FromRgb(200, 0, 0));

                // 8-10h = Text cam
                if (hours > 8)
                    return new SolidColorBrush(Color.FromRgb(255, 140, 0));

                // Normal
                return new SolidColorBrush(Color.FromRgb(50, 50, 50));
            }

            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để hiển thị warning icon
    /// </summary>
    public class BoolToWarningIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasWarning && hasWarning)
            {
                return "⚠️";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để hiển thị border khi có warning
    /// </summary>
    public class BoolToWarningBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasWarning && hasWarning)
            {
                return new SolidColorBrush(Color.FromRgb(255, 140, 0));
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để hiển thị visibility của warning
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để format currency (VNĐ)
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                return $"{amount:N0} ₫";
            }
            return "0 ₫";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                str = str.Replace("₫", "").Replace(",", "").Trim();
                if (decimal.TryParse(str, out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }

    /// <summary>
    /// Converter để format hours
    /// </summary>
    public class HoursFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal hours)
            {
                if (hours == 0)
                    return "-";

                // Format: 8.5h hoặc 8.25h
                return $"{hours:0.##}h";
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                str = str.Replace("h", "").Trim();
                if (decimal.TryParse(str, out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }

    /// <summary>
    /// Converter cho weekend styling
    /// </summary>
    public class IsWeekendConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int dayIndex)
            {
                // 5 = Saturday, 6 = Sunday
                return dayIndex >= 5;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để hiển thị màu theo mức độ validation
    /// </summary>
    public class ValidationSeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ValidationSeverity severity)
            {
                switch (severity)
                {
                    case ValidationSeverity.Info:
                        return new SolidColorBrush(Color.FromRgb(23, 162, 184)); // Blue
                    case ValidationSeverity.Warning:
                        return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
                    case ValidationSeverity.Error:
                        return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
                    case ValidationSeverity.Critical:
                        return new SolidColorBrush(Color.FromRgb(128, 0, 0)); // Dark Red
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
