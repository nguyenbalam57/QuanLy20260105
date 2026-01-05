using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Create file comment request DTO
    /// </summary>
    public class CreateFileCommentRequest
    {
        [Required]
        public int FileVersionId { get; set; } 

        [Required]
        public string Content { get; set; } = "";

        public int? LineNumber { get; set; }
        public int? StartColumn { get; set; }
        public int? EndColumn { get; set; }

        [StringLength(50)]
        public string CommentType { get; set; } = "General"; // General, CodeReview, Bug, Suggestion, Question

        public int ParentCommentId { get; set; } 
    }
}
