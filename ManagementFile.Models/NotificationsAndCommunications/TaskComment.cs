using ManagementFile.Contracts.Enums;
using ManagementFile.Models.BaseModels;
using ManagementFile.Models.ProjectManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ManagementFile.Models.NotificationsAndCommunications
{
    /// <summary>
    /// TaskComment - Bình luận trên task
    /// Comments, discussions trên tasks
    /// </summary>
    [Table("TaskComments")]
    [Index(nameof(TaskId), nameof(CreatedAt))]
    [Index(nameof(TaskId), nameof(CommentStatus))]
    [Index(nameof(ReviewerId), nameof(CreatedAt))]
    public class TaskComment : SoftDeletableEntity
    {
        /// <summary>
        /// TaskId - ID task được comment
        /// </summary>
        [Required]
        public int TaskId { get; set; }

        /// <summary>
        /// Content - Nội dung comment
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; } = "";

        /// <summary>
        /// ParentCommentId - ID comment cha (nếu là reply)
        /// </summary>
        public int? ParentCommentId { get; set; }

        #region Comment Type & Category

        /// <summary>
        /// CommentType - Loại comment
        /// ReviewFeedback: Góp ý review
        /// IssueReport: Báo lỗi/vấn đề
        /// ChangeRequest: Yêu cầu thay đổi
        /// StatusUpdate: Cập nhật tiến độ
        /// Clarification: Làm rõ yêu cầu
        /// Approval: Phê duyệt
        /// Rejection: Từ chối
        /// Question: Câu hỏi
        /// Suggestion: Đề xuất
        /// General: Bình luận chung
        /// </summary>
        [Required]
        public CommentType CommentType { get; set; } = CommentType.General;

        #endregion

        #region Review & Feedback Properties

        /// <summary>
        /// CommentStatus - Trạng thái của comment
        /// Sử dụng TaskStatuss để theo dõi comment đã xử lý chưa
        /// Todo: Chưa xử lý
        /// InProgress: Đang xử lý
        /// InReview: Đang review lại
        /// Completed: Đã xử lý xong
        /// Blocked: Bị chặn, không thể xử lý
        /// OnHold: Tạm hoãn
        /// Cancelled: Hủy bỏ
        /// </summary>
        [Required]
        public TaskStatuss CommentStatus { get; set; } = TaskStatuss.Todo;

        /// <summary>
        /// Priority - Độ ưu tiên của comment
        /// Critical: Nghiêm trọng, phải sửa ngay
        /// High: Cao, cần sửa sớm
        /// Medium: Trung bình
        /// Low: Thấp, có thể sửa sau
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Low;

        /// <summary>
        /// ReviewerId - ID người review/checker
        /// Người kiểm tra và đưa ra feedback
        /// </summary>
        public int? ReviewerId { get; set; }

        /// <summary>
        /// AssignedToId - ID người được giao xử lý comment
        /// Người chịu trách nhiệm fix/resolve comment này
        /// </summary>
        public int? AssignedToId { get; set; }

        #endregion

        #region Issue Details

        /// <summary>
        /// IssueTitle - Tiêu đề vấn đề (tóm tắt ngắn gọn)
        /// </summary>
        [StringLength(500)]
        public string IssueTitle { get; set; } = "";

        /// <summary>
        /// SuggestedFix - Đề xuất cách sửa
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string SuggestedFix { get; set; } = "";

        #endregion

        #region Location & Reference

        /// <summary>
        /// RelatedModule - Module/phần liên quan
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string RelatedModule { get; set; } = "";

        /// <summary>
        /// RelatedFiles - JSON array các file liên quan
        /// ["src/auth/login.cs", "views/login.xaml"]
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string RelatedFiles { get; set; } = "[]";

        /// <summary>
        /// RelatedScreenshots - JSON array ảnh chụp màn hình
        /// Đường dẫn hoặc URL các ảnh minh họa
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string RelatedScreenshots { get; set; } = "[]";

        /// <summary>
        /// RelatedDocuments - JSON array tài liệu liên quan
        /// Requirements, design docs, etc.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string RelatedDocuments { get; set; } = "[]";

        #endregion

        #region Attachments & Mentions

        /// <summary>
        /// Attachments - JSON array các file đính kèm
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Attachments { get; set; } = "[]";

        /// <summary>
        /// MentionedUsers - JSON array các user được mention
        /// Ngươời dùng được tag trong comment
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string MentionedUsers { get; set; } = "[]";

        #endregion

        #region Resolution Tracking

        /// <summary>
        /// ResolvedAt - Thời gian đã giải quyết
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// ResolvedBy - ID người giải quyết
        /// </summary>
        public int? ResolvedBy { get; set; }

        /// <summary>
        /// ResolutionNotes - Ghi chú về cách giải quyết
        /// Mô tả đã fix như thế nào
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string ResolutionNotes { get; set; } = "";

        /// <summary>
        /// ResolutionCommitId - ID commit đã fix
        /// Liên kết với version control
        /// </summary>
        [StringLength(100)]
        public string ResolutionCommitId { get; set; } = "";

        /// <summary>
        /// VerifiedAt - Thời gian đã verify
        /// Thời gian người review xác nhận đã fix đúng
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// VerifiedBy - ID người verify
        /// </summary>
        public int? VerifiedBy { get; set; }

        /// <summary>
        /// VerificationNotes - Ghi chú verify
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string VerificationNotes { get; set; } = "";

        /// <summary>
        /// IsVerified - Đã verify chưa
        /// </summary>
        public bool IsVerified { get; set; } = false;

        #endregion

        #region Time Tracking

        /// <summary>
        /// EstimatedFixTime - Thời gian ước tính để fix (giờ)
        /// </summary>
        public decimal EstimatedFixTime { get; set; } = 0;

        /// <summary>
        /// ActualFixTime - Thời gian thực tế đã fix (giờ)
        /// </summary>
        public decimal ActualFixTime { get; set; } = 0;

        /// <summary>
        /// DueDate - Deadline phải fix xong
        /// </summary>
        public DateTime? DueDate { get; set; }

        #endregion

        #region Additional Flags

        /// <summary>
        /// IsBlocking - Comment này có block task không
        /// </summary>
        public bool IsBlocking { get; set; } = false;

        /// <summary>
        /// RequiresDiscussion - Cần thảo luận thêm
        /// </summary>
        public bool RequiresDiscussion { get; set; } = false;

        /// <summary>
        /// IsAgreed - Đã đồng ý với feedback
        /// </summary>
        public bool IsAgreed { get; set; } = false;

        /// <summary>
        /// AgreedBy - ID người đồng ý
        /// </summary>
        public int? AgreedBy { get; set; }

        /// <summary>
        /// AgreedAt - Thời gian đồng ý
        /// </summary>
        public DateTime? AgreedAt { get; set; }

        #endregion

        #region Metadata

        /// <summary>
        /// Tags - JSON array các tag
        /// ["bug", "security", "urgent"]
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Tags { get; set; } = "[]";

        /// <summary>
        /// Metadata - JSON object chứa thông tin bổ sung
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Metadata { get; set; } = "{}";

        #endregion

        /// <summary>
        /// Comment này là hệ thống tự động tạo (true) hay do user tạo (false)
        /// </summary>
        public bool IsSystemComment { get; set; } = false;


        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(TaskId))]
        public virtual ProjectTask ProjectTask { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ParentCommentId))]
        public virtual TaskComment ParentComment { get; set; }

        [JsonIgnore]
        public virtual ICollection<TaskComment> Replies { get; set; } = new List<TaskComment>();

        #region Helper Methods

        /// <summary>
        /// GetMentionedUsers - Lấy danh sách user được mention
        /// </summary>
        public virtual List<string> GetMentionedUsers()
        {
            try
            {
                return string.IsNullOrWhiteSpace(MentionedUsers) ?
                    new List<string>() :
                    JsonSerializer.Deserialize<List<string>>(MentionedUsers) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// SetMentionedUsers - Set danh sách user được mention
        /// </summary>
        public virtual void SetMentionedUsers(List<string> users)
        {
            try
            {
                MentionedUsers = users == null || users.Count == 0 ?
                    "[]" : JsonSerializer.Serialize(users);
            }
            catch
            {
                MentionedUsers = "[]";
            }
        }

        /// <summary>
        /// GetAttachments - Lấy danh sách attachments
        /// </summary>
        public virtual List<string> GetAttachments()
        {
            try
            {
                return string.IsNullOrWhiteSpace(Attachments) ?
                    new List<string>() :
                    JsonSerializer.Deserialize<List<string>>(Attachments) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// SetAttachments - Set danh sách attachments
        /// </summary>
        public virtual void SetAttachments(List<string> attachments)
        {
            try
            {
                Attachments = attachments == null || attachments.Count == 0 ?
                    "[]" : JsonSerializer.Serialize(attachments);
            }
            catch
            {
                Attachments = "[]";
            }
        }

        /// <summary>
        /// GetRelatedFiles - Lấy danh sách file liên quan
        /// </summary>
        public virtual List<string> GetRelatedFiles()
        {
            try
            {
                return string.IsNullOrWhiteSpace(RelatedFiles) ?
                    new List<string>() :
                    JsonSerializer.Deserialize<List<string>>(RelatedFiles) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// SetRelatedFiles - Set danh sách file liên quan
        /// </summary>
        public virtual void SetRelatedFiles(List<string> files)
        {
            try
            {
                RelatedFiles = files == null || files.Count == 0 ?
                    "[]" : JsonSerializer.Serialize(files);
            }
            catch
            {
                RelatedFiles = "[]";
            }
        }

        /// <summary>
        /// GetRelatedScreenshots - Lấy danh sách screenshots
        /// </summary>
        public virtual List<string> GetRelatedScreenshots()
        {
            try
            {
                return string.IsNullOrWhiteSpace(RelatedScreenshots) ?
                    new List<string>() :
                    JsonSerializer.Deserialize<List<string>>(RelatedScreenshots) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// SetRelatedScreenshots - Set danh sách screenshots
        /// </summary>
        public virtual void SetRelatedScreenshots(List<string> screenshots)
        {
            try
            {
                RelatedScreenshots = screenshots == null || screenshots.Count == 0 ?
                    "[]" : JsonSerializer.Serialize(screenshots);
            }
            catch
            {
                RelatedScreenshots = "[]";
            }
        }

        /// <summary>
        /// SetRelatedDocuments - Set danh sách tài liệu liên quan
        /// </summary>
        /// <param name="documents">Danh sách đường dẫn hoặc URL tài liệu</param>
        public virtual void SetRelatedDocuments(List<string> documents)
        {
            try
            {
                RelatedDocuments = documents == null || documents.Count == 0 ?
                    "[]" : JsonSerializer.Serialize(documents);
            }
            catch
            {
                RelatedDocuments = "[]";
            }
        }

        /// <summary>
        /// GetRelatedDocuments - Lấy danh sách tài liệu liên quan
        /// </summary>
        /// <returns>Danh sách đường dẫn hoặc URL tài liệu</returns>
        public virtual List<string> GetRelatedDocuments()
        {
            try
            {
                return string.IsNullOrWhiteSpace(RelatedDocuments) ?
                    new List<string>() :
                    JsonSerializer.Deserialize<List<string>>(RelatedDocuments) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// GetTags - Lấy danh sách tags
        /// </summary>
        public virtual List<string> GetTags()
        {
            try
            {
                return string.IsNullOrWhiteSpace(Tags) ?
                    new List<string>() :
                    JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// SetTags - Set danh sách tags
        /// </summary>
        public virtual void SetTags(List<string> tags)
        {
            try
            {
                Tags = tags == null || tags.Count == 0 ?
                    "[]" : JsonSerializer.Serialize(tags);
            }
            catch
            {
                Tags = "[]";
            }
        }

        /// <summary>
        /// MarkAsResolved - Đánh dấu đã giải quyết
        /// </summary>
        public virtual void MarkAsResolved(int resolvedBy, string resolutionNotes = "", string commitId = "")
        {
            CommentStatus = TaskStatuss.InReview;
            ResolvedAt = DateTime.UtcNow;
            ResolvedBy = resolvedBy;
            ResolutionNotes = resolutionNotes;
            ResolutionCommitId = commitId;
            MarkAsUpdated(resolvedBy);
        }

        /// <summary>
        /// MarkAsVerified - Đánh dấu đã verify
        /// </summary>
        public virtual void MarkAsVerified(int verifiedBy, string verificationNotes = "")
        {
            IsVerified = true;
            VerifiedAt = DateTime.UtcNow;
            VerifiedBy = verifiedBy;
            VerificationNotes = verificationNotes;
            MarkAsUpdated(verifiedBy);
        }

        /// <summary>
        /// AssignTo - Giao cho người xử lý
        /// </summary>
        public virtual void AssignTo(int userId)
        {
            AssignedToId = userId;
            if (CommentStatus == TaskStatuss.Todo)
            {
                CommentStatus = TaskStatuss.InProgress;
            }
        }

        /// <summary>
        /// ReviewBy - Đặt người review/checker
        /// </summary>
        /// <param name="reviewerId"></param>
        public virtual void ReviewBy(int reviewerId)
        {
            ReviewerId = reviewerId;
            if (CommentStatus == TaskStatuss.InProgress)
            {
                CommentStatus = TaskStatuss.InReview;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public virtual void Prioritize(TaskPriority priority)
        {
            Priority = priority;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        public virtual void UpdateStatus(TaskStatuss status)
        {
            CommentStatus = status;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public virtual void UpdateType(CommentType type)
        {
            CommentType = type;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="isBlocking"></param>
        public virtual void IsBlockingFlag(bool isBlocking)
        {
            IsBlocking = isBlocking;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requiresDiscussion"></param>
        public virtual void RequiresDiscussionFlag(bool requiresDiscussion)
        {
            RequiresDiscussion = requiresDiscussion;
        }

        /// <summary>
        /// IsResolved - Kiểm tra đã giải quyết chưa
        /// </summary>
        public virtual bool IsResolved()
        {
            return CommentStatus == TaskStatuss.Completed && ResolvedAt.HasValue;
        }

        /// <summary>
        /// IsOverdue - Kiểm tra có quá hạn không
        /// </summary>
        public virtual bool IsOverdue()
        {
            return DueDate.HasValue &&
                   DateTime.UtcNow > DueDate.Value &&
                   !IsResolved();
        }

        #region Additional Helper Methods

        /// <summary>
        /// GetMetadata - Lấy metadata dưới dạng Dictionary
        /// </summary>
        /// <returns>Dictionary chứa metadata</returns>
        public virtual Dictionary<string, object> GetMetadata()
        {
            try
            {
                return string.IsNullOrWhiteSpace(Metadata) ?
                    new Dictionary<string, object>() :
                    JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata) ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// SetMetadata - Set metadata từ Dictionary
        /// </summary>
        /// <param name="metadata">Dictionary chứa metadata</param>
        public virtual void SetMetadata(Dictionary<string, object> metadata)
        {
            try
            {
                Metadata = metadata == null || metadata.Count == 0 ?
                    "{}" : JsonSerializer.Serialize(metadata);
            }
            catch
            {
                Metadata = "{}";
            }
        }

        /// <summary>
        /// AddMetadata - Thêm một key-value vào metadata
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public virtual void AddMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            var currentMetadata = GetMetadata();
            currentMetadata[key] = value;
            SetMetadata(currentMetadata);
        }

        /// <summary>
        /// RemoveMetadata - Xóa một key khỏi metadata
        /// </summary>
        /// <param name="key">Key cần xóa</param>
        public virtual void RemoveMetadata(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            var currentMetadata = GetMetadata();
            if (currentMetadata.ContainsKey(key))
            {
                currentMetadata.Remove(key);
                SetMetadata(currentMetadata);
            }
        }

        /// <summary>
        /// HasMetadata - Kiểm tra có metadata key không
        /// </summary>
        /// <param name="key">Key cần kiểm tra</param>
        /// <returns>True nếu có key</returns>
        public virtual bool HasMetadata(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            var currentMetadata = GetMetadata();
            return currentMetadata.ContainsKey(key);
        }

        /// <summary>
        /// AddTag - Thêm một tag vào danh sách
        /// </summary>
        /// <param name="tag">Tag cần thêm</param>
        public virtual void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            var currentTags = GetTags();
            if (!currentTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                currentTags.Add(tag.Trim());
                SetTags(currentTags);
            }
        }

        /// <summary>
        /// RemoveTag - Xóa một tag khỏi danh sách
        /// </summary>
        /// <param name="tag">Tag cần xóa</param>
        public virtual void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            var currentTags = GetTags();
            var tagToRemove = currentTags.FirstOrDefault(t =>
                string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));

            if (tagToRemove != null)
            {
                currentTags.Remove(tagToRemove);
                SetTags(currentTags);
            }
        }

        /// <summary>
        /// HasTag - Kiểm tra có tag không
        /// </summary>
        /// <param name="tag">Tag cần kiểm tra</param>
        /// <returns>True nếu có tag</returns>
        public virtual bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;

            var currentTags = GetTags();
            return currentTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// AddMentionedUser - Thêm một user vào mention list
        /// </summary>
        /// <param name="username">Username cần mention</param>
        public virtual void AddMentionedUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;

            var currentUsers = GetMentionedUsers();
            if (!currentUsers.Contains(username, StringComparer.OrdinalIgnoreCase))
            {
                currentUsers.Add(username.Trim());
                SetMentionedUsers(currentUsers);
            }
        }

        /// <summary>
        /// RemoveMentionedUser - Xóa một user khỏi mention list
        /// </summary>
        /// <param name="username">Username cần xóa</param>
        public virtual void RemoveMentionedUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;

            var currentUsers = GetMentionedUsers();
            var userToRemove = currentUsers.FirstOrDefault(u =>
                string.Equals(u, username, StringComparison.OrdinalIgnoreCase));

            if (userToRemove != null)
            {
                currentUsers.Remove(userToRemove);
                SetMentionedUsers(currentUsers);
            }
        }

        /// <summary>
        /// AddAttachment - Thêm một file đính kèm
        /// </summary>
        /// <param name="filePath">Đường dẫn file</param>
        public virtual void AddAttachment(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var currentAttachments = GetAttachments();
            if (!currentAttachments.Contains(filePath))
            {
                currentAttachments.Add(filePath.Trim());
                SetAttachments(currentAttachments);
            }
        }

        /// <summary>
        /// RemoveAttachment - Xóa một file đính kèm
        /// </summary>
        /// <param name="filePath">Đường dẫn file cần xóa</param>
        public virtual void RemoveAttachment(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var currentAttachments = GetAttachments();
            if (currentAttachments.Contains(filePath))
            {
                currentAttachments.Remove(filePath);
                SetAttachments(currentAttachments);
            }
        }

        /// <summary>
        /// AddRelatedFile - Thêm một file liên quan
        /// </summary>
        /// <param name="filePath">Đường dẫn file</param>
        public virtual void AddRelatedFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var currentFiles = GetRelatedFiles();
            if (!currentFiles.Contains(filePath))
            {
                currentFiles.Add(filePath.Trim());
                SetRelatedFiles(currentFiles);
            }
        }

        /// <summary>
        /// RemoveRelatedFile - Xóa một file liên quan
        /// </summary>
        /// <param name="filePath">Đường dẫn file cần xóa</param>
        public virtual void RemoveRelatedFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var currentFiles = GetRelatedFiles();
            if (currentFiles.Contains(filePath))
            {
                currentFiles.Remove(filePath);
                SetRelatedFiles(currentFiles);
            }
        }

        /// <summary>
        /// AddRelatedDocument - Thêm một document liên quan
        /// </summary>
        /// <param name="documentPath">Đường dẫn document</param>
        public virtual void AddRelatedDocument(string documentPath)
        {
            if (string.IsNullOrWhiteSpace(documentPath)) return;

            var currentDocs = GetRelatedDocuments();
            if (!currentDocs.Contains(documentPath))
            {
                currentDocs.Add(documentPath.Trim());
                SetRelatedDocuments(currentDocs);
            }
        }

        /// <summary>
        /// RemoveRelatedDocument - Xóa một document liên quan
        /// </summary>
        /// <param name="documentPath">Đường dẫn document cần xóa</param>
        public virtual void RemoveRelatedDocument(string documentPath)
        {
            if (string.IsNullOrWhiteSpace(documentPath)) return;

            var currentDocs = GetRelatedDocuments();
            if (currentDocs.Contains(documentPath))
            {
                currentDocs.Remove(documentPath);
                SetRelatedDocuments(currentDocs);
            }
        }

        /// <summary>
        /// AddRelatedScreenshot - Thêm một screenshot liên quan
        /// </summary>
        /// <param name="screenshotPath">Đường dẫn screenshot</param>
        public virtual void AddRelatedScreenshot(string screenshotPath)
        {
            if (string.IsNullOrWhiteSpace(screenshotPath)) return;

            var currentScreenshots = GetRelatedScreenshots();
            if (!currentScreenshots.Contains(screenshotPath))
            {
                currentScreenshots.Add(screenshotPath.Trim());
                SetRelatedScreenshots(currentScreenshots);
            }
        }

        /// <summary>
        /// RemoveRelatedScreenshot - Xóa một screenshot liên quan
        /// </summary>
        /// <param name="screenshotPath">Đường dẫn screenshot cần xóa</param>
        public virtual void RemoveRelatedScreenshot(string screenshotPath)
        {
            if (string.IsNullOrWhiteSpace(screenshotPath)) return;

            var currentScreenshots = GetRelatedScreenshots();
            if (currentScreenshots.Contains(screenshotPath))
            {
                currentScreenshots.Remove(screenshotPath);
                SetRelatedScreenshots(currentScreenshots);
            }
        }

        /// <summary>
        /// ClearAllLists - Xóa tất cả các lists (attachments, files, documents, etc.)
        /// </summary>
        public virtual void ClearAllLists()
        {
            SetAttachments(new List<string>());
            SetRelatedFiles(new List<string>());
            SetRelatedDocuments(new List<string>());
            SetRelatedScreenshots(new List<string>());
            SetMentionedUsers(new List<string>());
            SetTags(new List<string>());
            SetMetadata(new Dictionary<string, object>());
        }

        /// <summary>
        /// GetAllRelatedItems - Lấy tất cả items liên quan trong một object
        /// </summary>
        /// <returns>Object chứa tất cả related items</returns>
        public virtual object GetAllRelatedItems()
        {
            return new
            {
                Attachments = GetAttachments(),
                RelatedFiles = GetRelatedFiles(),
                RelatedDocuments = GetRelatedDocuments(),
                RelatedScreenshots = GetRelatedScreenshots(),
                MentionedUsers = GetMentionedUsers(),
                Tags = GetTags(),
                Metadata = GetMetadata()
            };
        }

        /// <summary>
        /// HasAnyRelatedItems - Kiểm tra có items liên quan không
        /// </summary>
        /// <returns>True nếu có ít nhất 1 item</returns>
        public virtual bool HasAnyRelatedItems()
        {
            return GetAttachments().Count > 0 ||
                   GetRelatedFiles().Count > 0 ||
                   GetRelatedDocuments().Count > 0 ||
                   GetRelatedScreenshots().Count > 0 ||
                   GetMentionedUsers().Count > 0 ||
                   GetTags().Count > 0 ||
                   GetMetadata().Count > 0;
        }

        /// <summary>
        /// GetSummaryText - Lấy text tóm tắt comment
        /// </summary>
        /// <param name="maxLength">Độ dài tối đa</param>
        /// <returns>Text tóm tắt</returns>
        public virtual string GetSummaryText(int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(Content))
                return "";

            if (Content.Length <= maxLength)
                return Content;

            return Content.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// Clone - Tạo bản copy của comment (không copy ID và timestamps)
        /// </summary>
        /// <returns>Bản copy của comment</returns>
        public virtual TaskComment Clone()
        {
            return new TaskComment
            {
                TaskId = this.TaskId,
                Content = this.Content,
                CommentType = this.CommentType,
                Priority = this.Priority,
                IssueTitle = this.IssueTitle,
                SuggestedFix = this.SuggestedFix,
                RelatedModule = this.RelatedModule,
                RelatedFiles = this.RelatedFiles,
                RelatedScreenshots = this.RelatedScreenshots,
                RelatedDocuments = this.RelatedDocuments,
                Attachments = this.Attachments,
                MentionedUsers = this.MentionedUsers,
                Tags = this.Tags,
                Metadata = this.Metadata,
                EstimatedFixTime = this.EstimatedFixTime,
                DueDate = this.DueDate,
                IsBlocking = this.IsBlocking,
                RequiresDiscussion = this.RequiresDiscussion
            };
        }

        /// <summary>
        /// UpdateFrom - Cập nhật các field từ comment khác
        /// </summary>
        /// <param name="other">Comment source để copy</param>
        /// <param name="updatedBy">ID người update</param>
        public virtual void UpdateFrom(TaskComment other, int updatedBy)
        {
            if (other == null) return;

            Content = other.Content;
            CommentType = other.CommentType;
            Priority = other.Priority;
            IssueTitle = other.IssueTitle;
            SuggestedFix = other.SuggestedFix;
            RelatedModule = other.RelatedModule;
            RelatedFiles = other.RelatedFiles;
            RelatedScreenshots = other.RelatedScreenshots;
            RelatedDocuments = other.RelatedDocuments;
            Attachments = other.Attachments;
            MentionedUsers = other.MentionedUsers;
            Tags = other.Tags;
            Metadata = other.Metadata;
            EstimatedFixTime = other.EstimatedFixTime;
            DueDate = other.DueDate;
            IsBlocking = other.IsBlocking;
            RequiresDiscussion = other.RequiresDiscussion;

            MarkAsUpdated(updatedBy);
        }

        /// <summary>
        /// GetWorkflowStatus - Lấy trạng thái workflow tổng hợp
        /// </summary>
        /// <returns>Object chứa workflow status</returns>
        public virtual object GetWorkflowStatus()
        {
            return new
            {
                Status = CommentStatus.ToString(),
                IsResolved = IsResolved(),
                IsVerified = IsVerified,
                IsAgreed = IsAgreed,
                IsOverdue = IsOverdue(),
                IsBlocking = IsBlocking,
                RequiresDiscussion = RequiresDiscussion,
                HasAssignee = AssignedToId.HasValue,
                HasReviewer = ReviewerId.HasValue,
                DaysToDeadline = DueDate.HasValue ? (DueDate.Value - DateTime.UtcNow).Days : (int?)null
            };
        }

        #endregion

        #endregion
    }
}
