using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// File comment response DTO
    /// </summary>
    public class FileCommentDto
    {
        public int Id { get; set; } 
        public int FileVersionId { get; set; }
        public string Content { get; set; } = "";
        public int? LineNumber { get; set; }
        public int? StartColumn { get; set; }
        public int? EndColumn { get; set; }
        public string CommentType { get; set; } = "";
        public int ParentCommentId { get; set; } 
        public bool IsResolved { get; set; }
        public int ResolvedBy { get; set; } 
        public string ResolvedByName { get; set; } = "";
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; } 
        public string CreatedByName { get; set; } = "";
        public List<FileCommentDto> Replies { get; set; } = new List<FileCommentDto>();
    }
}
