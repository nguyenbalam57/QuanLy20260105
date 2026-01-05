using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Move folder request DTO
    /// </summary>
    public class MoveFolderRequest
    {
        [Required]
        public int NewParentFolderId { get; set; } = -1;
    }
}
