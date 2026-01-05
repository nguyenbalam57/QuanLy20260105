using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.FileManagement
{
    /// <summary>
    /// Share access request DTO
    /// </summary>
    public class ShareAccessRequest
    {
        [Required]
        public string ShareToken { get; set; } = "";

        public string Password { get; set; } = "";
    }
}
