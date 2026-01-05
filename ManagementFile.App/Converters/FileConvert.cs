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
    /// Converter để chuyển đổi extension thành icon
    /// </summary>
    public class FileIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string extension)
            {
                return GetFileIcon(extension.ToLower());
            }
            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetFileIcon(string extension)
        {
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                case ".svg":
                case ".webp":
                    return "🖼️"; // Image icon
                case ".pdf":
                    return "📕"; // PDF icon
                case ".doc":
                case ".docx":
                    return "📘"; // Word icon
                case ".xls":
                case ".xlsx":
                    return "📗"; // Excel icon
                case ".ppt":
                case ".pptx":
                    return "📙"; // PowerPoint icon
                case ".txt":
                    return "📝"; // Text file icon
                case ".cs":
                    return "🔧"; // C# file icon
                case ".js":
                    return "📜"; // JavaScript file icon
                case ".html":
                case ".htm":
                    return "🌐"; // HTML file icon
                case ".css":
                    return "🎨"; // CSS file icon
                case ".xml":
                    return "📋"; // XML file icon
                case ".json":
                    return "📊"; // JSON file icon
                case ".sql":
                    return "🗃️"; // SQL file icon
                case ".zip":
                case ".rar":
                case ".7z":
                case ".tar":
                case ".gz":
                    return "📦"; // Archive icon
                case ".mp3":
                case ".wav":
                case ".flac":
                case ".aac":
                    return "🎵"; // Audio file icon
                case ".mp4":
                case ".avi":
                case ".mkv":
                case ".mov":
                    return "🎬"; // Video file icon
                case ".exe":
                case ".msi":
                    return "⚙️"; // Executable file icon
                default:
                    return "📄"; // Default file icon
            }

        }
    }

    /// <summary>
    /// Converter để chuyển đổi Boolean thành mũi tên cho collapsible header
    /// </summary>
    public class BooleanToArrowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? "▼" : "▶";
            }
            return "▶";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để chuyển đổi null/empty string thành Visibility
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNullOrEmpty = value == null || (value is string s && string.IsNullOrEmpty(s));

            // Nếu parameter là null hoặc empty, hiển thị khi value null/empty
            // Nếu parameter không phải null/empty, ẩn khi value null/empty
            bool shouldShow = string.IsNullOrEmpty(parameter?.ToString()) ? isNullOrEmpty : !isNullOrEmpty;

            return shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Trả về "N/Max" (ví dụ "12/2000"), an toàn khi value là null.
    public class LengthToCountConverter : IValueConverter
    {
        public int MaxChars { get; set; } = 2000;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            int len = s?.Length ?? 0;

            // đọc ConverterParameter nếu có (ví dụ "500")
            if (parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int p))
                {
                    return $"{len}/{p}";
                }
            }

            return $"{len}/{MaxChars}";
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
