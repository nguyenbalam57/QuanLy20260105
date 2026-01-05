using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// Enum định nghĩa các loại comment trên Task
    /// Phân loại comment theo mục đích và tính chất để dễ quản lý và filter
    /// </summary>
    public enum CommentType
    {
        /// <summary>
        /// Bình luận chung, thảo luận thông thường
        /// Không có yêu cầu cụ thể, chỉ là trao đổi thông tin
        /// Default type cho các comment đơn giản
        /// </summary>
        [Display(Name = "Bình luận chung", ShortName = "Bình luận chung")]
        [Description("Thảo luận và trao đổi thông tin chung")]
        General = 0,

        /// <summary>
        /// Góp ý từ quá trình review code/design
        /// Feedback từ reviewer về chất lượng, best practices
        /// Thường có độ ưu tiên cao và cần response từ developer
        /// </summary>
        [Display(Name = "Góp ý review", ShortName = "Review")]
        [Description("Feedback từ quá trình review code, design hoặc requirements")]
        ReviewFeedback = 1,

        /// <summary>
        /// Báo cáo lỗi, bug, defect
        /// Issue cần được fix, có impact đến functionality
        /// Thường blocking hoặc high priority
        /// </summary>
        [Display(Name = "Báo lỗi/vấn đề", ShortName = "Báo lỗi")]
        [Description("Báo cáo bug, lỗi hệ thống hoặc vấn đề kỹ thuật")]
        IssueReport = 2,

        /// <summary>
        /// Yêu cầu thay đổi requirement, design, implementation
        /// Change request từ client hoặc stakeholder
        /// Có thể impact timeline và scope
        /// </summary>
        [Display(Name = "Yêu cầu thay đổi", ShortName = "Thay đổi")]
        [Description("Yêu cầu modify requirements, design hoặc implementation")]
        ChangeRequest = 3,

        /// <summary>
        /// Cập nhật tiến độ công việc, status task
        /// Thông báo progress, milestone đạt được
        /// Thường là system comment hoặc manual update từ assignee
        /// </summary>
        [Display(Name = "Cập nhật tiến độ", ShortName = "Cập nhật")]
        [Description("Thông báo progress và trạng thái công việc")]
        StatusUpdate = 4,

        /// <summary>
        /// Yêu cầu làm rõ requirement, specification
        /// Khi developer cần thêm thông tin để implement
        /// Thường cần response từ BA, client hoặc reviewer
        /// </summary>
        [Display(Name = "Làm rõ yêu cầu", ShortName = "Làm rõ")]
        [Description("Yêu cầu clarification về requirements hoặc specifications")]
        Clarification = 5,

        /// <summary>
        /// Phê duyệt task, solution, implementation
        /// Formal approval từ reviewer, team lead, client
        /// Mark completion của review process
        /// </summary>
        [Display(Name = "Phê duyệt", ShortName = "Phê duyệt")]
        [Description("Chấp thuận và phê duyệt solution hoặc implementation")]
        Approval = 6,

        /// <summary>
        /// Từ chối solution, approach, implementation
        /// Formal rejection với lý do cụ thể
        /// Yêu cầu rework hoặc alternative solution
        /// </summary>
        [Display(Name = "Từ chối", ShortName = "Từ chối")]
        [Description("Không chấp thuận solution hiện tại, yêu cầu thay đổi")]
        Rejection = 7,

        /// <summary>
        /// Câu hỏi về technical, business logic, process
        /// Yêu cầu thông tin, hướng dẫn từ team members
        /// Cần response để unblock work
        /// </summary>
        [Display(Name = "Câu hỏi", ShortName = "Câu hỏi")]
        [Description("Đặt câu hỏi về technical, business hoặc process")]
        Question = 8,

        /// <summary>
        /// Đề xuất improvement, optimization, best practice
        /// Suggestion để enhance quality, performance, maintainability
        /// Optional implementation, không blocking
        /// </summary>
        [Display(Name = "Đề xuất", ShortName = "Đề xuất")]
        [Description("Gợi ý cải thiện và tối ưu hóa")]
        Suggestion = 9,

        [Display(Name = "Tất cả loại", ShortName = "Tất cả")]
        [Description("Lựa chọn tất cả các loại bình luận")]
        All = 99
    }

    /// <summary>
    /// Extension methods cho CommentType enum
    /// Cung cấp utility methods để work với enum
    /// Compatible với .NET Framework và .NET Core/5+
    /// </summary>
    public static class CommentTypeExtensions
    {
        /// <summary>
        /// Lấy display name của enum value
        /// Sử dụng Display attribute hoặc fallback về enum name
        /// </summary>
        /// <param name="commentType">Enum value</param>
        /// <returns>Display name string</returns>
        public static string GetDisplayName(this CommentType commentType)
        {
            var fieldInfo = commentType.GetType().GetField(commentType.ToString());
            var displayAttribute = fieldInfo?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return displayAttribute?.Name ?? commentType.ToString();
        }

        /// <summary>
        /// Lấy description của enum value
        /// Sử dụng Display.Description attribute
        /// </summary>
        /// <param name="commentType">Enum value</param>
        /// <returns>Description string</returns>
        public static string GetDescription(this CommentType commentType)
        {
            var fieldInfo = commentType.GetType().GetField(commentType.ToString());
            var displayAttribute = fieldInfo?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return displayAttribute?.Description ?? commentType.ToString();
        }

        /// <summary>
        /// Kiểm tra comment type có cần response không
        /// Một số type như Question, Clarification cần response để unblock
        /// </summary>
        /// <param name="commentType">Enum value</param>
        /// <returns>True nếu cần response</returns>
        public static bool RequiresResponse(this CommentType commentType)
        {
            switch (commentType)
            {
                case CommentType.Question:
                case CommentType.Clarification:
                case CommentType.ChangeRequest:
                case CommentType.IssueReport:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Kiểm tra comment type có thể blocking task không
        /// Một số type như IssueReport, ChangeRequest có thể block progress
        /// </summary>
        /// <param name="commentType">Enum value</param>
        /// <returns>True nếu có thể blocking</returns>
        public static bool CanBeBlocking(this CommentType commentType)
        {
            switch (commentType)
            {
                case CommentType.IssueReport:
                case CommentType.ChangeRequest:
                case CommentType.Rejection:
                case CommentType.Clarification:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Lấy default priority cho comment type
        /// Mỗi type có mức độ ưu tiên khác nhau
        /// </summary>
        /// <param name="commentType">Enum value</param>
        /// <returns>Default priority string</returns>
        public static TaskPriority GetDefaultPriority(this CommentType commentType)
        {
            switch (commentType)
            {
                case CommentType.IssueReport:
                case CommentType.Rejection:
                    return TaskPriority.High;
                case CommentType.ChangeRequest:
                case CommentType.ReviewFeedback:
                case CommentType.Clarification:
                case CommentType.Question:
                    return TaskPriority.Normal;
                case CommentType.Approval:
                case CommentType.StatusUpdate:
                case CommentType.Suggestion:
                case CommentType.General:
                    return TaskPriority.Low;
                default:
                    return TaskPriority.All;
            }
        }

        /// <summary>
        /// Lấy icon emoji cho comment type (dùng cho UI)
        /// Visual indicator để dễ nhận biết type
        /// </summary>
        /// <param name="commentType">Enum value</param>
        /// <returns>Emoji string</returns>
        public static string GetIcon(this CommentType commentType)
        {
            switch (commentType)
            {
                case CommentType.General:
                    return "💬";
                case CommentType.ReviewFeedback:
                    return "👁️";
                case CommentType.IssueReport:
                    return "🐛";
                case CommentType.ChangeRequest:
                    return "🔄";
                case CommentType.StatusUpdate:
                    return "📊";
                case CommentType.Clarification:
                    return "❓";
                case CommentType.Approval:
                    return "✅";
                case CommentType.Rejection:
                    return "❌";
                case CommentType.Question:
                    return "🤔";
                case CommentType.Suggestion:
                    return "💡";
                default:
                    return "💬";
            }
        }

        /// <summary>
        /// Lấy CSS class cho styling comment type badge
        /// Mỗi type có màu sắc khác nhau
        /// </summary>
        /// <param name="commentType">Enum value</param>
        /// <returns>CSS class name</returns>
        public static string GetCssClass(this CommentType commentType)
        {
            switch (commentType)
            {
                case CommentType.General:
                    return "comment-general";
                case CommentType.ReviewFeedback:
                    return "comment-review";
                case CommentType.IssueReport:
                    return "comment-issue";
                case CommentType.ChangeRequest:
                    return "comment-change";
                case CommentType.StatusUpdate:
                    return "comment-status";
                case CommentType.Clarification:
                    return "comment-clarification";
                case CommentType.Approval:
                    return "comment-approval";
                case CommentType.Rejection:
                    return "comment-rejection";
                case CommentType.Question:
                    return "comment-question";
                case CommentType.Suggestion:
                    return "comment-suggestion";
                default:
                    return "comment-default";
            }
        }

        /// <summary>
        /// Lấy màu hex cho comment type badge
        /// Consistent color scheme cho UI components
        /// </summary>
        /// <param name="commentType">Enum value</param>
        /// <returns>Hex color code</returns>
        public static string GetHexColor(this CommentType commentType)
        {
            switch (commentType)
            {
                case CommentType.General:
                    return "#95A5A6";        // Gray
                case CommentType.ReviewFeedback:
                    return "#3498DB";        // Blue
                case CommentType.IssueReport:
                    return "#E74C3C";        // Red
                case CommentType.ChangeRequest:
                    return "#F39C12";        // Orange
                case CommentType.StatusUpdate:
                    return "#2ECC71";        // Green
                case CommentType.Clarification:
                    return "#9B59B6";        // Purple
                case CommentType.Approval:
                    return "#27AE60";        // Dark Green
                case CommentType.Rejection:
                    return "#C0392B";        // Dark Red
                case CommentType.Question:
                    return "#F1C40F";        // Yellow
                case CommentType.Suggestion:
                    return "#1ABC9C";        // Turquoise
                default:
                    return "#95A5A6";        // Default Gray
            }
        }

        /// <summary>
        /// Try parse string thành CommentType enum
        /// Implement Try pattern manually để tương thích
        /// </summary>
        /// <param name="value">String value to parse</param>
        /// <param name="result">Out parameter with parsed result</param>
        /// <returns>True if parse successful</returns>
        public static bool TryParseCommentType(string value, out CommentType result)
        {
            result = CommentType.General;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                result = (CommentType)Enum.Parse(typeof(CommentType), value, true);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Parse string thành CommentType enum
        /// Case-insensitive parsing với fallback
        /// </summary>
        /// <param name="value">String value</param>
        /// <param name="defaultValue">Default nếu parse failed</param>
        /// <returns>CommentType enum</returns>
        public static CommentType ParseCommentType(string value, CommentType defaultValue = CommentType.General)
        {
            if (TryParseCommentType(value, out CommentType result))
                return result;

            return defaultValue;
        }

        /// <summary>
        /// Lấy tất cả CommentType values dưới dạng dictionary
        /// Key = enum value, Value = display name
        /// Dùng cho dropdown, select options
        /// </summary>
        /// <returns>Dictionary of enum values and display names</returns>
        public static Dictionary<CommentType, string> GetAllWithDisplayNames()
        {
            var result = new Dictionary<CommentType, string>();
            var values = Enum.GetValues(typeof(CommentType)).Cast<CommentType>();

            foreach (var type in values)
            {
                result[type] = type.GetDisplayName();
            }

            return result;
        }

        /// <summary>
        /// Lấy CommentType values theo filter criteria
        /// VD: chỉ lấy types có thể blocking, cần response, etc.
        /// </summary>
        /// <param name="requiresResponse">Filter types cần response</param>
        /// <param name="canBeBlocking">Filter types có thể blocking</param>
        /// <returns>Filtered enum values</returns>
        public static IEnumerable<CommentType> GetFilteredTypes(bool? requiresResponse = null, bool? canBeBlocking = null)
        {
            var allTypes = Enum.GetValues(typeof(CommentType)).Cast<CommentType>();

            if (requiresResponse.HasValue)
                allTypes = allTypes.Where(t => t.RequiresResponse() == requiresResponse.Value);

            if (canBeBlocking.HasValue)
                allTypes = allTypes.Where(t => t.CanBeBlocking() == canBeBlocking.Value);

            return allTypes;
        }

        /// <summary>
        /// Lấy tất cả enum values dưới dạng list
        /// Compatible với older .NET versions
        /// </summary>
        /// <returns>List of all CommentType values</returns>
        public static List<CommentType> GetAllValues()
        {
            return Enum.GetValues(typeof(CommentType)).Cast<CommentType>().ToList();
        }

        /// <summary>
        /// Kiểm tra enum value có hợp lệ không
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True if valid enum value</returns>
        public static bool IsValidCommentType(int value)
        {
            return Enum.IsDefined(typeof(CommentType), value);
        }

        /// <summary>
        /// Kiểm tra string value có map với enum không
        /// </summary>
        /// <param name="value">String value to check</param>
        /// <returns>True if can parse to valid enum</returns>
        public static bool IsValidCommentType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return Enum.TryParse<CommentType>(value, true, out _);
        }

        /// <summary>
        /// Lấy random CommentType (dùng cho testing)
        /// </summary>
        /// <param name="random">Random instance</param>
        /// <returns>Random CommentType value</returns>
        public static CommentType GetRandomCommentType(Random random = null)
        {
            if (random == null)
                random = new Random();

            var values = GetAllValues();
            return values[random.Next(values.Count)];
        }

        /// <summary>
        /// Lấy CommentType từ index
        /// Safe getter với bounds checking
        /// </summary>
        /// <param name="index">Index value</param>
        /// <returns>CommentType at index or General if out of bounds</returns>
        public static CommentType GetByIndex(int index)
        {
            var values = GetAllValues();
            if (index >= 0 && index < values.Count)
                return values[index];

            return CommentType.General;
        }

        /// <summary>
        /// Parse CommentType từ Display Name
        /// Tìm enum value dựa trên display name
        /// </summary>
        /// <param name="displayName">Display name to search</param>
        /// <param name="result">Out parameter với parsed result</param>
        /// <returns>True if found matching display name</returns>
        public static bool TryParseFromDisplayName(string displayName, out CommentType result)
        {
            result = CommentType.General;

            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            var allTypes = GetAllWithDisplayNames();
            foreach (var kvp in allTypes)
            {
                if (string.Equals(kvp.Value, displayName, StringComparison.OrdinalIgnoreCase))
                {
                    result = kvp.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Lấy CommentType từ description
        /// </summary>
        /// <param name="description">Description to search</param>
        /// <param name="result">Out parameter với parsed result</param>
        /// <returns>True if found matching description</returns>
        public static bool TryParseFromDescription(string description, out CommentType result)
        {
            result = CommentType.General;

            if (string.IsNullOrWhiteSpace(description))
                return false;

            var allValues = GetAllValues();
            foreach (var type in allValues)
            {
                if (string.Equals(type.GetDescription(), description, StringComparison.OrdinalIgnoreCase))
                {
                    result = type;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Smart parse - thử parse từ nhiều nguồn
        /// Parse theo thứ tự: enum name -> display name -> description
        /// </summary>
        /// <param name="value">Value to parse</param>
        /// <param name="result">Out parameter với parsed result</param>
        /// <returns>True if parse successful from any source</returns>
        public static bool TrySmartParse(string value, out CommentType result)
        {
            result = CommentType.General;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Try enum name first
            if (TryParseCommentType(value, out result))
                return true;

            // Try display name
            if (TryParseFromDisplayName(value, out result))
                return true;

            // Try description
            if (TryParseFromDescription(value, out result))
                return true;

            return false;
        }

        /// <summary>
        /// Lấy suggestion cho user input
        /// Tìm các CommentType có tên gần giống với input
        /// </summary>
        /// <param name="input">User input</param>
        /// <param name="maxSuggestions">Max số suggestions</param>
        /// <returns>List suggestions</returns>
        public static List<CommentType> GetSuggestions(string input, int maxSuggestions = 5)
        {
            if (string.IsNullOrWhiteSpace(input))
                return GetAllValues().Take(maxSuggestions).ToList();

            var suggestions = new List<CommentType>();
            var allTypes = GetAllValues();

            // Exact match priority
            foreach (var type in allTypes)
            {
                if (type.ToString().Equals(input, StringComparison.OrdinalIgnoreCase) ||
                    type.GetDisplayName().Equals(input, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Insert(0, type); // Add to front
                }
            }

            // Partial match
            foreach (var type in allTypes)
            {
                if (!suggestions.Contains(type) &&
                    (type.ToString().IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     type.GetDisplayName().IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    suggestions.Add(type);
                }
            }

            return suggestions.Take(maxSuggestions).ToList();
        }

        /// <summary>
        /// Convert CommentType sang full info object
        /// Dùng cho API responses và UI binding
        /// </summary>
        /// <param name="commentType">CommentType to convert</param>
        /// <returns>Anonymous object với full info</returns>
        public static object ToFullInfo(this CommentType commentType)
        {
            return new
            {
                Value = commentType,
                Name = commentType.ToString(),
                DisplayName = commentType.GetDisplayName(),
                Description = commentType.GetDescription(),
                Icon = commentType.GetIcon(),
                Color = commentType.GetHexColor(),
                CssClass = commentType.GetCssClass(),
                DefaultPriority = commentType.GetDefaultPriority(),
                RequiresResponse = commentType.RequiresResponse(),
                CanBeBlocking = commentType.CanBeBlocking(),
                NumericValue = (int)commentType
            };
        }

        /// <summary>
        /// Lấy tất cả CommentTypes với full info
        /// Dùng cho API endpoints và UI dropdowns
        /// </summary>
        /// <returns>List of full info objects</returns>
        public static List<object> GetAllFullInfo()
        {
            return GetAllValues().Select(type => type.ToFullInfo()).ToList();
        }

        /// <summary>
        /// Validate CommentType có phù hợp với context không
        /// </summary>
        /// <param name="commentType">CommentType to validate</param>
        /// <param name="isBlocking">Context có blocking không</param>
        /// <param name="hasAssignee">Context có assignee không</param>
        /// <param name="isReply">Context có phải reply không</param>
        /// <returns>Validation result với messages</returns>
        public static CommentTypeValidationResult ValidateForContext(
            this CommentType commentType,
            bool isBlocking = false,
            bool hasAssignee = false,
            bool isReply = false)
        {
            var result = new CommentTypeValidationResult { IsValid = true };

            // Validate blocking context
            if (isBlocking && !commentType.CanBeBlocking())
            {
                result.IsValid = false;
                result.Messages.Add($"CommentType '{commentType.GetDisplayName()}' không thể được đánh dấu blocking.");
            }

            // Validate assignee context
            if (hasAssignee && commentType == CommentType.General)
            {
                result.Warnings.Add("CommentType 'General' thường không cần assignee.");
            }

            // Validate reply context
            if (isReply && commentType == CommentType.StatusUpdate)
            {
                result.Warnings.Add("StatusUpdate comment thường không nên là reply.");
            }

            // Additional business rules
            if (commentType == CommentType.IssueReport && !hasAssignee)
            {
                result.Warnings.Add("IssueReport thường cần có assignee để xử lý.");
            }

            if (commentType.RequiresResponse() && isReply)
            {
                result.Messages.Add($"CommentType '{commentType.GetDisplayName()}' cần response, hãy đảm bảo ai đó sẽ trả lời.");
            }

            return result;
        }

        /// <summary>
        /// Lấy CommentTypes tương thích với workflow step
        /// </summary>
        /// <param name="workflowStep">Current workflow step</param>
        /// <returns>List compatible CommentTypes</returns>
        public static List<CommentType> GetCompatibleTypes(string workflowStep)
        {
            switch(workflowStep.ToLower())
            {
                case "planning":
                    return new List<CommentType> { CommentType.General, CommentType.Question, CommentType.Suggestion, CommentType.ChangeRequest };
                case "development":
                    return new List<CommentType> { CommentType.General, CommentType.Question, CommentType.IssueReport, CommentType.ReviewFeedback };
                case "review":
                    return new List<CommentType> { CommentType.ReviewFeedback, CommentType.Approval, CommentType.Rejection, CommentType.IssueReport };
                case "testing":
                    return new List<CommentType> { CommentType.IssueReport, CommentType.General, CommentType.Question };
                case "completed":
                    return new List<CommentType> { CommentType.General, CommentType.StatusUpdate };
                default:
                    return GetAllValues();
            }

        }

        /// <summary>
        /// Group CommentTypes theo category cho UI
        /// </summary>
        /// <returns>Dictionary grouped by category</returns>
        public static Dictionary<string, List<CommentType>> GetGroupedByCategory()
        {
            return new Dictionary<string, List<CommentType>>
            {
                ["Communication"] = new List<CommentType> { CommentType.General, CommentType.Question, CommentType.StatusUpdate },
                ["Review Process"] = new List<CommentType> { CommentType.ReviewFeedback, CommentType.Approval, CommentType.Rejection },
                ["Issue Management"] = new List<CommentType> { CommentType.IssueReport, CommentType.ChangeRequest, CommentType.Clarification },
                ["Improvement"] = new List<CommentType> { CommentType.Suggestion }
            };
        }

        /// <summary>
        /// Tạo CommentType recommendation dựa trên content
        /// Simple keyword-based recommendation
        /// </summary>
        /// <param name="content">Comment content</param>
        /// <returns>Recommended CommentType</returns>
        public static CommentType RecommendFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return CommentType.General;

            var lowerContent = content.ToLower();

            // Keywords mapping
            var keywords = new Dictionary<CommentType, string[]>
            {
                [CommentType.IssueReport] = new[] { "bug", "error", "issue", "problem", "broken", "not working", "fail" },
                [CommentType.Question] = new[] { "?", "how", "what", "why", "when", "where", "question" },
                [CommentType.Suggestion] = new[] { "suggest", "recommend", "should", "could", "better", "improve" },
                [CommentType.Approval] = new[] { "approve", "approved", "good", "lgtm", "looks good", "accept" },
                [CommentType.Rejection] = new[] { "reject", "no", "wrong", "incorrect", "disagree", "not acceptable" },
                [CommentType.ChangeRequest] = new[] { "change", "modify", "update", "different", "instead" },
                [CommentType.Clarification] = new[] { "clarify", "unclear", "confuse", "explain", "detail" }
            };

            foreach (var kvp in keywords)
            {
                if (kvp.Value.Any(keyword => lowerContent.Contains(keyword)))
                {
                    return kvp.Key;
                }
            }

            return CommentType.General;
        }

        public static ObservableCollection<CommentTypeItem> GetCommentTypeItems()
        {
            var items = new ObservableCollection<CommentTypeItem>();
            foreach (CommentType type in Enum.GetValues(typeof(CommentType)).Cast<CommentType>())
            {
                items.Add(new CommentTypeItem(type));
            }
            return items;
        }

        /// <summary>
        /// Lấy danh sách ProjectStatusItem không bao gồm "All"
        /// </summary>
        public static ObservableCollection<CommentTypeItem> GetCommentTypeItemsWithoutAll()
        {
            var items = new ObservableCollection<CommentTypeItem>();
            foreach (CommentType status in Enum.GetValues(typeof(CommentType))
                .Cast<CommentType>()
                .Where(s => s != CommentType.All))
            {
                items.Add(new CommentTypeItem(status));
            }
            return items;
        }

    }

    #region Supporting Classes

    /// <summary>
    /// Result class cho CommentType validation
    /// </summary>
    public class CommentTypeValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Messages { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public bool HasWarnings => Warnings.Count > 0;
        public bool HasMessages => Messages.Count > 0;

        public string GetAllMessages()
        {
            var allMessages = new List<string>();
            allMessages.AddRange(Messages);
            allMessages.AddRange(Warnings.Select(w => $"Warning: {w}"));
            return string.Join("; ", allMessages);
        }
    }

    #endregion

    public class CommentTypeItem
    {
        public CommentTypeItem()
        {

        }

        public CommentTypeItem(CommentType value)
        {
            Value = value;
            DisplayName = value.GetDisplayName();
            Description = value.GetDescription();
            Icon = value.GetIcon();
            Color = value.GetHexColor();
        }

        public CommentType Value { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string DisplayText => $"{Icon} {DisplayName}";
    }
}