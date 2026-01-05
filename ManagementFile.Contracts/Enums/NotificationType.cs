using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// NotificationType - Loại thông báo hệ thống
    /// Định nghĩa các loại thông báo khác nhau trong hệ thống quản lý file
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Info - Thông báo thông tin chung
        /// </summary>
        [Description("Information")]
        Info = 1,

        /// <summary>
        /// Warning - Cảnh báo
        /// </summary>
        [Description("Warning")]
        Warning = 2,

        /// <summary>
        /// Error - Lỗi hệ thống
        /// </summary>
        [Description("Error")]
        Error = 3,

        /// <summary>
        /// Success - Thông báo thành công
        /// </summary>
        [Description("Success")]
        Success = 4,

        /// <summary>
        /// TaskAssigned - Được giao task mới
        /// </summary>
        [Description("Task Assigned")]
        TaskAssigned = 9,

        /// <summary>
        /// TaskReporter - Báo cáo task
        /// </summary>
        [Description("Task Reporter")]
        TaskReporter = 10,

        /// <summary>
        /// TaskUpdated - Task được cập nhật
        /// </summary>
        [Description("Task Updated")]
        TaskUpdated = 11,

        /// <summary>
        /// TaskCompleted - Task hoàn thành
        /// </summary>
        [Description("Task Completed")]
        TaskCompleted = 12,

        /// <summary>
        /// TaskOverdue - Task quá hạn
        /// </summary>
        [Description("Task Overdue")]
        TaskOverdue = 13,

        /// <summary>
        /// TaskDeadlineReminder - Nhắc nhở deadline task
        /// </summary>
        [Description("Task Deadline Reminder")]
        TaskDeadlineReminder = 14,

        /// <summary>
        /// TaskCommentAdded - Có comment mới trong task
        /// </summary>
        [Description("Task Comment Added")]
        TaskCommentAdded = 15,

        /// <summary>
        /// ProjectInvitation - Mời tham gia project
        /// </summary>
        [Description("Project Invitation")]
        ProjectInvitation = 20,

        /// <summary>
        /// ProjectUpdated - Project được cập nhật
        /// </summary>
        [Description("Project Updated")]
        ProjectUpdated = 21,

        /// <summary>
        /// ProjectCompleted - Project hoàn thành
        /// </summary>
        [Description("Project Completed")]
        ProjectCompleted = 22,

        /// <summary>
        /// ProjectDeadlineReminder - Nhắc nhở deadline project
        /// </summary>
        [Description("Project Deadline Reminder")]
        ProjectDeadlineReminder = 23,

        /// <summary>
        /// ProjectMemberAdded - Thành viên mới tham gia project
        /// </summary>
        [Description("Project Member Added")]
        ProjectMemberAdded = 24,

        /// <summary>
        /// ProjectMemberRemoved - Thành viên rời project
        /// </summary>
        [Description("Project Member Removed")]
        ProjectMemberRemoved = 25,

        /// <summary>
        /// FileUploaded - File mới được upload
        /// </summary>
        [Description("File Uploaded")]
        FileUploaded = 30,

        /// <summary>
        /// FileUpdated - File được cập nhật
        /// </summary>
        [Description("File Updated")]
        FileUpdated = 31,

        /// <summary>
        /// FileShared - File được chia sẻ
        /// </summary>
        [Description("File Shared")]
        FileShared = 32,

        /// <summary>
        /// FileCommentAdded - Có comment mới trong file
        /// </summary>
        [Description("File Comment Added")]
        FileCommentAdded = 33,

        /// <summary>
        /// FileVersionCreated - Tạo version mới của file
        /// </summary>
        [Description("File Version Created")]
        FileVersionCreated = 34,

        /// <summary>
        /// FileCheckedOut - File được checkout
        /// </summary>
        [Description("File Checked Out")]
        FileCheckedOut = 35,

        /// <summary>
        /// FileCheckedIn - File được checkin
        /// </summary>
        [Description("File Checked In")]
        FileCheckedIn = 36,

        /// <summary>
        /// FileOverdueCheckout - File checkout quá hạn
        /// </summary>
        [Description("File Overdue Checkout")]
        FileOverdueCheckout = 37,

        /// <summary>
        /// ApprovalRequired - Cần phê duyệt
        /// </summary>
        [Description("Approval Required")]
        ApprovalRequired = 40,

        /// <summary>
        /// ApprovalApproved - Đã được phê duyệt
        /// </summary>
        [Description("Approval Approved")]
        ApprovalApproved = 41,

        /// <summary>
        /// ApprovalRejected - Bị từ chối phê duyệt
        /// </summary>
        [Description("Approval Rejected")]
        ApprovalRejected = 42,

        /// <summary>
        /// ReviewRequest - Yêu cầu review
        /// </summary>
        [Description("Review Request")]
        ReviewRequest = 43,

        /// <summary>
        /// ReviewCompleted - Review hoàn thành
        /// </summary>
        [Description("Review Completed")]
        ReviewCompleted = 44,

        /// <summary>
        /// UserMentioned - Được mention trong comment
        /// </summary>
        [Description("User Mentioned")]
        UserMentioned = 50,

        /// <summary>
        /// MessageReceived - Nhận tin nhắn mới
        /// </summary>
        [Description("Message Received")]
        MessageReceived = 51,

        /// <summary>
        /// SystemMaintenance - Bảo trì hệ thống
        /// </summary>
        [Description("System Maintenance")]
        SystemMaintenance = 60,

        /// <summary>
        /// SystemUpdate - Cập nhật hệ thống
        /// </summary>
        [Description("System Update")]
        SystemUpdate = 61,

        /// <summary>
        /// AccountSecurityAlert - Cảnh báo bảo mật tài khoản
        /// </summary>
        [Description("Account Security Alert")]
        AccountSecurityAlert = 62,

        /// <summary>
        /// PasswordExpiring - Mật khẩu sắp hết hạn
        /// </summary>
        [Description("Password Expiring")]
        PasswordExpiring = 63,

        /// <summary>
        /// LoginFromNewDevice - Đăng nhập từ thiết bị mới
        /// </summary>
        [Description("Login From New Device")]
        LoginFromNewDevice = 64,

        /// <summary>
        /// WorkflowStarted - Workflow được khởi tạo
        /// </summary>
        [Description("Workflow Started")]
        WorkflowStarted = 70,

        /// <summary>
        /// WorkflowCompleted - Workflow hoàn thành
        /// </summary>
        [Description("Workflow Completed")]
        WorkflowCompleted = 71,

        /// <summary>
        /// WorkflowStepAssigned - Được giao bước workflow
        /// </summary>
        [Description("Workflow Step Assigned")]
        WorkflowStepAssigned = 72,

        /// <summary>
        /// CalendarEventReminder - Nhắc nhở sự kiện lịch
        /// </summary>
        [Description("Calendar Event Reminder")]
        CalendarEventReminder = 80,

        /// <summary>
        /// MeetingInvitation - Mời họp
        /// </summary>
        [Description("Meeting Invitation")]
        MeetingInvitation = 81,

        /// <summary>
        /// MeetingCancelled - Hủy họp
        /// </summary>
        [Description("Meeting Cancelled")]
        MeetingCancelled = 82,

        /// <summary>
        /// ReportGenerated - Báo cáo được tạo
        /// </summary>
        [Description("Report Generated")]
        ReportGenerated = 90,

        /// <summary>
        /// BackupCompleted - Backup hoàn thành
        /// </summary>
        [Description("Backup Completed")]
        BackupCompleted = 91,

        /// <summary>
        /// BackupFailed - Backup thất bại
        /// </summary>
        [Description("Backup Failed")]
        BackupFailed = 92,

        /// <summary>
        /// CustomNotification - Thông báo tùy chỉnh
        /// </summary>
        [Description("Custom Notification")]
        CustomNotification = 999
    }
}
