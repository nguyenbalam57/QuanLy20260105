using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectMembers
{
    /// <summary>
    /// CreateProjectMemberRequest - Request model để tạo member mới cho project
    /// Kế thừa từ BaseProjectMemberRequest để đảm bảo tính đồng nhất
    /// Chứa tất cả thông tin cần thiết để thêm một member vào project
    /// </summary>
    public class CreateProjectMemberRequest : BaseProjectMemberRequest
    {
        /// <summary>
        /// UserId - ID của người dùng được thêm vào project
        /// Bắt buộc phải có để xác định user nào được thêm vào project
        /// </summary>
        [Required(ErrorMessage = "UserId là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "UserId phải lớn hơn 0")]
        public int UserId { get; set; }

        /// <summary>
        /// JoinedAt - Ngày bắt đầu tham gia dự án (tùy chọn)
        /// Nếu không cung cấp sẽ sử dụng thời gian hiện tại khi tạo entity
        /// </summary>
        public DateTime? JoinedAt { get; set; }

    }
}
