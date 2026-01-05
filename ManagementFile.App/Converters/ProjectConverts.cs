using ManagementFile.App.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ManagementFile.App.Converters
{
    public class ModeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DialogMode mode)
            {
                switch(mode)
                {
                    case DialogMode.Add:
                        return new SolidColorBrush(Color.FromRgb(46, 204, 113)); // Green
                    case DialogMode.Edit:
                        return new SolidColorBrush(Color.FromRgb(243, 156, 18)); // Orange
                    case DialogMode.View:
                        return new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Blue
                    default:
                        return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Gray
                }

            }
            return new SolidColorBrush(Color.FromRgb(149, 165, 166));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ModeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DialogMode mode)
            {
                switch(mode)
                {
                    case DialogMode.Add:
                        return "THÊM MỚI";
                    case DialogMode.Edit:
                        return "CHỈNH SỬA";
                    case DialogMode.View:
                        return "XEM CHI TIẾT";
                    default:
                        return "UNKNOWN";
                }

            }
            return "UNKNOWN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
