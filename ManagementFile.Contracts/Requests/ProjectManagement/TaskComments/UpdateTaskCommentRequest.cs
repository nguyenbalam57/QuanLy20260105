using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TaskComments
{
    /// <summary>
    /// DTO để cập nhật TaskComment
    /// Kế thừa từ BaseTaskCommentDto và thêm các field đặc thù cho Update
    /// </summary>
    public class UpdateTaskCommentRequest : BaseTaskCommentRequest
    {
        /// <summary>
        /// ID của comment cần update
        /// Required để identify record
        /// Must exist trong database
        /// </summary>
        [Required(ErrorMessage = "Id comment là bắt buộc")]
        public int Id { get; set; }

        /// <summary>
        /// Trạng thái xử lý của comment - chỉ có trong Update
        /// Values: Todo, InProgress, InReview, Completed, Blocked, OnHold, Cancelled
        /// Create mặc định = Todo, Update có thể change status
        /// </summary>
        [Required(ErrorMessage = "Trạng thái comment là bắt buộc")]
        public TaskStatuss CommentStatus { get; set; } = TaskStatuss.Todo;

        /// <summary>
        /// Version cho optimistic concurrency control
        /// Must match current version trong DB để update
        /// Prevent concurrent modification conflicts
        /// Client phải lấy latest version trước khi update
        /// </summary>
        [Required(ErrorMessage = "Version là bắt buộc cho concurrency control")]
        public long Version { get; set; }

        /// <summary>
        /// Gửi notification khi update không
        /// true = notify assignee, reviewer, mentioned users
        /// false = silent update
        /// Default true trừ khi là minor update
        /// </summary>
        public bool SendNotification { get; set; } = true;

        #region Update-specific Resolution Fields

        /// <summary>
        /// Ghi chú giải quyết - chỉ khi update status = Completed
        /// Required khi mark as resolved
        /// Mô tả cách đã fix issue
        /// </summary>
        [StringLength(2000, ErrorMessage = "Ghi chú giải quyết không được quá 2000 ký tự")]
        public string ResolutionNotes { get; set; } = "";

        /// <summary>
        /// Commit ID chứa fix - optional
        /// Link với version control system
        /// VD: Git commit hash, SVN revision
        /// </summary>
        [StringLength(100, ErrorMessage = "Commit ID không được quá 100 ký tự")]
        public string ResolutionCommitId { get; set; } = "";

        /// <summary>
        /// Ghi chú verification - khi verify fix
        /// Required khi mark as verified
        /// Confirm fix hoạt động đúng
        /// </summary>
        [StringLength(2000, ErrorMessage = "Ghi chú verification không được quá 2000 ký tự")]
        public string VerificationNotes { get; set; } = "";

        /// <summary>
        /// Đánh dấu đã verify fix
        /// true = fix confirmed working
        /// false = chưa verify hoặc verify failed
        /// </summary>
        public bool IsVerified { get; set; } = false;


        #endregion
    }
}
