using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Move file request DTO
    /// </summary>
    public class MoveFileRequest
    {
        [Required]
        public int NewFolderId { get; set; } 
    }
}
