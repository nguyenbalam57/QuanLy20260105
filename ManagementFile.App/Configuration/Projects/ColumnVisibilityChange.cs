using ManagementFile.App.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Configuration.Projects
{
    /// <summary>
    /// Model để track column visibility changes
    /// </summary>
    public class ColumnVisibilityChange
    {
        public string ColumnName { get; set; }
        public string CheckBoxName { get; set; }
        public bool OldValue { get; set; }
        public bool NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
        public string Reason { get; set; } = "User preference";

        public override string ToString()
        {
            return $"{ChangedAt:HH:mm:ss} - {ColumnName}: {OldValue} → {NewValue} by {ChangedBy}";
        }
    }

}
