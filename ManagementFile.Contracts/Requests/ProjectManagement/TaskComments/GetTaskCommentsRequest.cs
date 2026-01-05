using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TaskComments
{
    /// <summary>
    /// Request để lấy danh sách TaskComment với filtering mạnh mẽ
    /// Hỗ trợ pagination, sorting, filtering theo nhiều tiêu chí
    /// </summary>
    public class GetTaskCommentsRequest
    {
        /// <summary>
        /// ID task cần lấy comments
        /// Required - must be valid ProjectTask.Id
        /// </summary>
        [Required(ErrorMessage = "TaskId là bắt buộc")]
        public int TaskId { get; set; }

        #region Pagination - Phân trang

        /// <summary>
        /// Trang hiện tại (bắt đầu từ 1)
        /// Default = 1, Min = 1
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn 0")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Số records mỗi trang
        /// Default = 20, Max = 100 để tránh performance issue
        /// </summary>
        [Range(1, 100, ErrorMessage = "Kích thước trang phải từ 1 đến 100")]
        public int PageSize { get; set; } = 20;

        #endregion

        #region Filtering - Lọc theo nhiều tiêu chí

        /// <summary>
        /// Lất parent comment cụ thể
        /// </summary>
        public int? ParentTaskCommentId { get; set; }

        /// <summary>
        /// Filter theo loại comment
        /// Empty list = lấy tất cả loại
        /// VD: ["General", "IssueReport", "StatusUpdate"]
        /// </summary>
        public List<CommentType> CommentTypes { get; set; } = new List<CommentType>();

        /// <summary>
        /// Filter theo trạng thái xử lý
        /// Empty list = lấy tất cả status
        /// VD: ["Todo", "InProgress", "Completed"]
        /// </summary>
        public List<TaskStatuss> CommentStatuses { get; set; } = new List<TaskStatuss>();

        /// <summary>
        /// Filter theo độ ưu tiên
        /// Empty list = lấy tất cả priority
        /// VD: ["Critical", "High"]
        /// </summary>
        public List<TaskPriority> Priorities { get; set; } = new List<TaskPriority>();

        /// <summary>
        /// Filter theo reviewer cụ thể
        /// null = không filter theo reviewer
        /// </summary>
        public int? ReviewerId { get; set; }

        /// <summary>
        /// Filter theo assignee cụ thể
        /// null = không filter theo assignee
        /// </summary>
        public int? AssignedToId { get; set; }

        /// <summary>
        /// Filter theo người tạo comment
        /// null = không filter theo creator
        /// </summary>
        public int? CreatedBy { get; set; }

        /// <summary>
        /// Filter từ ngày (inclusive)
        /// null = không giới hạn từ ngày
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter đến ngày (inclusive)
        /// null = không giới hạn đến ngày
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter comments đã resolve
        /// null = lấy cả resolved và chưa resolved
        /// true = chỉ lấy resolved, false = chỉ lấy chưa resolved
        /// </summary>
        public bool? IsResolved { get; set; }

        /// <summary>
        /// Filter comments đã verify
        /// null = lấy cả verified và chưa verified
        /// true = chỉ lấy verified, false = chỉ lấy chưa verified
        /// </summary>
        public bool? IsVerified { get; set; }

        /// <summary>
        /// Filter comments đang block task
        /// null = lấy cả blocking và non-blocking
        /// true = chỉ lấy blocking, false = chỉ lấy non-blocking
        /// </summary>
        public bool? IsBlocking { get; set; }

        /// <summary>
        /// Filter comments cần discussion
        /// null = lấy tất cả
        /// true = chỉ lấy cần discussion, false = không cần discussion
        /// </summary>
        public bool? RequiresDiscussion { get; set; }

        /// <summary>
        /// Filter theo tags cụ thể
        /// Empty list = không filter theo tag
        /// VD: ["bug", "urgent"] = chỉ lấy comments có ít nhất 1 trong 2 tags này
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        #endregion

        #region Sorting - Sắp xếp

        /// <summary>
        /// Field để sort
        /// Values: "CreatedAt", "UpdatedAt", "Priority", "DueDate", "CommentType"
        /// Default = "CreatedAt" để hiển thị comments mới nhất
        /// </summary>
        [StringLength(50, ErrorMessage = "Trường sắp xếp không được quá 50 ký tự")]
        public string SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// Hướng sắp xếp
        /// "ASC" = tăng dần, "DESC" = giảm dần
        /// Default = "DESC" để hiển thị mới nhất đầu tiên
        /// </summary>
        [StringLength(10, ErrorMessage = "Hướng sắp xếp phải là ASC hoặc DESC")]
        public string SortDirection { get; set; } = "DESC";

        #endregion

        #region Include Options - Tùy chọn include dữ liệu

        /// <summary>
        /// Có include replies không
        /// true = load nested comments (performance cost)
        /// false = chỉ load comments gốc (faster)
        /// </summary>
        public bool IncludeReplies { get; set; } = true;

        /// <summary>
        /// Có include system comments không
        /// true = bao gồm comments tự động (status change, assign, etc.)
        /// false = chỉ user comments
        /// </summary>
        public bool IncludeSystemComments { get; set; } = true;

        /// <summary>
        /// Có include comments đã soft delete không
        /// true = bao gồm deleted comments (admin view)
        /// false = chỉ active comments (normal view)
        /// </summary>
        public bool IncludeDeleted { get; set; } = false;

        #endregion

        #region Search - Tìm kiếm

        /// <summary>
        /// Tìm kiếm trong nội dung comment
        /// Empty = không search content
        /// Support full-text search hoặc LIKE %term%
        /// </summary>
        [StringLength(500, ErrorMessage = "Từ khóa tìm kiếm không được quá 500 ký tự")]
        public string SearchContent { get; set; } = "";

        /// <summary>
        /// Tìm kiếm trong tiêu đề issue
        /// Empty = không search issue title
        /// </summary>
        [StringLength(200, ErrorMessage = "Từ khóa tìm kiếm tiêu đề không được quá 200 ký tự")]
        public string SearchIssueTitle { get; set; } = "";

        #endregion
    }
}
