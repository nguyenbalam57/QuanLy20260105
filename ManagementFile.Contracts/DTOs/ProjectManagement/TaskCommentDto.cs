using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.DTOs.ProjectManagement
{
    /// <summary>
    /// DTO cho TaskComment - Bình luận trên task
    /// Chứa tất cả thông tin về bình luận, từ nội dung cơ bản đến tracking resolution
    /// </summary>
    public class TaskCommentDto
    {
        /// <summary>
        /// ID duy nhất của comment
        /// Primary key, auto-generated
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID của task chứa comment này
        /// Foreign key tới ProjectTask
        /// </summary>
        [Required]
        public int TaskId { get; set; }

        /// <summary>
        /// Nội dung chính của comment
        /// Có thể chứa HTML/Markdown, mention users (@username), link files
        /// </summary>
        [Required]
        public string Content { get; set; } = "";

        /// <summary>
        /// ID của comment cha (nếu đây là reply)
        /// null = comment gốc, có giá trị = reply của comment khác
        /// Tạo cấu trúc thread/nested comments
        /// </summary>
        public int? ParentCommentId { get; set; }

        #region Comment Type & Category - Phân loại comment

        /// <summary>
        /// Loại comment chính - định nghĩa mục đích của comment
        /// Values: General, ReviewFeedback, IssueReport, ChangeRequest, 
        /// StatusUpdate, Clarification, Approval, Rejection, Question, Suggestion
        /// Dùng để filter và hiển thị badge
        /// </summary>
        [Required]
        public CommentType CommentType { get; set; } = CommentType.General;

        #endregion

        #region Review & Feedback Properties - Quản lý review và feedback

        /// <summary>
        /// Trạng thái xử lý của comment
        /// Values: Todo, InProgress, InReview, Completed, Blocked, OnHold, Cancelled
        /// Theo dõi lifecycle của comment từ tạo đến resolve
        /// </summary>
        [Required]
        public TaskStatuss CommentStatus { get; set; } = TaskStatuss.Todo;

        /// <summary>
        /// Độ ưu tiên xử lý comment
        /// Values: Critical, High, Medium, Low
        /// Quyết định thứ tự xử lý và deadline
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Low;

        /// <summary>
        /// ID người reviewer - người đưa ra feedback/comment này
        /// Thường là senior dev, team lead, architect
        /// </summary>
        public int? ReviewerId { get; set; }

        /// <summary>
        /// Tên hiển thị của reviewer
        /// Cached để tránh join với User table
        /// </summary>
        public string ReviewerName { get; set; } = "";

        /// <summary>
        /// ID người được assign để xử lý comment này
        /// Developer chịu trách nhiệm fix/resolve issue
        /// </summary>
        public int? AssignedToId { get; set; }

        /// <summary>
        /// Tên hiển thị của assignee
        /// Cached để tránh join với User table
        /// </summary>
        public string AssignedToName { get; set; } = "";

        #endregion

        #region Issue Details - Chi tiết về vấn đề

        /// <summary>
        /// Tiêu đề ngắn gọn của issue/vấn đề
        /// Tóm tắt 1 câu về vấn đề cần fix
        /// VD: "Login button không hoạt động trên IE11"
        /// </summary>
        [StringLength(500)]
        public string IssueTitle { get; set; } = "";

        /// <summary>
        /// Đề xuất cách fix chi tiết
        /// Hướng dẫn technical để resolve issue
        /// VD: "Thay đổi event handler từ addEventListener sang onclick"
        /// </summary>
        public string SuggestedFix { get; set; } = "";

        #endregion

        #region Location & Reference - Vị trí và tham chiếu

        /// <summary>
        /// Module/component liên quan đến comment
        /// VD: "UserAuthentication", "PaymentGateway", "ReportGenerator"
        /// Giúp filter comments theo module
        /// </summary>
        public string RelatedModule { get; set; } = "";

        /// <summary>
        /// Danh sách file code liên quan
        /// VD: ["src/auth/login.cs", "views/login.xaml"]
        /// Link trực tiếp tới source code cần fix
        /// </summary>
        public List<string> RelatedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách screenshot minh họa
        /// Đường dẫn tới file ảnh chụp màn hình bug/issue
        /// VD: ["screenshots/login_error.png", "mockups/new_design.jpg"]
        /// </summary>
        public List<string> RelatedScreenshots { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách document tham khảo
        /// Link tới requirements, design docs, API docs
        /// VD: ["specs/login_requirement.pdf", "api/auth_api.md"]
        /// </summary>
        public List<string> RelatedDocuments { get; set; } = new List<string>();

        #endregion

        #region Attachments & Mentions - File đính kèm và mention

        /// <summary>
        /// Danh sách file đính kèm trong comment
        /// Log files, test results, code snippets
        /// VD: ["error_log.txt", "test_result.xlsx", "code_sample.cs"]
        /// </summary>
        public List<string> Attachments { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách user được mention trong comment
        /// VD: ["john.doe", "jane.smith"] - username hoặc email
        /// Trigger notification cho các user này
        /// </summary>
        public List<string> MentionedUsers { get; set; } = new List<string>();

        #endregion

        #region Resolution Tracking - Theo dõi quá trình giải quyết

        /// <summary>
        /// Thời gian comment được mark as resolved
        /// null = chưa resolve, có giá trị = đã resolve
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// ID người resolve comment
        /// Thường là assignee hoặc reviewer
        /// </summary>
        public int? ResolvedBy { get; set; }

        /// <summary>
        /// Tên người resolve comment
        /// Cached để hiển thị UI
        /// </summary>
        public string ResolvedByName { get; set; } = "";

        /// <summary>
        /// Ghi chú về cách đã resolve
        /// Mô tả chi tiết solution đã implement
        /// VD: "Đã fix bằng cách thay đổi validation logic trong AuthController.Login()"
        /// </summary>
        public string ResolutionNotes { get; set; } = "";

        /// <summary>
        /// ID commit chứa fix cho issue này
        /// Link với version control system (Git/SVN)
        /// VD: "a1b2c3d4e5f6" - Git commit hash
        /// </summary>
        public string ResolutionCommitId { get; set; } = "";

        /// <summary>
        /// Thời gian verify rằng fix đã đúng
        /// Bước cuối trong workflow: resolve -> verify -> close
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// ID người verify fix
        /// Thường là reviewer hoặc QA tester
        /// </summary>
        public int? VerifiedBy { get; set; }

        /// <summary>
        /// Tên người verify
        /// Cached để hiển thị UI
        /// </summary>
        public string VerifiedByName { get; set; } = "";

        /// <summary>
        /// Ghi chú verify
        /// Xác nhận fix đã hoạt động đúng, test cases pass
        /// VD: "Đã test trên Chrome, Firefox, IE11. Login hoạt động bình thường."
        /// </summary>
        public string VerificationNotes { get; set; } = "";

        /// <summary>
        /// Flag đánh dấu comment đã được verify
        /// true = fix đã được confirm working
        /// false = chưa verify hoặc verify failed
        /// </summary>
        public bool IsVerified { get; set; } = false;

        #endregion

        #region Time Tracking - Theo dõi thời gian

        /// <summary>
        /// Thời gian ước tính để fix (tính bằng giờ)
        /// Estimate từ assignee hoặc team lead
        /// VD: 2.5 = 2 giờ 30 phút
        /// </summary>
        public decimal EstimatedFixTime { get; set; } = 0;

        /// <summary>
        /// Thời gian thực tế đã fix (tính bằng giờ)
        /// Actual time spent, dùng để improve estimation
        /// VD: 3.0 = 3 giờ (over estimate 0.5h)
        /// </summary>
        public decimal ActualFixTime { get; set; } = 0;

        /// <summary>
        /// Deadline phải fix xong
        /// Based on priority và project timeline
        /// Critical = same day, High = 1-2 days, Medium = 1 week, Low = next sprint
        /// </summary>
        public DateTime? DueDate { get; set; }

        #endregion

        #region Additional Flags - Các flag bổ sung

        /// <summary>
        /// Comment này có block task development không
        /// true = task không thể continue cho đến khi resolve comment
        /// false = có thể work parallel
        /// </summary>
        public bool IsBlocking { get; set; } = false;

        /// <summary>
        /// Comment cần thảo luận thêm
        /// true = cần meeting/discussion để clarify requirement
        /// false = đã rõ, có thể implement ngay
        /// </summary>
        public bool RequiresDiscussion { get; set; } = false;

        /// <summary>
        /// Assignee có đồng ý với feedback/comment không
        /// true = đồng ý và sẽ implement
        /// false = không đồng ý, cần negotiate
        /// </summary>
        public bool IsAgreed { get; set; } = false;

        /// <summary>
        /// ID người đồng ý với comment
        /// Thường là assignee confirm sẽ implement
        /// </summary>
        public int? AgreedBy { get; set; }

        /// <summary>
        /// Tên người đồng ý
        /// Cached để hiển thị UI
        /// </summary>
        public string AgreedByName { get; set; } = "";

        /// <summary>
        /// Thời gian đồng ý
        /// Timestamp khi assignee accept comment
        /// </summary>
        public DateTime? AgreedAt { get; set; }

        #endregion

        #region Metadata - Dữ liệu bổ sung

        /// <summary>
        /// Các tag phân loại comment
        /// VD: ["bug", "security", "urgent", "ui", "backend"]
        /// Dùng để filter và search
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Metadata mở rộng dạng key-value
        /// Lưu thông tin custom không có trong schema
        /// VD: {"browser": "IE11", "os": "Windows 7", "severity": "high"}
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        #endregion

        #region Base Properties - Thuộc tính cơ sở

        /// <summary>
        /// Thời gian tạo comment
        /// Auto-set khi create, không thay đổi
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// ID người tạo comment
        /// Foreign key tới User table
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Tên người tạo comment
        /// Cached để hiển thị UI, tránh join
        /// </summary>
        public string CreatedByName { get; set; } = "";

        /// <summary>
        /// Thời gian update cuối cùng
        /// Auto-update mỗi khi edit comment
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// ID người update cuối cùng
        /// Track user thực hiện thay đổi gần nhất
        /// </summary>
        public int? UpdatedBy { get; set; }

        /// <summary>
        /// Tên người update cuối cùng
        /// Cached để hiển thị "Edited by John at 10:30 AM"
        /// </summary>
        public string UpdatedByName { get; set; } = "";

        /// <summary>
        /// Version cho optimistic concurrency control
        /// Tăng mỗi lần update, prevent concurrent edit conflicts
        /// </summary>
        public long Version { get; set; } = 1;

        #endregion

        /// <summary>
        /// Đánh hệ dấu comment hệ thống
        /// </summary>
        public bool IsSystemComment { get; set; } = false;

        #region Navigation - Dữ liệu liên quan

        public int TotalReplyCount { get; set; } = 0;

        /// <summary>
        /// Danh sách reply comments
        /// Comments có ParentCommentId = this.Id
        /// Load lazy hoặc eager tùy business need
        /// </summary>
        public List<TaskCommentDto> Replies { get; set; } = new List<TaskCommentDto>();



        #endregion
    }
}
