using ManagementFile.Contracts.Enums;
using ManagementFile.Models.BaseModels;
using ManagementFile.Models.NotificationsAndCommunications;
using ManagementFile.Models.TimeTracking;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManagementFile.Models.ProjectManagement
{
    /// <summary>
    /// ProjectTask - Nhiệm vụ trong dự án
    /// Quản lý tasks/work items với workflow và tracking chi tiết
    /// </summary>
    [Table("ProjectTasks")]
    [Index(nameof(ProjectId), nameof(Status))]
    [Index(nameof(AssignedToId), nameof(Status))]
    [Index(nameof(Priority), nameof(DueDate))]
    [Index(nameof(ParentTaskId), nameof(IsActive))]
    public class ProjectTask : SoftDeletableEntity, IHasMetadata, IHasTags
    {
        /// <summary>
        /// ProjectId - ID dự án chứa task này
        /// </summary>
        [Required]
        public int ProjectId { get; set; }

        /// <summary>
        /// ParentTaskId - ID task cha (nếu là subtask)
        /// </summary>
        public int? ParentTaskId { get; set; }

        /// <summary>
        /// TaskCode - Mã nhiệm vụ (unique trong project)
        /// </summary>
        [StringLength(50)]
        public string TaskCode { get; set; } = "";

        /// <summary>
        /// Title - Tiêu đề nhiệm vụ
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        /// <summary>
        /// Description - Mô tả chi tiết nhiệm vụ
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Description { get; set; } = "";

        /// <summary>
        /// Status - Trạng thái nhiệm vụ
        /// </summary>
        public TaskStatuss Status { get; set; } = TaskStatuss.Todo;

        /// <summary>
        /// Priority - Độ ưu tiên
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Low;

        /// <summary>
        /// AssignedToId - ID người được giao nhiệm vụ
        /// Sử dụng khi ParentTaskId != null để chỉ định người chịu trách nhiệm chính
        /// </summary>
        public int? AssignedToId { get; set; }

        /// <summary>
        /// Danh sách ID người được giao nhiệm vụ
        /// Sử dụng đầu cấp để hỗ trợ nhiều người được giao nhiệm vụ
        /// Json array lưu trữ danh sách ID người dùng
        /// </summary>
        public string AssignedToIds { get; set; } = "[]";

        /// <summary>
        /// ReporterId - ID người tạo/report task
        /// </summary>
        public int? ReporterId { get; set; } 

        /// <summary>
        /// EstimatedHours - Số giờ ước tính
        /// </summary>
        public int EstimatedHours { get; set; } = 0;

        /// <summary>
        /// ActualHours - Số giờ thực tế đã làm
        /// </summary>
        public int ActualHours { get; set; } = 0;

        /// <summary>
        /// Progress - Tiến độ hoàn thành (0-100%)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal Progress { get; set; } = 0;

        /// <summary>
        /// StartDate - Ngày bắt đầu dự kiến
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// DueDate - Ngày hoàn thành dự kiến
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// CompletedAt - Thời gian hoàn thành thực tế
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// CompletedBy - ID người đánh dấu hoàn thành
        /// </summary>
        public int? CompletedBy { get; set; }

        /// <summary>
        /// IsBlocked - Task có bị block không
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// BlockReason - Lý do bị block
        /// </summary>
        [StringLength(500)]
        public string BlockReason { get; set; } = "";

        /// <summary>
        /// IsActive - Task có đang active không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// TaskType - Loại task
        /// </summary>
        public Department TaskType { get; set; } = Department.OTHER;

        /// <summary>
        /// Metadata - Thông tin metadata bổ sung (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Metadata { get; set; } = "{}";

        /// <summary>
        /// Tags - Các tags của task (JSON array)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Tags { get; set; } = "";

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(ProjectId))]
        public virtual Project Project { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ParentTaskId))]
        public virtual ProjectTask ParentTask { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProjectTask> SubTasks { get; set; } = new List<ProjectTask>();

        [JsonIgnore]
        public virtual ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();

        [JsonIgnore]
        public virtual ICollection<TaskTimeLog> TaskTimeLogs { get; set; } = new List<TaskTimeLog>();

        /// <summary>
        /// Computed Properties
        /// </summary>
        [NotMapped]
        public bool IsOverdue => DueDate.HasValue && DateTime.UtcNow > DueDate.Value && Status != TaskStatuss.Completed;

        [NotMapped]
        public bool IsCompleted => Status == TaskStatuss.Completed;

        [NotMapped]
        public bool IsSubTask => ParentTaskId >= 0;

        [NotMapped]
        public int TotalSubTasks => SubTasks?.Count ?? 0;

        [NotMapped]
        public int CompletedSubTasks => SubTasks?.Count(st => st.IsCompleted) ?? 0;

        [NotMapped]
        public decimal SubTasksProgress => TotalSubTasks == 0 ? 0 :
                                           (CompletedSubTasks * 100.0m / TotalSubTasks);


        #region IHasMetadata Implementation

        public T GetMetadata<T>() where T : class, new()
        {
            if (string.IsNullOrEmpty(Metadata) || Metadata == "{}")
                return new T();

            try
            {
                return JsonSerializer.Deserialize<T>(Metadata) ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        public void SetMetadata<T>(T data) where T : class
        {
            if (data == null)
            {
                Metadata = "{}";
                return;
            }

            try
            {
                Metadata = JsonSerializer.Serialize(data);
            }
            catch
            {
                Metadata = "{}";
            }
        }

        #endregion

        #region IHasTags Implementation

        public List<string> GetTags()
        {
            if (string.IsNullOrEmpty(Tags) || Tags == "[]")
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public void SetTags(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                Tags = "[]";
                return;
            }

            try
            {
                var cleanTags = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                                   .Select(t => t.Trim())
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .ToList();
                
                Tags = JsonSerializer.Serialize(cleanTags);
            }
            catch
            {
                Tags = "[]";
            }
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            var currentTags = GetTags();
            var cleanTag = tag.Trim();

            if (!currentTags.Contains(cleanTag, StringComparer.OrdinalIgnoreCase))
            {
                currentTags.Add(cleanTag);
                SetTags(currentTags);
            }
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            var currentTags = GetTags();
            var cleanTag = tag.Trim();

            if (currentTags.RemoveAll(t => string.Equals(t, cleanTag, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                SetTags(currentTags);
            }
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            var currentTags = GetTags();
            var cleanTag = tag.Trim();

            return currentTags.Any(t => string.Equals(t, cleanTag, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        /// <summary>
        /// Business Methods
        /// </summary>

        public virtual void CompleteTask(int completedBy)
        {
            if (Status == TaskStatuss.Completed)
                throw new InvalidOperationException("Task đã được hoàn thành rồi");

            if (IsBlocked)
                throw new InvalidOperationException("Không thể hoàn thành task đang bị block");

            Status = TaskStatuss.Completed;
            Progress = 100;
            CompletedAt = DateTime.UtcNow;
            CompletedBy = completedBy;
            MarkAsUpdated(completedBy);
        }

        public virtual void StartTask(int startedBy)
        {
            if (Status != TaskStatuss.Todo)
                throw new InvalidOperationException("Chỉ có thể start task ở trạng thái Todo");

            if (IsBlocked)
                throw new InvalidOperationException("Không thể start task đang bị block");

            Status = TaskStatuss.InProgress;
            StartDate = DateTime.UtcNow;
            Progress = 0;
            MarkAsUpdated(startedBy);
        }

        public virtual void UpdateProgress(decimal newProgress, int updatedBy)
        {
            if (newProgress < 0 || newProgress > 100)
                throw new ArgumentException("Progress phải nằm trong khoảng 0-100");

            Progress = newProgress;

            if (newProgress == 100 && Status != TaskStatuss.Completed)
            {
                CompleteTask(updatedBy);
            }
            else if (newProgress > 0 && Status == TaskStatuss.Todo)
            {
                Status = TaskStatuss.InProgress;
                StartDate = DateTime.UtcNow;
            }

            MarkAsUpdated(updatedBy);
        }

        public virtual void BlockTask(string reason, int blockedBy)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("BlockReason không được để trống");

            if (Status == TaskStatuss.Completed)
                throw new InvalidOperationException("Không thể block task đã hoàn thành");

            IsBlocked = true;
            BlockReason = reason;
            MarkAsUpdated(blockedBy);
        }

        public virtual void UnblockTask(int unblockedBy)
        {
            if (!IsBlocked)
                throw new InvalidOperationException("Task không bị block");

            IsBlocked = false;
            BlockReason = "";
            MarkAsUpdated(unblockedBy);
        }

        public virtual void AssignTo(int userId, int assignedBy)
        {
            AssignedToId = userId;
            MarkAsUpdated(assignedBy);
        }

        public virtual void Unassign(int unassignedBy)
        {
            AssignedToId = null;
            MarkAsUpdated(unassignedBy);
        }

        #region Helper Methods

        /// <summary>
        /// Get danh sách ID người được giao nhiệm vụ
        /// </summary>
        public virtual List<int> GetAssignedToIds()
        {
            if (string.IsNullOrEmpty(AssignedToIds) || AssignedToIds == "[]")
                return new List<int>();
            try
            {
                return JsonSerializer.Deserialize<List<int>>(AssignedToIds) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }


        /// <summary>
        /// Set danh sásch ID người được giao nhiệm vụ
        /// </summary>
        /// <param name="userIds"></param>
        public virtual void SetAssignedToIds(List<int> userIds)
        {
            if (userIds == null || userIds.Count == 0)
            {
                AssignedToIds = "[]";
                return;
            }
            try
            {
                var distinctIds = userIds.Distinct().ToList();
                AssignedToIds = JsonSerializer.Serialize(distinctIds);
            }
            catch
            {
                AssignedToIds = "[]";
            }
        }

        #endregion

        #region Validation Methods

        public virtual bool CanBeCompleted()
        {
            return Status != TaskStatuss.Completed && 
                   !IsBlocked && 
                   (TotalSubTasks == 0 || CompletedSubTasks == TotalSubTasks);
        }

        public virtual bool CanBeStarted()
        {
            return Status == TaskStatuss.Todo && !IsBlocked;
        }

        public virtual bool CanBeDeleted()
        {
            return Status != TaskStatuss.Completed && 
                   TotalSubTasks == 0;
        }

        #endregion
    }
}