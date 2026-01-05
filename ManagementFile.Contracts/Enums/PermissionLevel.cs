using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// PermissionLevel - Cấp độ phân quyền truy cập file
    /// Định nghĩa các mức quyền hạn từ thấp đến cao trong hệ thống quản lý file
    /// Áp dụng mô hình phân quyền tăng dần (hierarchical permissions)
    /// </summary>
    public enum PermissionLevel
    {
        /// <summary>None - Không có quyền truy cập</summary>
        /// <remarks>
        /// - Không thể xem, tải xuống, hoặc thực hiện bất kỳ thao tác nào với file
        /// - Thường được sử dụng để thu hồi quyền truy cập
        /// - User không xuất hiện trong danh sách chia sẻ
        /// </remarks>
        [Description("Không có quyền truy cập")]
        None = 0,

        /// <summary>Reader - Quyền đọc cơ bản</summary>
        /// <remarks>
        /// Các quyền được cấp:
        /// - Xem nội dung file (Read)
        /// - Tải xuống file (Download)  
        /// - In file (Print)
        /// - Xem lịch sử phiên bản (View Version History)
        /// </remarks>
        [Description("Quyền xem và tải xuống")]
        Reader = 1,

        /// <summary>Reviewer - Quyền phê duyệt và nhận xét</summary>
        /// <remarks>
        /// Kế thừa tất cả quyền của Reader, bổ sung:
        /// - Thêm nhận xét (Add Comments)
        /// - Phê duyệt/từ chối (Approve/Reject)
        /// - Xem và trả lời comments
        /// - Tạo review request
        /// - Theo dõi workflow approval
        /// </remarks>
        [Description("Quyền xem, nhận xét và phê duyệt")]
        Reviewer = 2,

        /// <summary>Editor - Quyền chỉnh sửa nội dung</summary>
        /// <remarks>
        /// Kế thừa tất cả quyền của Reviewer, bổ sung:
        /// - Chỉnh sửa nội dung file (Write/Edit)
        /// - Checkout file để chỉnh sửa độc quyền
        /// - Checkin file sau khi chỉnh sửa
        /// - Tạo phiên bản mới (Create New Version)
        /// - Upload file thay thế
        /// - Khôi phục phiên bản cũ (Restore Previous Version)
        /// </remarks>
        [Description("Quyền chỉnh sửa và quản lý phiên bản")]
        Editor = 3,

        /// <summary>Owner - Quyền quản trị đầy đủ</summary>
        /// <remarks>
        /// Kế thừa tất cả quyền của Editor, bổ sung:
        /// - Xóa file vĩnh viễn (Delete)
        /// - Quản lý quyền truy cập (Manage Permissions)
        /// - Chia sẻ với người dùng khác (Share)
        /// - Thay đổi chủ sở hữu (Transfer Ownership)
        /// - Cấu hình workflow approval
        /// - Quản lý metadata và thuộc tính file
        /// - Xem audit log đầy đủ
        /// </remarks>
        [Description("Quyền quản trị đầy đủ")]
        Owner = 4
    }

    /// <summary>
    /// File action permissions - Định nghĩa các hành động cụ thể
    /// </summary>
    [Flags]
    public enum FileAction
    {
        None = 0,
        Read = 1 << 0,              // 1
        Download = 1 << 1,          // 2  
        Print = 1 << 2,             // 4
        Comment = 1 << 3,           // 8
        Approve = 1 << 4,           // 16
        Write = 1 << 5,             // 32
        Checkout = 1 << 6,          // 64
        CreateVersion = 1 << 7,     // 128
        Delete = 1 << 8,            // 256
        ManagePermissions = 1 << 9, // 512
        Share = 1 << 10,            // 1024
        TransferOwnership = 1 << 11 // 2048
    }

    /// <summary>
    /// Permission validation và utility methods
    /// </summary>
    public static class PermissionHelper
    {
        /// <summary>
        /// Mapping từ PermissionLevel sang FileAction
        /// </summary>
        private static readonly Dictionary<PermissionLevel, FileAction> _permissionMapping =
            new Dictionary<PermissionLevel, FileAction>
            {
                [PermissionLevel.None] = FileAction.None,

                [PermissionLevel.Reader] =
                    FileAction.Read | FileAction.Download | FileAction.Print,

                [PermissionLevel.Reviewer] =
                    FileAction.Read | FileAction.Download | FileAction.Print |
                    FileAction.Comment | FileAction.Approve,

                [PermissionLevel.Editor] =
                    FileAction.Read | FileAction.Download | FileAction.Print |
                    FileAction.Comment | FileAction.Approve |
                    FileAction.Write | FileAction.Checkout | FileAction.CreateVersion,

                [PermissionLevel.Owner] =
                    FileAction.Read | FileAction.Download | FileAction.Print |
                    FileAction.Comment | FileAction.Approve |
                    FileAction.Write | FileAction.Checkout | FileAction.CreateVersion |
                    FileAction.Delete | FileAction.ManagePermissions | FileAction.Share |
                    FileAction.TransferOwnership
            };

        /// <summary>
        /// Kiểm tra permission level có quyền thực hiện action không
        /// </summary>
        public static bool HasPermission(PermissionLevel level, FileAction action)
        {
            if (!_permissionMapping.TryGetValue(level, out FileAction allowedActions))
                return false;

            return allowedActions.HasFlag(action);
        }

        /// <summary>
        /// Lấy tất cả actions được phép cho permission level
        /// </summary>
        public static FileAction GetAllowedActions(PermissionLevel level)
        {
            return _permissionMapping.TryGetValue(level, out FileAction actions)
                ? actions : FileAction.None;
        }

        /// <summary>
        /// Kiểm tra có thể nâng cấp từ level cũ lên level mới không
        /// </summary>
        public static bool CanUpgrade(PermissionLevel currentLevel, PermissionLevel newLevel)
        {
            return (int)newLevel > (int)currentLevel;
        }

        /// <summary>
        /// Kiểm tra có thể hạ cấp từ level cũ xuống level mới không
        /// </summary>
        public static bool CanDowngrade(PermissionLevel currentLevel, PermissionLevel newLevel)
        {
            return (int)newLevel < (int)currentLevel;
        }
    }
}