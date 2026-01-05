using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.UserManagement
{
    /// <summary>
    /// 
    /// </summary>
    public class CommentRequest
    {
        public string Content { get; set; } = "";

        [StringLength(450)]
        public string ParentId { get; set; }

        public List<string> FileIds { get; set; } = new List<string>();
        public List<string> VersionIds { get; set; } = new List<string>();
    }
}
