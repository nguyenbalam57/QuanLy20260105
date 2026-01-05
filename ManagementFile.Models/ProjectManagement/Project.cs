using ManagementFile.Models.BaseModels;
using ManagementFile.Contracts.Enums;
using ManagementFile.Models.UserManagement;
using ManagementFile.Models.FileManagement;
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

namespace ManagementFile.Models.ProjectManagement
{
    /// <summary>
    /// Project - Dự án
    /// Quản lý thông tin dự án với tất cả các tính năng tracking và metadata
    /// </summary>
    [Table("Projects")]
    [Index(nameof(ProjectCode), IsUnique = true)]
    [Index(nameof(Status), nameof(IsActive))]
    [Index(nameof(ClientId), nameof(IsActive))]
    public class Project : SoftDeletableEntity, IHasMetadata, IHasTags
    {
        /// <summary>
        /// Bổ sung thêm project cha
        /// </summary>
        public int? ProjectParentId { get; set; }
        
        /// <summary>
        /// ProjectCode - Mã dự án duy nhất
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ProjectCode { get; set; } = "";

        /// <summary>
        /// ProjectName - Tên dự án
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ProjectName { get; set; } = "";

        /// <summary>
        /// Description - Mô tả dự án
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Description { get; set; } = "";

        /// <summary>
        /// Status - Trạng thái dự án
        /// </summary>
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

        /// <summary>
        /// Priority - Độ ưu tiên dự án
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Low;

        /// <summary>
        /// ClientId - ID khách hàng (nếu có)
        /// </summary>
        public int? ClientId { get; set; } 

        /// <summary>
        /// ClientName - Tên khách hàng
        /// </summary>
        [StringLength(200)]
        public string ClientName { get; set; } = "";

        /// <summary>
        /// ProjectManagerId - ID quản lý dự án
        /// </summary>
        public int? ProjectManagerId { get; set; }

        /// <summary>
        /// StartDate - Ngày bắt đầu dự án
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// PlannedEndDate - Ngày kết thúc dự kiến
        /// </summary>
        public DateTime? PlannedEndDate { get; set; }

        /// <summary>
        /// ActualEndDate - Ngày kết thúc thực tế
        /// </summary>
        public DateTime? ActualEndDate { get; set; }

        /// <summary>
        /// EstimatedBudget - Ngân sách ước tính
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedBudget { get; set; } = 0;

        /// <summary>
        /// ActualBudget - Ngân sách thực tế đã sử dụng
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualBudget { get; set; } = 0;

        /// <summary>
        /// EstimatedHours - Số giờ ước tính
        /// </summary>
        public int EstimatedHours { get; set; } = 0;

        /// <summary>
        /// ActualHours - Số giờ thực tế đã làm
        /// </summary>
        public int ActualHours { get; set; } = 0;

        /// <summary>
        /// CompletionPercentage - Phần trăm hoàn thành (0-100)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal CompletionPercentage { get; set; } = 0;

        /// <summary>
        /// IsActive - Dự án có đang hoạt động không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// IsPublic - Dự án có public không (cho external clients)
        /// </summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// Metadata - Thông tin metadata bổ sung (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Metadata { get; set; } = "{}";

        /// <summary>
        /// Tags - Các tags của dự án (JSON array)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Tags { get; set; } = "";

