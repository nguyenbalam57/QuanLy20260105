using ManagementFile.Models.BaseModels;
using ManagementFile.Models.ProjectManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ManagementFile.Models.TimeTracking
{
    /// <summary>
    /// TaskTimeLog - Log thời gian làm việc trên task
    /// Tracking chi tiết thời gian work
    /// </summary>
    [Table("TaskTimeLogs")]
    [Index(nameof(TaskId), nameof(UserId), nameof(StartTime))]
    public class TaskTimeLog : BaseEntity
    {
        /// <summary>
        /// TaskId - ID task được log time
        /// </summary>
        [Required]
        public int TaskId { get; set; }

        /// <summary>
        /// UserId - ID người làm việc
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// StartTime - Thời gian bắt đầu work
        /// Ngày giờ bắt đầu công việc
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// EndTime - Thời gian kết thúc work
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Duration - Thời lượng work (phút)
        /// </summary>
        public int Duration { get; set; } = 0;

        /// <summary>
        /// Description - Mô tả công việc đã làm
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Description { get; set; } = "";

        /// <summary>
        /// IsBillable - Có tính phí khách hàng không
        /// </summary>
        public bool IsBillable { get; set; } = true;

        /// <summary>
        /// HourlyRate - Mức lương giờ khi log (snapshot)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? HourlyRate { get; set; }

        /// <summary>
        /// Navigation Properties
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(TaskId))]
        public virtual ProjectTask ProjectTask { get; set; }

    }
}
