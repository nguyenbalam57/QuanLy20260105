using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Models.Dialogs
{
    /// <summary>
    /// Configuration for a field in CommentLine dialog
    /// </summary>
    public class CommentLineFieldConfig
    {
        /// <summary>
        /// Field label text displayed above the input
        /// </summary>
        public string Label { get; set; } = "Nhập nội dung";

        /// <summary>
        /// Placeholder text shown when field is empty
        /// </summary>
        public string Placeholder { get; set; } = "";

        /// <summary>
        /// Whether the field is required (shows * indicator and validates)
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// Whether the field is multiline (textarea) - default is single line
        /// </summary>
        public bool Multiline { get; set; } = false;

        /// <summary>
        /// Maximum character length (0 = no limit). Shows character counter if > 0 and Multiline = true
        /// </summary>
        public int MaxLength { get; set; } = 0;
    }
}
