using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TaskComments
{
    /// <summary>
    /// DTO cho các hành động đặc biệt trên TaskComment
    /// Mục đích : đã được phân công, đã được giải quyết, đã được xác minh, đồng ý, từ chối
    /// </summary>
    public class ResolveTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }

        [StringLength(2000)]
        public string ResolutionNotes { get; set; } = "";

        [StringLength(100)]
        public string ResolutionCommitId { get; set; } = "";

        [Range(0, 999.99)]
        public decimal ActualFixTime { get; set; } = 0;

        [Required]
        public long Version { get; set; }

    }

    /// <summary>
    /// DTO để verify TaskComment
    /// </summary>
    public class VerifyTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }

        [StringLength(2000)]
        public string VerificationNotes { get; set; } = "";

        [Required]
        public long Version { get; set; }

    }

    /// <summary>
    /// DTO để agree với TaskComment
    /// </summary>
    public class AgreeTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        public bool IsAgreed { get; set; }

        [Required]
        public long Version { get; set; }

    }

    /// <summary>
    /// DTO để assign TaskComment
    /// </summary>
    public class AssignTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        public int AssignedToId { get; set; }

        [Required]
        public long Version { get; set; }

    }

    /// <summary>
    /// DTO để gán ai là người review TaskComment
    /// </summary>
    public class ReviewTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }
        [Required]
        public int ReviewerId { get; set; }

        [Required]
        public long Version { get; set; }
    }

    public class PriorityTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }
        [Required]
        public TaskPriority Priority { get; set; }
        [Required]
        public long Version { get; set; }
    }

    public class ToggleBlockingTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }
        [Required]
        public bool IsBlocking { get; set; }
        [Required]
        public long Version { get; set; }
    }

    public class ToggleDiscussionTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }
        [Required]
        public bool RequiresDiscussion { get; set; }
        [Required]
        public long Version { get; set; }
    }

    public class StatusTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }
        [Required]
        public TaskStatuss Status { get; set; }
        [Required]
        public long Version { get; set; }
    }

    public class CommentTypeTaskCommentRequest
    {
        [Required]
        public int CommentId { get; set; }
        [Required]
        public CommentType CommentType { get; set; }
        [Required]
        public long Version { get; set; }
    }


}
