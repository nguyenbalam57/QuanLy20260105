using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.ProjectMembers
{
    /// <summary>
    /// UpdateProjectMemberRequest - Request model để cập nhật thông tin member trong project
    /// Kế thừa từ BaseProjectMemberRequest để đảm bảo tính đồng nhất
    /// Tất cả thuộc tính đều nullable để chỉ cập nhật những field được cung cấp
    /// </summary>
    public class UpdateProjectMemberRequest : BaseProjectMemberRequest
    {

        /// <summary>
        /// JoinedAt - Ngày tham gia dự án (có thể cập nhật nếu cần)
        /// Thường không thay đổi sau khi đã set, nhưng có thể điều chỉnh trong một số trường hợp đặc biệt
        /// </summary>
        public DateTime? JoinedAt { get; set; }

        /// <summary>
        /// LeftAt - Ngày rời dự án (để đánh dấu member rời project)
        /// Khi set giá trị này, thường sẽ đồng thời set IsActive = false
        /// </summary>
        public DateTime? LeftAt { get; set; }

        public byte[] RowVersion { get; set; }
    }
}