        /// <summary>
        /// CustomProperties - Các thuộc tính tùy chỉnh (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string CustomProperties { get; set; } = "";

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(ProjectParentId))]
        public virtual Project ProjectParent { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

        [JsonIgnore]
        public virtual ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();

        [JsonIgnore]
        public virtual ICollection<ProjectFolder> ProjectFolders { get; set; } = new List<ProjectFolder>();

        [JsonIgnore]
        public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

        /// <summary>
        /// Computed Properties
        /// </summary>
        [NotMapped]
        public bool IsOverdue => PlannedEndDate.HasValue && DateTime.UtcNow > PlannedEndDate.Value && Status != ProjectStatus.Completed;

        [NotMapped]
        public bool IsCompleted => Status == ProjectStatus.Completed;

        [NotMapped]
        public TimeSpan? RemainingTime => PlannedEndDate.HasValue ? PlannedEndDate.Value - DateTime.UtcNow : null;

        [NotMapped]
        public decimal BudgetVariance => ActualBudget - EstimatedBudget;

        [NotMapped]
        public int HourVariance => ActualHours - EstimatedHours;

        #region IHasMetadata Implementation

        /// <summary>
        /// GetMetadata - Deserialize metadata thành object
        /// </summary>
        public T GetMetadata<T>() where T : class, new()
        {
            if (string.IsNullOrEmpty(Metadata))
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

        /// <summary>
        /// SetMetadata - Serialize object thành metadata JSON
        /// </summary>
        public void SetMetadata<T>(T data) where T : class
        {
            if (data == null)
            {
                Metadata = "";
                return;
            }

            try
            {
                Metadata = JsonSerializer.Serialize(data);
            }
            catch
            {
                Metadata = "";
            }
        }

        #endregion

        #region IHasTags Implementation

        /// <summary>
        /// GetTags - Lấy danh sách tags
        /// </summary>
        public List<string> GetTags()
        {
            if (string.IsNullOrEmpty(Tags))
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

        /// <summary>
        /// SetTags - Set danh sách tags
        /// </summary>
        public void SetTags(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                Tags = "";
                return;
            }

            try
            {
                // Remove duplicates and empty tags
                var cleanTags = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                                   .Select(t => t.Trim())
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .ToList();
                
                Tags = JsonSerializer.Serialize(cleanTags);
            }
            catch
            {
                Tags = "";
            }
        }

        /// <summary>
        /// AddTag - Thêm tag mới
        /// </summary>
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

        /// <summary>
        /// RemoveTag - Xóa tag
        /// </summary>
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

        /// <summary>
        /// HasTag - Kiểm tra có tag không
        /// </summary>
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

        /// <summary>
        /// StartProject - Bắt đầu dự án
        /// </summary>
        public virtual void StartProject(int startedBy)
        {
            if (Status != ProjectStatus.Planning)
                throw new InvalidOperationException("Chỉ có thể start project ở trạng thái Planning");

            Status = ProjectStatus.Active;
            StartDate = DateTime.UtcNow;
            IsActive = true;
            MarkAsUpdated(startedBy);
        }

        /// <summary>
        /// CompleteProject - Hoàn thành dự án
        /// </summary>
        public virtual void CompleteProject(int completedBy)
        {
            if (Status != ProjectStatus.Active)
                throw new InvalidOperationException("Chỉ có thể complete project ở trạng thái Active");

            Status = ProjectStatus.Completed;
            ActualEndDate = DateTime.UtcNow;
            CompletionPercentage = 100;
            MarkAsUpdated(completedBy);
        }

        /// <summary>
        /// PauseProject - Tạm dừng dự án
        /// </summary>
        public virtual void PauseProject(int pausedBy, string reason = "")
        {
            if (Status != ProjectStatus.Active)
                throw new InvalidOperationException("Chỉ có thể pause project ở trạng thái Active");

            Status = ProjectStatus.OnHold;
            // Add pause reason to custom properties or metadata
            MarkAsUpdated(pausedBy);
        }

        /// <summary>
        /// ResumeProject - Tiếp tục dự án
        /// </summary>
        public virtual void ResumeProject(int resumedBy)
        {
            if (Status != ProjectStatus.OnHold)
                throw new InvalidOperationException("Chỉ có thể resume project ở trạng thái OnHold");

            Status = ProjectStatus.Active;
            MarkAsUpdated(resumedBy);
        }

        /// <summary>
        /// CancelProject - Hủy dự án
        /// </summary>
        public virtual void CancelProject(int cancelledBy, string reason = "")
        {
            if (Status == ProjectStatus.Completed || Status == ProjectStatus.Cancelled)
                throw new InvalidOperationException("Không thể cancel project đã completed hoặc cancelled");

            Status = ProjectStatus.Cancelled;
            IsActive = false;
            // Add cancel reason to custom properties or metadata
            MarkAsUpdated(cancelledBy);
        }

        /// <summary>
        /// ArchiveProject - Lưu trữ dự án
        /// </summary>
        public virtual void ArchiveProject(int archivedBy)
        {
            if (Status != ProjectStatus.Completed && Status != ProjectStatus.Cancelled)
                throw new InvalidOperationException("Chỉ có thể archive project đã completed hoặc cancelled");

            Status = ProjectStatus.Archived;
            IsActive = false;
            MarkAsUpdated(archivedBy);
        }

        /// <summary>
        /// UpdateProgress - Cập nhật tiến độ dự án
        /// </summary>
        public virtual void UpdateProgress(decimal newPercentage, int updatedBy)
        {
            if (newPercentage < 0 || newPercentage > 100)
                throw new ArgumentException("Percentage phải nằm trong khoảng 0-100");

            CompletionPercentage = newPercentage;
            
            // Tự động complete nếu đạt 100%
            if (newPercentage == 100 && Status == ProjectStatus.Active)
            {
                CompleteProject(updatedBy);
            }
            else
            {
                MarkAsUpdated(updatedBy);
            }
        }

        /// <summary>
        /// GetActiveMembers - Lấy danh sách thành viên đang active
        /// </summary>
        public virtual IEnumerable<ProjectMember> GetActiveMembers()
        {
            return ProjectMembers?.Where(pm => pm.IsActive) ?? Enumerable.Empty<ProjectMember>();
        }

        /// <summary>
        /// GetProjectManager - Lấy thông tin project manager
        /// </summary>
        public virtual ProjectMember GetProjectManager()
        {
            return ProjectMembers?.FirstOrDefault(pm => pm.IsActive && pm.ProjectRole == UserRole.Manager);
        }

        /// <summary>
        /// GetTotalFiles - Lấy tổng số file trong project
        /// </summary>
        public virtual int GetTotalFiles()
        {
            return ProjectFiles?.Count(pf => pf.IsActive && !pf.IsDeleted) ?? 0;
        }

        /// <summary>
        /// GetTotalFileSize - Lấy tổng kích thước file trong project
        /// </summary>
        public virtual long GetTotalFileSize()
        {
            return ProjectFiles?.Where(pf => pf.IsActive && !pf.IsDeleted).Sum(pf => pf.CurrentFileSize) ?? 0;
        }
    }
}