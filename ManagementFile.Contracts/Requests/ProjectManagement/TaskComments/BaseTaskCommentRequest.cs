using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TaskComments
{
    /// <summary>
    /// Base DTO chung cho Create và Update TaskComment
    /// Chứa các field chung để tránh code duplication
    /// </summary>
    public abstract class BaseTaskCommentRequest
    {
        /// <summary>
        /// Nội dung comment - field chính nhất
        /// Không được empty, max 4000 chars (SQL Server nvarchar limit)
        /// Có thể chứa markdown, HTML basic, mention syntax
        /// </summary>
        [Required(ErrorMessage = "Nội dung comment không được để trống")]
        [StringLength(4000, MinimumLength = 1, ErrorMessage = "Nội dung comment phải từ 1-4000 ký tự")]
        public string Content { get; set; } = "";

        #region Comment Type & Category - Phân loại comment

        /// <summary>
        /// Loại comment - dropdown có sẵn options
        /// Default "General" cho comment thông thường
        /// Values được validate bởi business rules
        /// </summary>
        public CommentType CommentType { get; set; } = CommentType.General;

        #endregion

        #region Review & Feedback Properties - Gán người xử lý

        /// <summary>
        /// Độ ưu tiên - ảnh hưởng đến DueDate
        /// Critical = cần fix ngay, Low = có thể để sau
        /// Default Medium cho comment thông thường
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Low;

        /// <summary>
        /// ID reviewer nếu cần assign review
        /// Optional, có thể assign sau
        /// Must be valid User.Id if provided
        /// </summary>
        public int? ReviewerId { get; set; }

        /// <summary>
        /// ID assignee - người chịu trách nhiệm fix
        /// Optional khi tạo, có thể assign sau
        /// Auto change status to InProgress when assigned
        /// </summary>
        public int? AssignedToId { get; set; }

        #endregion

        #region Issue Details - Chi tiết vấn đề

        /// <summary>
        /// Tiêu đề ngắn gọn của issue
        /// Optional, để trống nếu comment không phải issue
        /// Max 500 chars cho database performance
        /// </summary>
        [StringLength(500, ErrorMessage = "Tiêu đề issue không được quá 500 ký tự")]
        public string IssueTitle { get; set; } = "";

        /// <summary>
        /// Đề xuất solution chi tiết
        /// Optional, reviewer có thể đưa ra suggested fix
        /// Max 2000 chars để giữ DB performance
        /// </summary>
        [StringLength(2000, ErrorMessage = "Đề xuất fix không được quá 2000 ký tự")]
        public string SuggestedFix { get; set; } = "";

        #endregion

        #region Location & Reference - Context location

        /// <summary>
        /// Module/component liên quan
        /// Help assignee locate code faster
        /// VD: "Authentication", "Payment", "Reporting"
        /// </summary>
        [StringLength(1000, ErrorMessage = "Module liên quan không được quá 1000 ký tự")]
        public string RelatedModule { get; set; } = "";

        /// <summary>
        /// File paths liên quan đến comment
        /// Help developer navigate to exact files
        /// VD: ["src/controllers/AuthController.cs", "views/Login.xaml"]
        /// </summary>
        public List<string> RelatedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Screenshots minh họa issue/requirement
        /// File paths hoặc URLs tới images
        /// Upload riêng rồi pass path vào đây
        /// </summary>
        public List<string> RelatedScreenshots { get; set; } = new List<string>();

        /// <summary>
        /// Documents tham khảo thêm
        /// Requirements docs, API specs, design docs
        /// File paths hoặc URLs
        /// </summary>
        public List<string> RelatedDocuments { get; set; } = new List<string>();

        #endregion

        #region Attachments & Mentions - File và mention

        /// <summary>
        /// File đính kèm với comment
        /// Log files, code samples, test results
        /// Upload riêng rồi pass file paths vào
        /// </summary>
        public List<string> Attachments { get; set; } = new List<string>();

        /// <summary>
        /// Users được mention trong comment
        /// Trigger notifications cho users này
        /// VD: ["john.doe", "jane.smith"] - usernames
        /// </summary>
        public List<string> MentionedUsers { get; set; } = new List<string>();

        #endregion

        #region Time Tracking - Ước tính thời gian

        /// <summary>
        /// Ước tính thời gian fix (giờ)
        /// Optional, có thể estimate sau
        /// Range 0-999.99 hours (reasonable limit)
        /// </summary>
        [Range(0, 999.99, ErrorMessage = "Thời gian ước tính phải từ 0 đến 999.99 giờ")]
        public decimal EstimatedFixTime { get; set; } = 0;

        /// <summary>
        /// Thời gian thực tế đã fix (giờ) - chỉ có trong Update
        /// Actual time spent, dùng để improve estimation
        /// VD: 3.0 = 3 giờ (over estimate 0.5h)
        /// </summary>
        [Range(0, 999.99, ErrorMessage = "Thời gian thực tế phải từ 0 đến 999.99 giờ")]
        public decimal ActualFixTime { get; set; } = 0;

        /// <summary>
        /// Deadline phải fix xong
        /// Optional, auto-calculate dựa trên Priority nếu không set
        /// Critical = today, High = 2 days, Medium = 1 week, Low = next sprint
        /// </summary>
        public DateTime? DueDate { get; set; }

        #endregion

        #region Additional Flags - Behavioral flags

        /// <summary>
        /// Comment này có block task development không
        /// true = task không thể tiếp tục cho đến khi resolve
        /// Default false cho most comments
        /// </summary>
        public bool IsBlocking { get; set; } = false;

        /// <summary>
        /// Cần thảo luận thêm không
        /// true = schedule meeting để clarify
        /// false = requirement đã rõ, có thể implement
        /// </summary>
        public bool RequiresDiscussion { get; set; } = false;

        /// <summary>
        /// Assignee có đồng ý với feedback/comment không - chỉ có trong Update
        /// true = đồng ý và sẽ implement
        /// false = không đồng ý, cần negotiate
        /// </summary>
        public bool IsAgreed { get; set; } = false;

        #endregion

        #region Metadata - Custom data

        /// <summary>
        /// Tags để categorize comment
        /// VD: ["bug", "ui", "backend", "urgent"]
        /// Dùng cho filtering và reporting
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Custom metadata mở rộng
        /// JSON object chứa thông tin custom
        /// VD: {"browser": "Chrome", "version": "v1.2", "environment": "staging"}
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        #endregion
    }
}
