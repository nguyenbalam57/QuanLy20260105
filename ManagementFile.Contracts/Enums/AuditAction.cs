using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// AuditAction - Hành động kiểm toán
    /// Định nghĩa tất cả các loại hành động có thể được ghi lại trong hệ thống
    /// </summary>
    public enum AuditAction
    {
        [Description("Other")]
        Other = 0,

        /// <summary>
        /// Tạo mới entity
        /// </summary>
        [Description("Create")]
        Create = 1,

        /// <summary>
        /// Cập nhật entity
        /// </summary>
        [Description("Update")]
        Update = 2,

        /// <summary>
        /// Xóa entity
        /// </summary>
        [Description("Delete")]
        Delete = 3,

        /// <summary>
        /// Đọc/Xem thông tin entity
        /// </summary>
        [Description("Read")]
        Read = 4,

        /// <summary>
        /// Xem danh sách
        /// </summary>
        [Description("View")]
        View = 5,

        /// <summary>
        /// Load lỗi
        /// </summary>
        [Description("Load Error")]
        LoadError = 6,

        /// <summary>
        /// Trả lời/Phản hồi
        Reply = 7,

        /// <summary>
        /// Đăng nhập hệ thống
        /// </summary>
        [Description("Login")]
        Login = 10,

        /// <summary>
        /// Đăng xuất hệ thống
        /// </summary>
        [Description("Logout")]
        Logout = 11,

        /// <summary>
        /// Thay đổi mật khẩu
        /// </summary>
        [Description("Change Password")]
        ChangePassword = 12,

        /// <summary>
        /// Reset mật khẩu
        /// </summary>
        [Description("Reset Password")]
        ResetPassword = 13,

        /// <summary>
        /// Upload file
        /// </summary>
        [Description("Upload")]
        Upload = 20,

        /// <summary>
        /// Download file
        /// </summary>
        [Description("Download")]
        Download = 21,

        /// <summary>
        /// Share/Chia sẻ file
        /// </summary>
        [Description("Share")]
        Share = 22,

        /// <summary>
        /// Approve/Phê duyệt
        /// </summary>
        [Description("Approve")]
        Approve = 30,

        /// <summary>
        /// Reject/Từ chối
        /// </summary>
        [Description("Reject")]
        Reject = 31,

        /// <summary>
        /// Submit/Gửi để phê duyệt
        /// </summary>
        [Description("Submit")]
        Submit = 32,

        /// <summary>
        /// Assign/Gán nhiệm vụ
        /// </summary>
        [Description("Assign")]
        Assign = 40,

        /// <summary>
        /// Complete/Hoàn thành
        /// </summary>
        [Description("Complete")]
        Complete = 41,

        /// <summary>
        /// Archive/Lưu trữ
        /// </summary>
        [Description("Archive")]
        Archive = 50,

        /// <summary>
        /// Restore/Khôi phục
        /// </summary>
        [Description("Restore")]
        Restore = 51,

        /// <summary>
        /// Export dữ liệu
        /// </summary>
        [Description("Export")]
        Export = 60,

        /// <summary>
        /// Import dữ liệu
        /// </summary>
        [Description("Import")]
        Import = 61,

        /// <summary>
        /// Backup dữ liệu
        /// </summary>
        [Description("Backup")]
        Backup = 70,

        /// <summary>
        /// Restore backup
        /// </summary>
        [Description("Restore Backup")]
        RestoreBackup = 71,

        /// <summary>
        /// Thay đổi quyền truy cập
        /// </summary>
        [Description("Change Permission")]
        ChangePermission = 80,

        /// <summary>
        /// Grant permission/Cấp quyền
        /// </summary>
        [Description("Grant Permission")]
        GrantPermission = 81,

        /// <summary>
        /// Revoke permission/Thu hồi quyền
        /// </summary>
        [Description("Revoke Permission")]
        RevokePermission = 82,

        /// <summary>
        /// Print/In
        /// </summary>
        [Description("Print")]
        Print = 90,

        /// <summary>
        /// Send notification/Gửi thông báo
        /// </summary>
        [Description("Send Notification")]
        SendNotification = 100,

        /// <summary>
        /// System configuration change/Thay đổi cấu hình hệ thống
        /// </summary>
        [Description("System Config Change")]
        SystemConfigChange = 999,


    }
}
