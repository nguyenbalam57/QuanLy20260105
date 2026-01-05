using ManagementFile.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ManagementFile.Models.FileManagement
{
    /// <summary>
    /// FileComment - Bình luận trên file
    /// Comments, reviews, feedback trên các files
    /// </summary>
    [Table("FileComments")]
    [Index(nameof(FileVersionId), nameof(CreatedAt))]
    public class FileComment : ActivatableEntity
    {
        /// <summary>
        /// FileVersionId - ID version file được comment
        /// </summary>
        [Required]
        public int FileVersionId { get; set; }  = -1;

        /// <summary>
        /// Content - Nội dung comment
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; } = "";

        /// <summary>
        /// LineNumber - Số dòng được comment (nếu có)
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// StartColumn - Cột bắt đầu highlight
        /// </summary>
        public int? StartColumn { get; set; }

        /// <summary>
        /// EndColumn - Cột kết thúc highlight
        /// </summary>
        public int? EndColumn { get; set; }

        /// <summary>
        /// CommentType - Loại comment
        /// General, CodeReview, Bug, Suggestion, Question
        /// </summary>
        [StringLength(50)]
        public string CommentType { get; set; } = "General";

        /// <summary>
        /// ParentCommentId - ID comment cha (nếu là reply)
        /// </summary>
        public int ParentCommentId { get; set; } = -1;

        /// <summary>
        /// IsResolved - Comment đã được resolve chưa
        /// </summary>
        public bool IsResolved { get; set; } = false;

        /// <summary>
        /// ResolvedBy - ID người resolve comment
        /// </summary>
        public int ResolvedBy { get; set; } 

        /// <summary>
        /// ResolvedAt - Thời gian resolve
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(FileVersionId))]
        public virtual FileVersion FileVersion { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ParentCommentId))]
        public virtual FileComment ParentComment { get; set; }

        [JsonIgnore]
        public virtual ICollection<FileComment> Replies { get; set; } = new List<FileComment>();

        /// <summary>
        /// Resolve - Đánh dấu comment đã resolve
        /// </summary>
        public virtual void Resolve(int resolvedBy)
        {
            IsResolved = true;
            ResolvedBy = resolvedBy;
            ResolvedAt = DateTime.UtcNow;
            MarkAsUpdated(resolvedBy);
        }
    }
}
