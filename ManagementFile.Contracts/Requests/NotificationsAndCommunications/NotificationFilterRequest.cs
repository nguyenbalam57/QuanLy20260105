using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.NotificationsAndCommunications
{
    /// <summary>
    /// Request để filter notifications
    /// </summary>
    public class NotificationFilterRequest
    {
        public int UserId { get; set; }
        public NotificationType? Type { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }
}
