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

namespace ManagementFile.Models.FileManagement
{
    /// <summary>
    /// ProjectFolder - Thư mục trong dự án
    /// Tổ chức cấu trúc thư mục cho files trong project
    /// </summary>
    [Table("ProjectFolders")]
    [Index(nameof(ProjectId), nameof(FolderName))]
    [Index(nameof(ParentFolderId), nameof(IsActive))]
    public class ProjectFolder : SoftDeletableEntity, IHasMetadata, IHasTags
    {
        /// <summary>
        /// ProjectId - ID dự án chứa thư mục này
        /// </summary>
        [Required]
        public int ProjectId { get; set; } = -1;

        /// <summary>
        /// ParentFolderId - ID thư mục cha (null nếu là root folder)
        /// </summary>
        public int ParentFolderId { get; set; } = -1; // -1 = root folder

        /// <summary>
        /// FolderName - Tên thư mục
        /// </summary>
        [Required]
        [StringLength(255)]
        public string FolderName { get; set; } = "";

        /// <summary>
        /// DisplayName - Tên hiển thị của thư mục
        /// </summary>
        [StringLength(255)]
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// Description - Mô tả thư mục
        /// </summary>
        [StringLength(1000)]
        public string Description { get; set; } = "";

        /// <summary>
        /// FolderPath - Đường dẫn đầy đủ của thư mục
        /// </summary>
        [StringLength(2000)]
        public string FolderPath { get; set; } = "";

        /// <summary>
        /// FolderLevel - Cấp độ thư mục (0 = root, 1 = level 1, ...)
        /// </summary>
        public int FolderLevel { get; set; } = 0;

        /// <summary>
        /// IsActive - Thư mục có đang active không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// IsPublic - Thư mục có public không
        /// </summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// IsReadOnly - Thư mục có readonly không
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// SortOrder - Thứ tự sắp xếp trong cùng level
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// IconName - Tên icon để hiển thị
        /// </summary>
        [StringLength(50)]
        public string IconName { get; set; } = "";

        /// <summary>
        /// Color - Màu sắc thư mục (hex color)
        /// </summary>
        [StringLength(7)]
        public string Color { get; set; } = "";

        /// <summary>
        /// Metadata - Thông tin metadata bổ sung (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Metadata { get; set; } = "{}";

