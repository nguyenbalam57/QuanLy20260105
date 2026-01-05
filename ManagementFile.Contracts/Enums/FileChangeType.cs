using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// FileChangeType - Loại thay đổi file
    /// Tracking file operations
    /// </summary>
    public enum FileChangeType
    {
        /// <summary>Created - Tạo mới file</summary>
        [Description("Tạo mới file")]
        Created = 0,

        /// <summary>Modified - Chỉnh sửa content</summary>
        [Description("Chỉnh sửa content")] 
        Modified = 1,

        /// <summary>Deleted - Xóa file</summary>
        [Description("Xóa file")] 
        Deleted = 2,

        /// <summary>Moved - Di chuyển vị trí</summary>
        [Description("Di chuyển vị trí")] 
        Moved = 3,

        /// <summary>Renamed - Đổi tên file</summary>
        [Description("Đổi tên file")] 
        Renamed = 4,

        /// <summary>Copied - Sao chép file</summary>
        [Description("Sao chép file")] 
        Copied = 5,

        /// <summary>Restored - Khôi phục từ backup</summary>
        [Description("Khôi phục từ backup")] 
        Restored = 6
    }
}
