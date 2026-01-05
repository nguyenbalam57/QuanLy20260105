using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Update file comment request DTO
    /// </summary>
    public class UpdateFileCommentRequest
    {
        [Required]
        public string Content { get; set; } = "";
    }
}