        /// <summary>
        /// Tags - Các tags của thư mục (JSON array)
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
        [ForeignKey(nameof(ProjectId))]
        public virtual Project Project { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ParentFolderId))]
        public virtual ProjectFolder ParentFolder { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProjectFolder> SubFolders { get; set; } = new List<ProjectFolder>();

        [JsonIgnore]
        public virtual ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();

        /// <summary>
        /// Computed Properties
        /// </summary>
        [NotMapped]
        public bool IsRootFolder => ParentFolderId >= 0;

        [NotMapped]
        public int TotalFiles => ProjectFiles?.Count(f => f.IsActive && !f.IsDeleted) ?? 0;

        [NotMapped]
        public long TotalFileSize => ProjectFiles?.Where(f => f.IsActive && !f.IsDeleted).Sum(f => f.CurrentFileSize) ?? 0;

        [NotMapped]
        public int TotalSubFolders => SubFolders?.Count(sf => sf.IsActive && !sf.IsDeleted) ?? 0;

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
        /// GetFullPath - Lấy đường dẫn đầy đủ của thư mục
        /// </summary>
        public virtual string GetFullPath()
        {
            if (IsRootFolder)
                return FolderName;

            var path = new List<string>();
            var currentFolder = this;
            
            while (currentFolder != null && !currentFolder.IsRootFolder)
            {
                path.Insert(0, currentFolder.FolderName);
                currentFolder = currentFolder.ParentFolder;
            }
            
            if (currentFolder != null)
                path.Insert(0, currentFolder.FolderName);

            return string.Join("/", path);
        }

        /// <summary>
        /// UpdateFolderPath - Cập nhật đường dẫn thư mục
        /// </summary>
        public virtual void UpdateFolderPath()
        {
            FolderPath = GetFullPath();
        }

        /// <summary>
        /// GetAllDescendantFolders - Lấy tất cả thư mục con (đệ quy)
        /// </summary>
        public virtual IEnumerable<ProjectFolder> GetAllDescendantFolders()
        {
            var descendants = new List<ProjectFolder>();
            
            foreach (var subFolder in SubFolders.Where(sf => sf.IsActive && !sf.IsDeleted))
            {
                descendants.Add(subFolder);
                descendants.AddRange(subFolder.GetAllDescendantFolders());
            }
            
            return descendants;
        }

        /// <summary>
        /// GetAllDescendantFiles - Lấy tất cả file trong thư mục và thư mục con
        /// </summary>
        public virtual IEnumerable<ProjectFile> GetAllDescendantFiles()
        {
            var files = ProjectFiles.Where(f => f.IsActive && !f.IsDeleted).ToList();
            
            foreach (var subFolder in SubFolders.Where(sf => sf.IsActive && !sf.IsDeleted))
            {
                files.AddRange(subFolder.GetAllDescendantFiles());
            }
            
            return files;
        }

        /// <summary>
        /// CanMoveTo - Kiểm tra có thể move thư mục đến vị trí mới không
        /// </summary>
        public virtual bool CanMoveTo(int newParentFolderId)
        {
            // Không thể move vào chính nó
            if (newParentFolderId == Id)
                return false;

            // Không thể move vào thư mục con của nó
            var descendantIds = GetAllDescendantFolders().Select(f => f.Id);
            if (descendantIds.Contains(newParentFolderId))
                return false;

            return true;
        }

        /// <summary>
        /// MoveTo - Di chuyển thư mục đến vị trí mới
        /// </summary>
        public virtual void MoveTo(int newParentFolderId, int movedBy)
        {
            if (!CanMoveTo(newParentFolderId))
                throw new InvalidOperationException("Không thể move thư mục đến vị trí này");

            ParentFolderId = newParentFolderId;
            
            // Cập nhật level dựa trên parent mới
            if (newParentFolderId >= 0)
            {
                FolderLevel = 0;
            }
            else
            {
                // Tìm parent folder và cập nhật level
                // Note: Cần load ParentFolder để lấy level
                // FolderLevel = ParentFolder.FolderLevel + 1;
            }

            UpdateFolderPath();
            MarkAsUpdated(movedBy);

            // Cập nhật path cho tất cả thư mục con
            foreach (var subFolder in SubFolders.Where(sf => sf.IsActive && !sf.IsDeleted))
            {
                subFolder.UpdateFolderPath();
            }
        }

        /// <summary>
        /// Rename - Đổi tên thư mục
        /// </summary>
        public virtual void Rename(string newName, int renamedBy)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Tên thư mục không được rỗng");

            var oldName = FolderName;
            FolderName = newName.Trim();
            
            if (string.IsNullOrEmpty(DisplayName) || DisplayName == oldName)
            {
                DisplayName = FolderName;
            }

            UpdateFolderPath();
            MarkAsUpdated(renamedBy);

            // Cập nhật path cho tất cả thư mục con và file
            foreach (var subFolder in SubFolders.Where(sf => sf.IsActive && !sf.IsDeleted))
            {
                subFolder.UpdateFolderPath();
            }

            foreach (var file in ProjectFiles.Where(f => f.IsActive && !f.IsDeleted))
            {
                // Cập nhật relative path của file nếu cần
            }
        }

        /// <summary>
        /// CreateSubFolder - Tạo thư mục con
        /// </summary>
        public virtual ProjectFolder CreateSubFolder(string folderName, int createdBy, string description = "")
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentException("Tên thư mục không được rỗng");

            // Kiểm tra trung tên trong cùng cấp
            if (SubFolders.Any(sf => sf.IsActive && !sf.IsDeleted && sf.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Thư mục '{folderName}' đã tồn tại");

            var subFolder = new ProjectFolder
            {
                ProjectId = ProjectId,
                ParentFolderId = Id,
                FolderName = folderName.Trim(),
                DisplayName = folderName.Trim(),
                Description = description,
                FolderLevel = FolderLevel + 1,
                IsActive = true,
                CreatedBy = createdBy
            };

            subFolder.UpdateFolderPath();
            SubFolders.Add(subFolder);

            return subFolder;
        }

        /// <summary>
        /// GetBreadcrumb - Lấy breadcrumb navigation
        /// </summary>
        public virtual List<ProjectFolder> GetBreadcrumb()
        {
            var breadcrumb = new List<ProjectFolder>();
            var currentFolder = this;

            while (currentFolder != null)
            {
                breadcrumb.Insert(0, currentFolder);
                currentFolder = currentFolder.ParentFolder;
            }

            return breadcrumb;
        }
    }
}