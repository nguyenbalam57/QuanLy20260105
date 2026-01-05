using ManagementFile.Contracts.DTOs.UserManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Responses.UserManagement
{
    /// <summary>
    /// 
    /// </summary>
    public class CommentResponse
    {
        public int Id { get; set; } = -1;
        public string Content { get; set; } = "";
        public int CreatorId { get; set; } = -1;
        public int ParentId { get; set; } = -1;
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public UserDto Creator { get; set; } = null;
        public List<CommentResponse> Replies { get; set; } = new List<CommentResponse>();
    }
}
