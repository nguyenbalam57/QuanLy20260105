using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ManagementFile.App.Converters
{
    /// <summary>
    /// Converter để lấy initials từ tên đầy đủ
    /// </summary>
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fullName && !string.IsNullOrWhiteSpace(fullName))
            {
                var words = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2)
                {
                    return $"{words[0][0]}{words[words.Length - 1][0]}".ToUpper();
                }
                else if (words.Length == 1)
                {
                    return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpper();
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
    /// Converter để chuyển đổi số 0 thành Visibility
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Hiển thị khi count = 0 (empty state)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    
}
