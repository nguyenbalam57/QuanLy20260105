using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TaskComments
{
    /// <summary>
    /// DTO để tạo TaskComment mới
    /// Kế thừa từ BaseTaskCommentDto và thêm các field đặc thù cho Create
    /// </summary>
    public class CreateTaskCommentRequest : BaseTaskCommentRequest
    {
        /// <summary>
        /// ID của task sẽ chứa comment này
        /// Must exist trong ProjectTask table
        /// Required cho Create operation
        /// </summary>
        [Required(ErrorMessage = "TaskId là bắt buộc")]
        public int TaskId { get; set; }

        /// <summary>
        /// ID comment cha nếu đây là reply
        /// null = tạo comment mới, có giá trị = reply existing comment
        /// Must exist và không được circular reference
        /// Chỉ set khi tạo reply, không thể change sau đó
        /// </summary>
        public int? ParentCommentId { get; set; }

        /// <summary>
        /// Auto assign cho creator không
        /// true = tự động assign comment cho người tạo
        /// false = để trống assignee, assign manual sau
        /// Default false để tránh spam assignment
        /// </summary>
        public bool AutoAssignToCreator { get; set; } = false;

        /// <summary>
        /// Gửi notification không
        /// true = send notification cho mentioned users và assignees
        /// false = tạo silent comment
        /// Default true cho user experience
        /// </summary>
        public bool SendNotification { get; set; } = true;
    }
}
