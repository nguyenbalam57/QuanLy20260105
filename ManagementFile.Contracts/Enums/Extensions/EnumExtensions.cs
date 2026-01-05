using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace ManagementFile.Contracts.Enums.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Lấy Description attribute từ enum value
        /// </summary>
        /// <param name="value">Enum value</param>
        /// <returns>Description text hoặc enum name nếu không có description</returns>
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);

            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field,
                        typeof(DescriptionAttribute)) as DescriptionAttribute;

                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }

            return value.ToString();
        }
        /// <summary>
        /// des -> enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="description"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static T FromDescription<T>(string description) where T : Enum
        {
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>();
                if (attribute != null && attribute.Description == description)
                {
                    return (T)field.GetValue(null);
                }
            }

            throw new ArgumentException($"Không tìm thấy enum {typeof(T).Name} với description = '{description}'");
        }

        public static TaskPriority GetEnumTaskPriority(string taskPriority)
        {
            switch (taskPriority)
            {
                case "Low":
                    return TaskPriority.Low;
                case "Normal":
                    return TaskPriority.Normal;
                case "High":
                    return TaskPriority.High;
                case "Critical":
                    return TaskPriority.Critical;
                case "Emergency":
                    return TaskPriority.Emergency;
                default:
                    return TaskPriority.All;
            }
        }

        public static T ToEnum<T>(int value) where T : struct, Enum
        {
            if (Enum.IsDefined(typeof(T), value))
            {
                return (T)(object)value;
            }
            else
            {
                throw new ArgumentException($"Giá trị {value} không hợp lệ cho enum {typeof(T).Name}");
            }
        }

        public static bool TryToEnum<T>(int value, out T result) where T : struct, Enum
        {
            if (Enum.IsDefined(typeof(T), value))
            {
                result = (T)(object)value;
                return true;
            }
            result = default;
            return false;
        }
    }
}
