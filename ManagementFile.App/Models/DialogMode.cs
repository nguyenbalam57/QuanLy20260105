using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Models
{
    /// <summary>
    /// Dialog mode enum
    /// </summary>
    public enum DialogMode
    {
        Add,      // Thêm mới
        Edit,     // Chỉnh sửa
        View      // Xem chi tiết
    }

    public static class DialogModeExtensions
    {
        /// <summary>
        /// Chuyển DialogMode thành chuỗi mô tả
        /// </summary>
        /// <param name="mode">DialogMode</param>
        /// <returns>Chuỗi mô tả</returns>
        public static string ToDescriptionString(this DialogMode mode)
        {
            switch (mode)
            {
                case DialogMode.Add:
                    return "Thêm mới";
                case DialogMode.Edit:
                    return "Chỉnh sửa";
                case DialogMode.View:
                    return "Xem chi tiết";
                default:
                    return "Không xác định";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static string ToIconString(this DialogMode mode)
        {
            switch (mode)
            {
                case DialogMode.Add:
                    return "➕";
                case DialogMode.Edit:
                    return "✏️";
                case DialogMode.View:
                    return "👁️";
                default:
                    return "❓";
            }
        }
    }
}
