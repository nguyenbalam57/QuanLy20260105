using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Configuration.Projects
{
    /// <summary>
    /// Configuration cho một column
    /// </summary>
    public class ColumnConfig
    {
        public string ColumnName { get; set; }
        public string CheckBoxName { get; set; }
        public bool IsVisibleByDefault { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public int SortOrder { get; set; }
        public bool IsEssential { get; set; } // Không thể ẩn

        public ColumnConfig(string columnName, string checkBoxName, bool isVisibleByDefault,
                           string displayName = "", string category = "General", int sortOrder = 0, bool isEssential = false)
        {
            ColumnName = columnName;
            CheckBoxName = checkBoxName;
            IsVisibleByDefault = isVisibleByDefault;
            DisplayName = string.IsNullOrEmpty(displayName) ? columnName : displayName;
            Category = category;
            SortOrder = sortOrder;
            IsEssential = isEssential;
        }
    }

    /// <summary>
    /// Manager cho column configurations
    /// </summary>
    public static class TaskCommentColumnConfigurationManager
    {
        /// <summary>
        /// Default column configurations - Có thể tùy chỉnh ở đây
        /// </summary>
        public static List<ColumnConfig> GetDefaultColumnConfigurations()
        {
            return new List<ColumnConfig>
            {
                // Basic columns - Essential và visible by default
                new ColumnConfig("IdColumn", "IdColumnCheckBox", true, "🆔 ID Comment", "Basic", 1, true),
                new ColumnConfig("ContentColumn", "ContentColumnCheckBox", true, "📝 Nội dung", "Basic", 2, true),
                new ColumnConfig("TypeColumn", "TypeColumnCheckBox", true, "🏷️ Loại comment", "Basic", 3),
                new ColumnConfig("AuthorColumn", "AuthorColumnCheckBox", true, "👤 Người tạo", "Basic", 4),
                new ColumnConfig("TimeColumn", "TimeColumnCheckBox", true, "⏰ Thời gian", "Basic", 5),

                // Status & Priority - Some visible by default
                new ColumnConfig("StatusColumn", "StatusColumnCheckBox", false, "📊 Trạng thái", "Status", 6),
                new ColumnConfig("PriorityColumn", "PriorityColumnCheckBox", true, "🎯 Ưu tiên", "Status", 7), // Changed to true

                // Assignment - Hidden by default
                new ColumnConfig("AssigneeColumn", "AssigneeColumnCheckBox", false, "👥 Người thực hiện", "Assignment", 8),
                new ColumnConfig("ReviewerColumn", "ReviewerColumnCheckBox", false, "🔍 Người xác nhận", "Assignment", 9),

                // Issue Details - Mixed visibility
                new ColumnConfig("IssueTitleColumn", "IssueTitleColumnCheckBox", true, "🐛 Tiêu đề", "Issue", 10), // Changed to true
                new ColumnConfig("SuggestedFixColumn", "SuggestedFixColumnCheckBox", false, "💡 Cách chỉnh sửa", "Issue", 11),
                new ColumnConfig("RelatedModuleColumn", "RelatedModuleColumnCheckBox", false, "📦 Liên quan đến", "Issue", 12),

                // Time Tracking - Hidden by default
                new ColumnConfig("DueDateColumn", "DueDateColumnCheckBox", false, "📅 Hạn chót", "Time", 13),
                new ColumnConfig("EstimatedTimeColumn", "EstimatedTimeColumnCheckBox", false, "⏱️ Số giờ ước tính", "Time", 14),
                new ColumnConfig("ActualTimeColumn", "ActualTimeColumnCheckBox", false, "⏲️ Số giờ thực tế", "Time", 15),

                // Resolution - Important ones visible
                new ColumnConfig("ResolvedColumn", "ResolvedColumnCheckBox", true, "✅ Đã giải quyết", "Resolution", 16), // Changed to true
                new ColumnConfig("VerifiedColumn", "VerifiedColumnCheckBox", false, "🔒 Đã xác nhận", "Resolution", 17),

                // Flags - Mixed visibility
                new ColumnConfig("BlockingColumn", "BlockingColumnCheckBox", true, "🚫 Đã khóa", "Flags", 18), // Changed to true
                new ColumnConfig("DiscussionColumn", "DiscussionColumnCheckBox", false, "💬 Yêu cầu thảo luận", "Flags", 19),
                new ColumnConfig("TagsColumn", "TagsColumnCheckBox", false, "🏷️ Tags", "Flags", 20)
            };
        }

        /// <summary>
        /// Get configuration by column name
        /// </summary>
        public static ColumnConfig GetColumnConfig(string columnName)
        {
            return GetDefaultColumnConfigurations().FirstOrDefault(c => c.ColumnName == columnName);
        }

        /// <summary>
        /// Get configuration by checkbox name
        /// </summary>
        public static ColumnConfig GetColumnConfigByCheckBox(string checkBoxName)
        {
            return GetDefaultColumnConfigurations().FirstOrDefault(c => c.CheckBoxName == checkBoxName);
        }
    }
}
