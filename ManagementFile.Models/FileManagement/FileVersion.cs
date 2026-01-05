using ManagementFile.Models.BaseModels;
using ManagementFile.Contracts.Enums;
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
    /// FileVersion - Phiên bản chi tiết của file
    /// Lưu trữ thông tin về từng phiên bản cụ thể của file
    /// </summary>
    [Table("FileVersions")]
    [Index(nameof(ProjectFileId), nameof(VersionNumber))]
    public class FileVersion : SoftDeletableEntity
    {
        /// <summary>
        /// ProjectFileId - ID của project file gốc
        /// </summary>
        [Required]
        public int ProjectFileId { get; set; } = -1;

        /// <summary>
        /// ChangeType - Loại thay đổi trong version này
        /// </summary>
        public FileChangeType ChangeType { get; set; } = FileChangeType.Modified;

        /// <summary>
        /// FileSize - Kích thước của version này
        /// </summary>
        public long FileSize { get; set; } = 0;

        /// <summary>
        /// FileHash - Hash của version này
        /// </summary>
        [StringLength(128)]
        public string FileHash { get; set; } = "";

        /// <summary>
        /// PhysicalPath - Đường dẫn vật lý của version này
        /// </summary>
        [StringLength(1000)]
        public string PhysicalPath { get; set; } = "";

        /// <summary>
        /// DiffFromPrevious - Diff so với version trước (JSON)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string DiffFromPrevious { get; set; } = "";

        /// <summary>
        /// VersionNotes - Ghi chú cho version này
        /// </summary>
        [StringLength(2000)]
        public string VersionNotes { get; set; } = "";

        /// <summary>
        /// IsCurrentVersion - Có phải version hiện tại không
        /// </summary>
        public bool IsCurrentVersion { get; set; } = false;

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(ProjectFileId))]
        public virtual ProjectFile ProjectFile { get; set; }

        [JsonIgnore]
        public virtual ICollection<FileComment> FileComments { get; set; } = new List<FileComment>();
    }
}
