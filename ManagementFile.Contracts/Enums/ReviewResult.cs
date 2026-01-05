using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ManagementFile.Contracts.Enums
{
    /// <summary>
    /// ReviewResult - Kết quả đánh giá/phê duyệt
    /// Định nghĩa các outcome có thể có trong quá trình review, approval workflow
    /// Hỗ trợ quality assurance, document approval, code review processes
    /// </summary>
    public enum ReviewResult
    {
        /// <summary>Pending - Chờ đánh giá</summary>
        /// <remarks>
        /// Trạng thái ban đầu khi tạo review request:
        /// - Review chưa được bắt đầu
        /// - Chờ reviewer assign hoặc pickup
        /// - Item đang trong queue chờ xử lý
        /// - Có thể có deadline cho review
        /// - Reviewer có thể cancel hoặc reassign
        /// </remarks>
        [Description("Đang chờ đánh giá")]
        Pending = 0,

        /// <summary>Approved - Chấp thuận hoàn toàn</summary>
        /// <remarks>
        /// Review đã hoàn thành và được chấp thuận:
        /// - Đáp ứng tất cả tiêu chí quality
        /// - Không cần thay đổi gì thêm
        /// - Có thể proceed to next stage
        /// - Item ready for production/release
        /// - May trigger automated workflows
        /// - Final approval trong multi-stage process
        /// </remarks>
        [Description("Đã phê duyệt")]
        Approved = 1,

        /// <summary>Rejected - Từ chối hoàn toàn</summary>
        /// <remarks>
        /// Review bị từ chối không thể sửa chữa:
        /// - Không đáp ứng basic requirements
        /// - Vi phạm policy hoặc standards
        /// - Quality quá thấp, cần làm lại từ đầu
        /// - Security/compliance issues nghiêm trọng
        /// - Item sẽ không được publish/deploy
        /// - Cần feedback chi tiết về lý do reject
        /// </remarks>
        [Description("Đã từ chối")]
        Rejected = 2,

        /// <summary>RequestChanges - Yêu cầu chỉnh sửa</summary>
        /// <remarks>
        /// Cần sửa đổi trước khi approve:
        /// - Overall direction tốt nhưng cần improvements
        /// - Có specific feedback cho từng issue
        /// - Minor/moderate changes required
        /// - Có thể approve sau khi address comments
        /// - Trigger re-review cycle
        /// - Maintain review history và change tracking
        /// </remarks>
        [Description("Yêu cầu chỉnh sửa")]
        RequestChanges = 3,

        /// <summary>Cancelled - Hủy bỏ review</summary>
        /// <remarks>
        /// Review bị hủy bỏ vì các lý do:
        /// - Requirements thay đổi giữa chừng
        /// - Item không còn relevant
        /// - Reviewer không available
        /// - Superseded by newer version
        /// - Business priority thay đổi
        /// - Technical constraints không thể giải quyết
        /// </remarks>
        [Description("Đã hủy bỏ")]
        Cancelled = 4
    }

    /// <summary>
    /// Review types cho different workflows
    /// </summary>
    public enum ReviewType
    {
        [Description("Xem xét tài liệu")]
        DocumentReview = 0,
        [Description("Đánh giá mã nguồn")]
        CodeReview = 1,
        [Description("Phê duyệt thiết kế")]
        DesignReview = 2,
        [Description("Kiểm tra bảo mật")]
        SecurityReview = 3,
        [Description("Phê duyệt ngân sách")]
        BudgetApproval = 4,
        [Description("Đánh giá chất lượng")]
        QualityAssurance = 5,
        [Description("Phê duyệt pháp lý")]
        LegalReview = 6
    }

    /// <summary>
    /// Review workflow và business logic helper
    /// </summary>
    public static class ReviewResultHelper
    {
        /// <summary>
        /// Các kết quả được coi là "final" (không cần action thêm)
        /// </summary>
        public static readonly ReviewResult[] FinalResults =
        {
            ReviewResult.Approved,
            ReviewResult.Rejected,
            ReviewResult.Cancelled
        };

        /// <summary>
        /// Các kết quả cần follow-up action
        /// </summary>
        public static readonly ReviewResult[] ActionRequiredResults =
        {
            ReviewResult.Pending,
            ReviewResult.RequestChanges
        };

        /// <summary>
        /// Các kết quả positive (tích cực)
        /// </summary>
        public static readonly ReviewResult[] PositiveResults =
        {
            ReviewResult.Approved,
            ReviewResult.RequestChanges  // Vẫn có potential để approve
        };

        /// <summary>
        /// Các kết quả negative (tiêu cực)
        /// </summary>
        public static readonly ReviewResult[] NegativeResults =
        {
            ReviewResult.Rejected,
            ReviewResult.Cancelled
        };

        /// <summary>
        /// Kiểm tra review đã hoàn tất chưa
        /// </summary>
        public static bool IsCompleted(ReviewResult result)
        {
            return FinalResults.Contains(result);
        }

        /// <summary>
        /// Kiểm tra có cần action từ author không
        /// </summary>
        public static bool RequiresAuthorAction(ReviewResult result)
        {
            return result == ReviewResult.RequestChanges;
        }

        /// <summary>
        /// Kiểm tra có cần action từ reviewer không
        /// </summary>
        public static bool RequiresReviewerAction(ReviewResult result)
        {
            return result == ReviewResult.Pending;
        }

        /// <summary>
        /// Kiểm tra có thể deploy/release không
        /// </summary>
        public static bool CanProceed(ReviewResult result)
        {
            return result == ReviewResult.Approved;
        }

        /// <summary>
        /// Lấy màu sắc cho hiển thị UI
        /// </summary>
        public static string GetDisplayColor(ReviewResult result)
        {

            switch(result)
            {
                case ReviewResult.Pending:
                    return "#FFC107"; // Yellow - Warning
                case ReviewResult.Approved:
                    return "#28A745"; // Green - Success
                case ReviewResult.Rejected:
                    return "#DC3545"; // Red - Danger
                case ReviewResult.RequestChanges:
                    return "#FD7E14"; // Orange - Info
                case ReviewResult.Cancelled:
                    return "#6C757D"; // Gray - Secondary
                default:
                    return "#000000"; // Black - Default
            }
        }

        /// <summary>
        /// Lấy icon cho hiển thị UI
        /// </summary>
        public static string GetDisplayIcon(ReviewResult result)
        {
            switch(result)
            {
                case ReviewResult.Pending:
                    return "⏳"; // Hourglass
                case ReviewResult.Approved:
                    return "✅"; // Check mark
                case ReviewResult.Rejected:
                    return "❌"; // Cross mark
                case ReviewResult.RequestChanges:
                    return "📝"; // Memo
                case ReviewResult.Cancelled:
                    return "🚫"; // Prohibited
                default:
                    return "❓"; // Question mark
            }
        }

        /// <summary>
        /// Lấy next actions cho mỗi result
        /// </summary>
        public static string[] GetNextActions(ReviewResult result)
        {

            switch(result)
            {
                case ReviewResult.Pending:
                    return new[] { "Start Review", "Assign Reviewer", "Cancel" };
                case ReviewResult.Approved:
                    return new[] { "Deploy", "Merge", "Archive" };
                case ReviewResult.Rejected:
                    return new[] { "Revise", "Archive", "Escalate" };
                case ReviewResult.RequestChanges:
                    return new[] { "Address Comments", "Request Re-review", "Discuss" };
                case ReviewResult.Cancelled:
                    return new[] { "Archive", "Restart" };
                default:
                    return new string[0];
            }
        }

        /// <summary>
        /// Tính completion rate cho multiple reviews
        /// </summary>
        public static (double CompletionRate, Dictionary<ReviewResult, int> Distribution)
            CalculateReviewMetrics(IEnumerable<ReviewResult> reviews)
        {
            var reviewList = reviews.ToList();
            var total = reviewList.Count;

            if (total == 0)
                return (0, new Dictionary<ReviewResult, int>());

            var completed = reviewList.Count(r => IsCompleted(r));
            var completionRate = (double)completed / total * 100;

            var distribution = reviewList
                .GroupBy(r => r)
                .ToDictionary(g => g.Key, g => g.Count());

            return (completionRate, distribution);
        }

        /// <summary>
        /// Determine overall status từ multiple review results
        /// </summary>
        public static ReviewResult GetOverallStatus(IEnumerable<ReviewResult> reviews)
        {
            var reviewList = reviews.ToList();

            if (!reviewList.Any())
                return ReviewResult.Pending;

            // Nếu có bất kỳ rejected nào -> overall rejected
            if (reviewList.Any(r => r == ReviewResult.Rejected))
                return ReviewResult.Rejected;

            // Nếu có request changes -> overall request changes
            if (reviewList.Any(r => r == ReviewResult.RequestChanges))
                return ReviewResult.RequestChanges;

            // Nếu có pending -> overall pending
            if (reviewList.Any(r => r == ReviewResult.Pending))
                return ReviewResult.Pending;

            // Nếu có cancelled nhưng không có negative khác -> cancelled
            if (reviewList.Any(r => r == ReviewResult.Cancelled))
                return ReviewResult.Cancelled;

            // Tất cả approved -> overall approved
            if (reviewList.All(r => r == ReviewResult.Approved))
                return ReviewResult.Approved;

            return ReviewResult.Pending; // Default fallback
        }

        /// <summary>
        /// Generate notification message dựa trên review result
        /// </summary>
        public static string GenerateNotificationMessage(
            ReviewResult result,
            string reviewerName,
            string itemTitle,
            string comment = null)
        {
            string baseMessage = string.Empty;

            switch(result)
            {
                case ReviewResult.Approved:
                    baseMessage += $"✅ {reviewerName} đã phê duyệt '{itemTitle}'";
                    break;
                case ReviewResult.Rejected:
                    baseMessage += $"❌ {reviewerName} đã từ chối '{itemTitle}'";
                    break;
                case ReviewResult.RequestChanges:
                    baseMessage += $"📝 {reviewerName} yêu cầu chỉnh sửa '{itemTitle}'";
                    break;
                case ReviewResult.Cancelled:
                    baseMessage += $"🚫 Review '{itemTitle}' đã bị hủy bởi {reviewerName}";
                    break;
                case ReviewResult.Pending:
                    baseMessage += $"⏳ '{itemTitle}' đang chờ review từ {reviewerName}";
                    break;
            }


            if (!string.IsNullOrEmpty(comment))
            {
                baseMessage += $"\nGhi chú: {comment}";
            }

            return baseMessage;
        }
    }

}