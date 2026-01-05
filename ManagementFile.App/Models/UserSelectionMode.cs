using System;

namespace ManagementFile.App.Models
{
    /// <summary>
    /// Chế độ chọn user (đơn hoặc đa)
    /// </summary>
    public enum UserSelectionMode
    {
        /// <summary>
        /// Chọn một user duy nhất
        /// </summary>
        Single,
        
        /// <summary>
        /// Chọn nhiều user
        /// </summary>
        Multi
    }
}