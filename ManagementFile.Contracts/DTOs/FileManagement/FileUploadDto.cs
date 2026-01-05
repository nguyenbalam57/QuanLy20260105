using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// File upload with content DTO
    /// </summary>
    public class FileUploadDto
    {
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long FileSize { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
