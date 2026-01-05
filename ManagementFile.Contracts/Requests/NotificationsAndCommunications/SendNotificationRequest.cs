using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.NotificationsAndCommunications
{
    /// <summary>
    /// Request để gửi notification
    /// </summary>
    public class SendNotificationRequest
    {
        [Required]
        public int UserId { get; set; } = -1;

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Content { get; set; } = "";

        public NotificationType Type { get; set; } = NotificationType.Info;

        public string RelatedEntityType { get; set; }

        public int RelatedEntityId { get; set; }

        public string ActionUrl { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
