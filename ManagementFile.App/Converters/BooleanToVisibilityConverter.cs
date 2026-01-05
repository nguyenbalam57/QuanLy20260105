using ManagementFile.Contracts.Enums;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace ManagementFile.App.Converters
{
    /// <summary>
    /// Converter để chuyển đổi Boolean thành Visibility với khả năng reverse
    /// Compatible với C# 7.3 và .NET Framework 4.8
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;

            if (value is bool)
                boolValue = (bool)value;
            else if (value is bool?)
                boolValue = (bool?)value ?? false;

            // Check for inverse parameter
            bool inverse = parameter != null &&
                          (parameter.ToString().ToLower() == "true" ||
                           parameter.ToString().ToLower() == "inverse");

            if (inverse)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = value is Visibility && (Visibility)value == Visibility.Visible;

            // Check for inverse parameter
            bool inverse = parameter != null &&
                          (parameter.ToString().ToLower() == "true" ||
                           parameter.ToString().ToLower() == "inverse");

            if (inverse)
                result = !result;

            return result;
        }
    }

    /// <summary>
    /// Separate converter for inverse Boolean to Visibility
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;

            if (value is bool)
                boolValue = (bool)value;
            else if (value is bool?)
                boolValue = (bool?)value ?? false;

            return !boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is Visibility && (Visibility)value == Visibility.Visible);
        }
    }

    /// <summary>
    /// Converter để chuyển đổi String thành Visibility
    /// Hiển thị nếu string không null/empty, ẩn nếu null/empty
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            var stringValue = value.ToString();

            // Check for inverse parameter
            bool inverse = parameter != null &&
                          (parameter.ToString().ToLower() == "true" ||
                           parameter.ToString().ToLower() == "inverse");

            bool hasValue = !string.IsNullOrWhiteSpace(stringValue);

            if (inverse)
                hasValue = !hasValue;

            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not used for String to Visibility conversion
            throw new NotImplementedException();
        }
    }

    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                var field = enumValue.GetType().GetField(enumValue.ToString());
                var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
                return attribute?.Description ?? enumValue.ToString();
            }
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType.IsEnum && value is string stringValue)
            {
                foreach (var field in targetType.GetFields())
                {
                    var attribute = field.GetCustomAttribute<DescriptionAttribute>();
                    if (attribute?.Description == stringValue)
                    {
                        return field.GetValue(null);
                    }
                }
            }
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Enhanced BooleanToVisibilityConverter with support for inversion
    /// Supports ConverterParameter="Invert" to reverse the logic
    /// </summary>
    public class BooleanToVisibilityConverterCombobox : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;

            // Handle various input types
            if (value is bool directBool)
            {
                boolValue = directBool;
            }
            else if (value is bool?)
            {
                boolValue = ((bool?)value).GetValueOrDefault(false);
            }
            else if (value != null)
            {
                // Try to convert other types to boolean
                if (bool.TryParse(value.ToString(), out bool parsed))
                {
                    boolValue = parsed;
                }
                else
                {
                    // Non-null values are considered true, null values false
                    boolValue = true;
                }
            }

            // Check for invert parameter
            bool shouldInvert = parameter?.ToString()?.ToLower() == "invert";

            if (shouldInvert)
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (value is Visibility visibility) && (visibility == Visibility.Visible);

            // Check for invert parameter
            bool shouldInvert = parameter?.ToString()?.ToLower() == "invert";

            if (shouldInvert)
            {
                isVisible = !isVisible;
            }

            return isVisible;
        }
    }

    /// <summary>
    /// Alternative converter that always inverts boolean to visibility
    /// For cases where you always want the opposite behavior
    /// </summary>
    public class InverseBooleanToVisibilityConverterCombobox : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;

            if (value is bool directBool)
            {
                boolValue = directBool;
            }
            else if (value is bool?)
            {
                boolValue = ((bool?)value).GetValueOrDefault(false);
            }
            else if (value != null)
            {
                boolValue = bool.TryParse(value.ToString(), out bool parsed) ? parsed : true;
            }

            // Always invert
            return (!boolValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (value is Visibility visibility) && (visibility == Visibility.Visible);
            return !isVisible; // Always invert back
        }
    }

    /// <summary>
    /// Converter để đảo ngược giá trị Boolean
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter?.ToString() == "Visibility")
                {
                    return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                }
                else if (parameter?.ToString() == "Collapsed")
                {
                    return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                }
                else if (parameter?.ToString()?.Contains("|") == true)
                {
                    // Format: "ValueForTrue|ValueForFalse"
                    var parts = parameter.ToString().Split('|');
                    if (parts.Length == 2)
                    {
                        return boolValue ? parts[1] : parts[0]; // Inverted
                    }
                }

                return !boolValue;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return value;
        }
    }

    /// <summary>
    /// Converter để chuyển đổi Enum thành string hiển thị
    /// </summary>
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";

            string valueString = parameter?.ToString();

            switch (value)
            {
                case CommentType commentType:
                 return $"{commentType.GetIcon()} {commentType.GetDisplayName()}";

                case TaskPriority priority:
                    return $"{TaskPriorityHelper.GetDisplayIcon(priority)} {TaskPriorityHelper.GetDescription(priority)}";

                case TaskStatuss status:
                    return $"{TaskStatussHelper.GetDisplayIcon(status)} {TaskStatussHelper.GetDescription(status)}";

                case UserRole role:
                    return $"{UserRoleExtensions.GetRoleIcon(role)} {UserRoleExtensions.GetDescription(role)}";

                case Department dept:

                    object param;
                    switch(valueString?.ToLower())
                    {
                        case "Icon":
                            param = DepartmentExtensions.GetIcon(dept);
                            break;
                        case "Description":
                            param = DepartmentExtensions.GetDescription(dept);
                            break;
                        case "Color":
                            param = DepartmentExtensions.GetColor(dept);
                            break;
                        case "DisplayText":
                            param = $"{DepartmentExtensions.GetIcon(dept)} {DepartmentExtensions.GetDescription(dept)}";
                            break;
                        default:
                            param = ""; // Default to full representation
                            break;
                    }

                    return param;

                default:
                    return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}